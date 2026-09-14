using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Learning;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface IDashboardService
{
    Task<StudentDashboardViewModel> GetStudentDashboardAsync(string userId, string fullName, CancellationToken cancellationToken = default);

    Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>Per-course lesson and quiz progress for the student's Progress page.</summary>
    Task<StudentProgressViewModel> GetProgressReportAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>Builds dashboards exclusively from real database data – no placeholder KPIs.</summary>
public sealed class DashboardService(
    ApplicationDbContext db,
    IEnrollmentService enrollments,
    IQuizService quizzes,
    IContactService contact,
    TimeProvider clock) : IDashboardService
{
    public async Task<StudentDashboardViewModel> GetStudentDashboardAsync(string userId, string fullName, CancellationToken cancellationToken = default)
    {
        var enrolled = await enrollments.GetEnrolledCoursesAsync(userId, cancellationToken);
        var history = await quizzes.GetHistoryAsync(userId, cancellationToken: cancellationToken);
        var enrolledIds = enrolled.Select(e => e.Course.Id).ToList();

        var availableCourseCount = await db.Courses.CountAsync(c => c.IsPublished && !enrolledIds.Contains(c.Id), cancellationToken);
        var completedResourceCount = await db.ResourceCompletions.CountAsync(c => c.UserId == userId && c.LearningResource.IsPublished, cancellationToken);

        // Best score per quiz is a fairer summary than averaging every retry.
        var bestScores = history.Attempts
            .GroupBy(a => a.QuizId)
            .Select(group => group.Max(a => a.ScorePercent))
            .ToList();

        var totalItems = enrolled.Sum(e => e.Progress.TotalItems);
        var completedItems = enrolled.Sum(e => e.Progress.CompletedItems);

        return new StudentDashboardViewModel
        {
            FirstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName,
            EnrolledCount = enrolled.Count,
            CompletedCount = enrolled.Count(e => e.Progress.IsCompleted),
            InProgressCount = enrolled.Count(e => !e.Progress.IsCompleted),
            AvailableCourseCount = availableCourseCount,
            CompletedResourceCount = completedResourceCount,
            QuizzesPassedCount = history.Attempts.Where(a => a.Passed).Select(a => a.QuizId).Distinct().Count(),
            AverageQuizScore = bestScores.Count == 0 ? null : (int)Math.Round(bestScores.Average()),
            OverallProgressPercent = new CourseProgress(completedItems, totalItems).Percent,
            ContinueLearning = enrolled.Where(e => !e.Progress.IsCompleted).Take(3).ToList(),
            RecentQuizResults = history.Attempts.Take(5).ToList(),
            RecentActivity = await GetRecentActivityAsync(userId, cancellationToken),
            RecommendedCourses = await GetRecommendationsAsync(userId, enrolled, cancellationToken)
        };
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var since = clock.GetUtcNow().UtcDateTime.AddDays(-30);

        var studentIds = UserIdsInRole(AppRoles.Student);

        var attemptCount = await db.QuizAttempts.CountAsync(cancellationToken);
        var passedAttemptCount = await db.QuizAttempts.CountAsync(a => a.Passed, cancellationToken);
        var averageScore = await db.QuizAttempts.AverageAsync(a => (double?)a.ScorePercent, cancellationToken);

        var popular = await db.Courses.AsNoTracking()
            .Where(c => c.Enrollments.Any())
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(5)
            .Select(c => new { c.Title, Count = c.Enrollments.Count })
            .ToListAsync(cancellationToken);

        var byCategory = await db.Categories.AsNoTracking()
            .Select(c => new { c.Name, Count = c.Courses.SelectMany(course => course.Enrollments).Count() })
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ToListAsync(cancellationToken);

        var courseWarnings = await db.Courses.AsNoTracking()
            .Where(c => c.IsPublished && !c.Resources.Any(r => r.IsPublished))
            .Select(c => new ContentWarning($"Published course \"{c.Title}\" has no published learning resources.", "Resources", "Create", c.Id))
            .ToListAsync(cancellationToken);

        var quizWarnings = await db.Quizzes.AsNoTracking()
            .Where(q => q.IsPublished && !q.Questions.Any())
            .Select(q => new ContentWarning($"Published quiz \"{q.Title}\" has no questions, so students cannot see it.", "Quizzes", "Details", q.Id))
            .ToListAsync(cancellationToken);

        return new AdminDashboardViewModel
        {
            UserCount = await db.Users.CountAsync(cancellationToken),
            AdminCount = await UserIdsInRole(AppRoles.Admin).CountAsync(cancellationToken),
            PublishedCourseCount = await db.Courses.CountAsync(c => c.IsPublished, cancellationToken),
            DraftCourseCount = await db.Courses.CountAsync(c => !c.IsPublished, cancellationToken),
            CategoryCount = await db.Categories.CountAsync(cancellationToken),
            ResourceCount = await db.LearningResources.CountAsync(cancellationToken),
            DraftResourceCount = await db.LearningResources.CountAsync(r => !r.IsPublished, cancellationToken),
            QuizCount = await db.Quizzes.CountAsync(cancellationToken),
            StudentCount = await studentIds.CountAsync(cancellationToken),
            NewStudentsLast30Days = await db.Users.CountAsync(u => u.CreatedAt >= since && studentIds.Contains(u.Id), cancellationToken),
            EnrollmentCount = await db.Enrollments.CountAsync(cancellationToken),
            EnrollmentsLast30Days = await db.Enrollments.CountAsync(e => e.EnrolledAt >= since, cancellationToken),
            CompletedEnrollmentCount = await db.Enrollments.CountAsync(e => e.CompletedAt != null, cancellationToken),
            AttemptCount = attemptCount,
            AverageScorePercent = averageScore is null ? null : (int)Math.Round(averageScore.Value),
            PassRatePercent = attemptCount == 0 ? null : (int)Math.Round(passedAttemptCount * 100.0 / attemptCount),
            UnreadMessageCount = await contact.CountUnreadAsync(cancellationToken),
            RecentEnrollments = await db.Enrollments.AsNoTracking()
                .OrderByDescending(e => e.EnrolledAt)
                .Take(6)
                .Select(e => new RecentEnrollmentItem(e.User.FullName, e.CourseId, e.Course.Title, e.EnrolledAt))
                .ToListAsync(cancellationToken),
            RecentAttempts = await db.QuizAttempts.AsNoTracking()
                .OrderByDescending(a => a.CompletedAt)
                .Take(6)
                .Select(a => new RecentAttemptItem(a.Id, a.User.FullName, a.Quiz.Title, a.ScorePercent, a.Passed, a.CompletedAt))
                .ToListAsync(cancellationToken),
            PopularCourses = ToBars(popular.Select(p => (p.Title, p.Count))),
            EnrollmentsByCategory = ToBars(byCategory.Select(c => (c.Name, c.Count))),
            ContentWarnings = courseWarnings.Concat(quizWarnings).ToList()
        };
    }

    public async Task<StudentProgressViewModel> GetProgressReportAsync(string userId, CancellationToken cancellationToken = default)
    {
        var courses = await db.Enrollments.AsNoTracking()
            .Where(e => e.UserId == userId && e.Course.IsPublished)
            .OrderByDescending(e => e.LastAccessedAt ?? e.EnrolledAt)
            .Select(e => new
            {
                e.CourseId,
                CourseTitle = e.Course.Title,
                e.Course.CategoryId,
                CategoryName = e.Course.Category.Name,
                e.EnrolledAt,
                e.CompletedAt,
                LessonsTotal = e.Course.Resources.Count(r => r.IsPublished),
                LessonsCompleted = e.Course.Resources.Count(r => r.IsPublished && r.Completions.Any(done => done.UserId == userId))
            })
            .ToListAsync(cancellationToken);

        var quizzesByCourse = (await quizzes.GetOverviewAsync(userId, cancellationToken)).Quizzes.ToLookup(q => q.CourseId);

        return new StudentProgressViewModel
        {
            Courses = courses
                .Select(c => new CourseProgressReport
                {
                    CourseId = c.CourseId,
                    CourseTitle = c.CourseTitle,
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    EnrolledAt = c.EnrolledAt,
                    CompletedAt = c.CompletedAt,
                    LessonsCompleted = c.LessonsCompleted,
                    LessonsTotal = c.LessonsTotal,
                    Quizzes = quizzesByCourse[c.CourseId].ToList()
                })
                .ToList()
        };
    }

    private IQueryable<string> UserIdsInRole(string roleName) =>
        from userRole in db.UserRoles
        join role in db.Roles on userRole.RoleId equals role.Id
        where role.Name == roleName
        select userRole.UserId;

    private async Task<IReadOnlyList<ActivityItem>> GetRecentActivityAsync(string userId, CancellationToken cancellationToken)
    {
        const int take = 8;

        var enrolledEvents = await db.Enrollments.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(take)
            .Select(e => new { When = e.EnrolledAt, e.CourseId, e.Course.Title })
            .ToListAsync(cancellationToken);

        var completionEvents = await db.ResourceCompletions.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CompletedAt)
            .Take(take)
            .Select(c => new { When = c.CompletedAt, c.LearningResourceId, c.LearningResource.Title })
            .ToListAsync(cancellationToken);

        var attemptEvents = await db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt)
            .Take(take)
            .Select(a => new { When = a.CompletedAt, a.Id, a.Quiz.Title, a.ScorePercent, a.Passed })
            .ToListAsync(cancellationToken);

        return enrolledEvents
            .Select(e => new ActivityItem(e.When, ActivityKind.Enrolled, $"Enrolled in {e.Title}", e.CourseId))
            .Concat(completionEvents.Select(c => new ActivityItem(c.When, ActivityKind.CompletedResource, $"Completed \"{c.Title}\"", c.LearningResourceId)))
            .Concat(attemptEvents.Select(a => new ActivityItem(
                a.When,
                a.Passed ? ActivityKind.PassedQuiz : ActivityKind.AttemptedQuiz,
                $"Scored {a.ScorePercent}% in {a.Title}",
                a.Id)))
            .OrderByDescending(item => item.OccurredAt)
            .Take(take)
            .ToList();
    }

    private async Task<IReadOnlyList<CourseCardViewModel>> GetRecommendationsAsync(
        string userId, IReadOnlyList<EnrolledCourseViewModel> enrolled, CancellationToken cancellationToken)
    {
        var enrolledIds = enrolled.Select(e => e.Course.Id).ToList();
        var preferredCategories = enrolled.Select(e => e.Course.CategoryId).Distinct().ToList();

        return await db.Courses.AsNoTracking()
            .Where(c => c.IsPublished && !enrolledIds.Contains(c.Id))
            .OrderByDescending(c => preferredCategories.Contains(c.CategoryId))
            .ThenByDescending(c => c.Enrollments.Count)
            .Take(3)
            .Select(CourseProjections.ToCard(userId))
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyList<ChartBar> ToBars(IEnumerable<(string Label, int Value)> values)
    {
        var list = values.ToList();
        var max = list.Count == 0 ? 0 : list.Max(v => v.Value);
        return list
            .Select(v => new ChartBar(v.Label, v.Value, max == 0 ? 0 : (int)Math.Round(v.Value * 100.0 / max)))
            .ToList();
    }
}
