using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Learning;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface IEnrollmentService
{
    Task<OperationResult> EnrollAsync(string userId, int courseId, CancellationToken cancellationToken = default);

    /// <summary>Removes the enrolment. Completion records and quiz attempts are kept so progress returns on re-enrolment.</summary>
    Task<OperationResult> LeaveAsync(string userId, int courseId, CancellationToken cancellationToken = default);

    /// <summary>Published courses the student is enrolled in, most recently used first, with progress and next lesson.</summary>
    Task<IReadOnlyList<EnrolledCourseViewModel>> GetEnrolledCoursesAsync(string userId, CancellationToken cancellationToken = default);

    Task<MyCoursesViewModel> GetMyCoursesAsync(string userId, MyCoursesFilter filter, CancellationToken cancellationToken = default);

    Task<bool> IsEnrolledAsync(string userId, int courseId, CancellationToken cancellationToken = default);

    Task RecordAccessAsync(string userId, int courseId, CancellationToken cancellationToken = default);

    Task<AdminEnrollmentListViewModel> GetAdminListAsync(AdminEnrollmentQuery query, CancellationToken cancellationToken = default);

    Task<OperationResult> AdminEnrollAsync(EnrollmentCreateViewModel model, CancellationToken cancellationToken = default);

    Task<EnrollmentDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult> RemoveAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class EnrollmentService(
    ApplicationDbContext db,
    IProgressService progress,
    ILookupService lookups,
    TimeProvider clock,
    ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public const int AdminPageSize = 20;

    public async Task<OperationResult> EnrollAsync(string userId, int courseId, CancellationToken cancellationToken = default)
    {
        var isPublished = await db.Courses.AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => (bool?)c.IsPublished)
            .FirstOrDefaultAsync(cancellationToken);

        if (isPublished is not true)
        {
            return OperationResult.NotFound();
        }

        return await AddEnrollmentAsync(userId, courseId, "You are already enrolled in this course.", cancellationToken);
    }

    public async Task<OperationResult> LeaveAsync(string userId, int courseId, CancellationToken cancellationToken = default)
    {
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, cancellationToken);
        if (enrollment is null)
        {
            return OperationResult.NotFound();
        }

        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} left course {CourseId}.", userId, courseId);
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<EnrolledCourseViewModel>> GetEnrolledCoursesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var enrollments = await db.Enrollments.AsNoTracking()
            .Where(e => e.UserId == userId && e.Course.IsPublished)
            .Select(e => new { e.CourseId, e.EnrolledAt, e.LastAccessedAt })
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            return [];
        }

        var courseIds = enrollments.Select(e => e.CourseId).ToList();
        var cards = await db.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .Select(CourseProjections.ToCard(userId))
            .ToDictionaryAsync(card => card.Id, cancellationToken);

        var progressByCourse = await progress.GetForCoursesAsync(userId, courseIds, cancellationToken);

        // Next lesson = first published resource (by order) the student has not completed.
        var incomplete = await db.LearningResources.AsNoTracking()
            .Where(r => courseIds.Contains(r.CourseId) && r.IsPublished && !r.Completions.Any(done => done.UserId == userId))
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => new { r.CourseId, r.Id, r.Title })
            .ToListAsync(cancellationToken);
        var nextByCourse = incomplete
            .GroupBy(r => r.CourseId)
            .ToDictionary(group => group.Key, group => group.First());

        return enrollments
            .Where(e => cards.ContainsKey(e.CourseId))
            .OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
            .Select(e =>
            {
                var card = cards[e.CourseId];
                var courseProgress = progressByCourse.GetValueOrDefault(e.CourseId, CourseProgress.Empty);
                card.Progress = courseProgress;
                nextByCourse.TryGetValue(e.CourseId, out var next);
                return new EnrolledCourseViewModel
                {
                    Course = card,
                    EnrolledAt = e.EnrolledAt,
                    LastAccessedAt = e.LastAccessedAt,
                    Progress = courseProgress,
                    NextResourceId = next?.Id,
                    NextResourceTitle = next?.Title
                };
            })
            .ToList();
    }

    public async Task<MyCoursesViewModel> GetMyCoursesAsync(string userId, MyCoursesFilter filter, CancellationToken cancellationToken = default)
    {
        var all = await GetEnrolledCoursesAsync(userId, cancellationToken);
        var filtered = filter switch
        {
            MyCoursesFilter.Completed => all.Where(c => c.Progress.IsCompleted).ToList(),
            MyCoursesFilter.InProgress => all.Where(c => !c.Progress.IsCompleted).ToList(),
            _ => all
        };

        return new MyCoursesViewModel
        {
            Filter = filter,
            Courses = filtered,
            AllCount = all.Count,
            CompletedCount = all.Count(c => c.Progress.IsCompleted),
            InProgressCount = all.Count(c => !c.Progress.IsCompleted)
        };
    }

    public Task<bool> IsEnrolledAsync(string userId, int courseId, CancellationToken cancellationToken = default) =>
        db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId, cancellationToken);

    public async Task RecordAccessAsync(string userId, int courseId, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await db.Enrollments
            .Where(e => e.UserId == userId && e.CourseId == courseId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.LastAccessedAt, (DateTime?)now), cancellationToken);
    }

    public async Task<AdminEnrollmentListViewModel> GetAdminListAsync(AdminEnrollmentQuery query, CancellationToken cancellationToken = default)
    {
        var enrollments = db.Enrollments.AsNoTracking();

        if (query.CourseId is int courseId)
        {
            enrollments = enrollments.Where(e => e.CourseId == courseId);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = SearchPattern.Contains(query.Q);
            enrollments = enrollments.Where(e =>
                EF.Functions.Like(e.User.FullName, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(e.User.Email!, pattern, SearchPattern.EscapeCharacter));
        }

        var page = await enrollments
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new
            {
                e.Id,
                e.UserId,
                StudentName = e.User.FullName,
                StudentEmail = e.User.Email ?? string.Empty,
                e.CourseId,
                CourseTitle = e.Course.Title,
                e.EnrolledAt,
                e.LastAccessedAt,
                e.CompletionPercentage,
                e.CompletedAt
            })
            .ToPagedResultAsync(query.Page, AdminPageSize, cancellationToken);

        var items = page.Items
            .Select(e => new AdminEnrollmentListItem(
                e.Id, e.UserId, e.StudentName, e.StudentEmail, e.CourseId, e.CourseTitle, e.EnrolledAt, e.LastAccessedAt,
                e.CompletionPercentage, e.CompletedAt))
            .ToList();

        query.Page = page.Page;
        return new AdminEnrollmentListViewModel
        {
            Query = query,
            Results = new PagedResult<AdminEnrollmentListItem>(items, page.Page, page.PageSize, page.TotalCount),
            Courses = await lookups.GetCoursesAsync(cancellationToken)
        };
    }

    public async Task<OperationResult> AdminEnrollAsync(EnrollmentCreateViewModel model, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = model.StudentEmail.Trim().ToUpperInvariant();
        var student = await (
            from user in db.Users
            where user.NormalizedEmail == normalizedEmail
            select new
            {
                user.Id,
                IsStudent = db.UserRoles.Any(ur => ur.UserId == user.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Student))
            }).FirstOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            return OperationResult.Failure("No account uses that email address.");
        }

        if (!student.IsStudent)
        {
            return OperationResult.Failure("Only student accounts can be enrolled in courses.");
        }

        if (!await db.Courses.AnyAsync(c => c.Id == model.CourseId, cancellationToken))
        {
            return OperationResult.Failure("The selected course no longer exists.");
        }

        return await AddEnrollmentAsync(student.Id, model.CourseId!.Value, "This student is already enrolled in that course.", cancellationToken);
    }

    public async Task<EnrollmentDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Enrollments.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EnrollmentDeleteViewModel(e.Id, e.User.FullName, e.User.Email ?? string.Empty, e.Course.Title, e.EnrolledAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult> RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await db.Enrollments.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return OperationResult.NotFound();
        }

        logger.LogInformation("Enrollment {EnrollmentId} removed by an administrator.", id);
        return OperationResult.Success();
    }

    private async Task<OperationResult> AddEnrollmentAsync(string userId, int courseId, string duplicateMessage, CancellationToken cancellationToken)
    {
        if (await db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId, cancellationToken))
        {
            return OperationResult.Failure(duplicateMessage);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        db.Enrollments.Add(new Enrollment { UserId = userId, CourseId = courseId, EnrolledAt = now, LastAccessedAt = now });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Double-submitted form: the unique (UserId, CourseId) index rejected the second insert.
            logger.LogWarning(exception, "Duplicate enrollment for user {UserId} in course {CourseId}.", userId, courseId);
            db.ChangeTracker.Clear();
            return OperationResult.Failure(duplicateMessage);
        }

        // Lessons completed before leaving the course count again straight away.
        await progress.SyncAsync(courseId, userId, cancellationToken);

        logger.LogInformation("User {UserId} enrolled in course {CourseId}.", userId, courseId);
        return OperationResult.Success();
    }
}
