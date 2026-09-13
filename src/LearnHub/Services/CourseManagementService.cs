using LearnHub.Data;
using LearnHub.Models;
using LearnHub.Services.Storage;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface ICourseManagementService
{
    Task<AdminCourseListViewModel> GetListAsync(AdminCourseQuery query, CancellationToken cancellationToken = default);

    Task<AdminCourseDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<CourseFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(CourseFormViewModel model, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(int id, CourseFormViewModel model, CancellationToken cancellationToken = default);

    /// <summary>Publishes or unpublishes a course; the value is the new published state.</summary>
    Task<OperationResult<bool>> TogglePublishedAsync(int id, CancellationToken cancellationToken = default);

    Task<CourseDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class CourseManagementService(
    ApplicationDbContext db,
    ILookupService lookups,
    IFileStorageService storage,
    ILogger<CourseManagementService> logger) : ICourseManagementService
{
    public const int PageSize = 12;

    public async Task<AdminCourseListViewModel> GetListAsync(AdminCourseQuery query, CancellationToken cancellationToken = default)
    {
        var courses = db.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = SearchPattern.Contains(query.Q);
            courses = courses.Where(c =>
                EF.Functions.Like(c.Title, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(c.InstructorName, pattern, SearchPattern.EscapeCharacter));
        }

        if (query.CategoryId is int categoryId)
        {
            courses = courses.Where(c => c.CategoryId == categoryId);
        }

        courses = query.Status switch
        {
            PublishStatusFilter.Published => courses.Where(c => c.IsPublished),
            PublishStatusFilter.Draft => courses.Where(c => !c.IsPublished),
            _ => courses
        };

        var results = await courses
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new AdminCourseListItem(
                c.Id, c.Title, c.Category.Name, c.Difficulty, c.IsPublished,
                c.Resources.Count, c.Quizzes.Count, c.Enrollments.Count, c.ThumbnailPath, c.UpdatedAt))
            .ToPagedResultAsync(query.Page, PageSize, cancellationToken);

        query.Page = results.Page;
        return new AdminCourseListViewModel
        {
            Query = query,
            Results = results,
            Categories = await lookups.GetCategoriesAsync(cancellationToken)
        };
    }

    public async Task<AdminCourseDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new AdminCourseDetailsViewModel
            {
                Id = c.Id,
                Title = c.Title,
                ShortDescription = c.ShortDescription,
                CategoryName = c.Category.Name,
                Difficulty = c.Difficulty,
                DurationMinutes = c.DurationMinutes,
                InstructorName = c.InstructorName,
                IsPublished = c.IsPublished,
                ThumbnailPath = c.ThumbnailPath,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                EnrollmentCount = c.Enrollments.Count,
                AttemptCount = c.Quizzes.SelectMany(q => q.Attempts).Count(),
                Resources = c.Resources
                    .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
                    .Select(r => new AdminResourceListItem(r.Id, r.Title, r.Type, c.Id, c.Title, r.SortOrder, r.IsPreview, r.Completions.Count, r.UpdatedAt))
                    .ToList(),
                Quizzes = c.Quizzes
                    .OrderBy(q => q.Id)
                    .Select(q => new AdminQuizListItem(q.Id, q.Title, c.Id, c.Title, q.Questions.Count, q.Attempts.Count, q.PassMarkPercent, q.IsPublished, q.UpdatedAt))
                    .ToList()
            })
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (course is not null)
        {
            // A limited (Take) collection inside a projection needs SQL APPLY, which SQLite lacks; query it separately.
            course.RecentEnrollments = await db.Enrollments.AsNoTracking()
                .Where(e => e.CourseId == id)
                .OrderByDescending(e => e.EnrolledAt)
                .Take(5)
                .Select(e => new RecentEnrollmentItem(e.User.FullName, e.CourseId, e.Course.Title, e.EnrolledAt))
                .ToListAsync(cancellationToken);
        }

        return course;
    }

    public async Task<CourseFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var model = await db.Courses.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseFormViewModel
            {
                Id = c.Id,
                Title = c.Title,
                ShortDescription = c.ShortDescription,
                Description = c.Description,
                LearningOutcomes = c.LearningOutcomes,
                InstructorName = c.InstructorName,
                CategoryId = c.CategoryId,
                Difficulty = c.Difficulty,
                DurationMinutes = c.DurationMinutes,
                IsPublished = c.IsPublished,
                ExistingThumbnailPath = c.ThumbnailPath
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (model is not null)
        {
            model.CategoryOptions = await lookups.GetCategoriesAsync(cancellationToken);
        }

        return model;
    }

    public async Task<OperationResult<int>> CreateAsync(CourseFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == model.CategoryId, cancellationToken))
        {
            return OperationResult<int>.Failure("The selected category no longer exists.");
        }

        var course = new Course();
        ApplyForm(course, model);

        StoredFile? uploaded = null;
        if (model.ThumbnailFile is not null)
        {
            var saved = await storage.SaveAsync(model.ThumbnailFile, UploadKind.CourseThumbnail, cancellationToken);
            if (!saved.Succeeded)
            {
                return OperationResult<int>.Failure(saved.Error!);
            }

            uploaded = saved.Value!;
            course.ThumbnailPath = storage.GetThumbnailUrl(uploaded);
        }

        db.Courses.Add(course);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Do not leave an orphaned upload behind when the database insert fails.
            storage.Delete(uploaded?.RelativePath);
            throw;
        }

        logger.LogInformation("Course {CourseId} \"{Title}\" created.", course.Id, course.Title);
        return OperationResult<int>.Success(course.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, CourseFormViewModel model, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (course is null)
        {
            return OperationResult.NotFound();
        }

        if (!await db.Categories.AnyAsync(c => c.Id == model.CategoryId, cancellationToken))
        {
            return OperationResult.Failure("The selected category no longer exists.");
        }

        ApplyForm(course, model);

        var previousThumbnail = course.ThumbnailPath;
        StoredFile? uploaded = null;
        if (model.ThumbnailFile is not null)
        {
            var saved = await storage.SaveAsync(model.ThumbnailFile, UploadKind.CourseThumbnail, cancellationToken);
            if (!saved.Succeeded)
            {
                return OperationResult.Failure(saved.Error!);
            }

            uploaded = saved.Value!;
            course.ThumbnailPath = storage.GetThumbnailUrl(uploaded);
        }
        else if (model.RemoveThumbnail)
        {
            course.ThumbnailPath = null;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            storage.Delete(uploaded?.RelativePath);
            throw;
        }

        if (previousThumbnail != course.ThumbnailPath)
        {
            storage.DeleteThumbnail(previousThumbnail);
        }

        logger.LogInformation("Course {CourseId} updated.", id);
        return OperationResult.Success();
    }

    public async Task<OperationResult<bool>> TogglePublishedAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (course is null)
        {
            return OperationResult<bool>.NotFound();
        }

        course.IsPublished = !course.IsPublished;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Course {CourseId} {State}.", id, course.IsPublished ? "published" : "unpublished");
        return OperationResult<bool>.Success(course.IsPublished);
    }

    public async Task<CourseDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Courses.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseDeleteViewModel(
                c.Id,
                c.Title,
                c.IsPublished,
                c.Resources.Count,
                c.Quizzes.Count,
                c.Enrollments.Count,
                c.Quizzes.SelectMany(q => q.Attempts).Count()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.Title, c.ThumbnailPath })
            .FirstOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return OperationResult.NotFound();
        }

        var resourceFiles = await db.LearningResources.AsNoTracking()
            .Where(r => r.CourseId == id && r.FilePath != null)
            .Select(r => r.FilePath!)
            .ToListAsync(cancellationToken);

        await db.InTransactionAsync(async () =>
        {
            // Quiz answers reference questions with NO ACTION, so they are removed first; deleting the course row
            // then lets the database cascade to resources, quizzes, questions, options, attempts and enrolments.
            await db.QuizAnswers
                .Where(answer => answer.QuizAttempt.Quiz.CourseId == id)
                .ExecuteDeleteAsync(cancellationToken);
            await db.Courses.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
        }, cancellationToken);

        // Files are removed only after the database change has been committed.
        foreach (var file in resourceFiles)
        {
            storage.Delete(file);
        }

        storage.DeleteThumbnail(course.ThumbnailPath);
        logger.LogInformation("Course {CourseId} \"{Title}\" deleted with {FileCount} stored files.", id, course.Title, resourceFiles.Count);
        return OperationResult.Success();
    }

    private static void ApplyForm(Course course, CourseFormViewModel model)
    {
        course.Title = TextInput.Required(model.Title);
        course.ShortDescription = TextInput.Required(model.ShortDescription);
        course.Description = TextInput.Required(model.Description);
        course.LearningOutcomes = TextInput.Clean(model.LearningOutcomes);
        course.InstructorName = TextInput.Required(model.InstructorName);
        course.CategoryId = model.CategoryId!.Value;
        course.Difficulty = model.Difficulty;
        course.DurationMinutes = model.DurationMinutes;
        course.IsPublished = model.IsPublished;
    }
}
