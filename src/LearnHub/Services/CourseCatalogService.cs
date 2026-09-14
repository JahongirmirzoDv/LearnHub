using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Public;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

/// <summary>Public (guest and student) view of the course catalogue.</summary>
public interface ICourseCatalogService
{
    Task<HomeViewModel> GetHomePageAsync(string? userId, CancellationToken cancellationToken = default);

    Task<CourseListViewModel> SearchAsync(CourseSearchQuery query, string? userId, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the course does not exist or is unpublished (unless viewed by an admin).</summary>
    Task<CourseDetailsViewModel?> GetDetailsAsync(int id, string? userId, bool isAdmin, CancellationToken cancellationToken = default);
}

public sealed class CourseCatalogService(
    ApplicationDbContext db,
    ICategoryService categories,
    IProgressService progress) : ICourseCatalogService
{
    public const int PageSize = 9;

    public async Task<HomeViewModel> GetHomePageAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var published = db.Courses.AsNoTracking().Where(c => c.IsPublished);

        var popular = await published
            .OrderByDescending(c => c.Enrollments.Count)
            .ThenByDescending(c => c.CreatedAt)
            .Take(6)
            .Select(CourseProjections.ToCard(userId))
            .ToListAsync(cancellationToken);
        await progress.AttachAsync(popular, userId, cancellationToken);

        var learnerCount = await (
            from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            where role.Name == AppRoles.Student
            select userRole.UserId).CountAsync(cancellationToken);

        return new HomeViewModel
        {
            PopularCourses = popular,
            Categories = await categories.GetPublicOptionsAsync(cancellationToken),
            PublishedCourseCount = await published.CountAsync(cancellationToken),
            LearnerCount = learnerCount,
            ResourceCount = await db.LearningResources.CountAsync(r => r.IsPublished && r.Course.IsPublished, cancellationToken),
            QuizCount = await db.Quizzes.CountAsync(q => q.IsPublished && q.Questions.Any() && q.Course.IsPublished, cancellationToken),
            LessonFormats = await db.LearningResources.AsNoTracking()
                .Where(r => r.IsPublished && r.Course.IsPublished)
                .GroupBy(r => r.Type)
                .Select(group => new LessonFormatCount(group.Key, group.Count()))
                .ToListAsync(cancellationToken),
            PreviewLesson = await db.LearningResources.AsNoTracking()
                .Where(r => r.IsPreview && r.IsPublished && r.Course.IsPublished)
                .OrderByDescending(r => r.Course.Enrollments.Count).ThenBy(r => r.SortOrder)
                .Select(r => new PreviewLessonLink(r.Id, r.Title, r.Course.Title))
                .FirstOrDefaultAsync(cancellationToken)
        };
    }

    public async Task<CourseListViewModel> SearchAsync(CourseSearchQuery query, string? userId, CancellationToken cancellationToken = default)
    {
        var courses = db.Courses.AsNoTracking().Where(c => c.IsPublished);
        var pattern = string.IsNullOrWhiteSpace(query.Q) ? null : SearchPattern.Contains(query.Q);

        if (pattern is not null)
        {
            courses = courses.Where(c =>
                EF.Functions.Like(c.Title, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(c.ShortDescription, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(c.Description, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(c.InstructorName, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(c.Category.Name, pattern, SearchPattern.EscapeCharacter)
                || c.Resources.Any(r => r.IsPublished && EF.Functions.Like(r.Title, pattern, SearchPattern.EscapeCharacter)));
        }

        if (query.CategoryId is int categoryId)
        {
            courses = courses.Where(c => c.CategoryId == categoryId);
        }

        if (query.Difficulty is DifficultyLevel difficulty)
        {
            courses = courses.Where(c => c.Difficulty == difficulty);
        }

        courses = query.Sort switch
        {
            CourseSort.Popular => courses.OrderByDescending(c => c.Enrollments.Count).ThenBy(c => c.Title),
            CourseSort.Title => courses.OrderBy(c => c.Title),
            CourseSort.Shortest => courses.OrderBy(c => c.DurationMinutes).ThenBy(c => c.Title),
            _ => courses.OrderByDescending(c => c.CreatedAt).ThenBy(c => c.Id)
        };

        var results = await courses
            .Select(CourseProjections.ToCard(userId))
            .ToPagedResultAsync(query.Page, PageSize, cancellationToken);
        await progress.AttachAsync(results.Items, userId, cancellationToken);

        if (pattern is not null && results.Items.Count > 0)
        {
            await AttachMatchingLessonsAsync(results.Items, pattern, cancellationToken);
        }

        query.Page = results.Page;
        return new CourseListViewModel
        {
            Query = query,
            Results = results,
            Categories = await categories.GetPublicOptionsAsync(cancellationToken)
        };
    }

    /// <summary>Shows which lessons matched, so a result found through a lesson title is not a mystery.</summary>
    private async Task AttachMatchingLessonsAsync(IReadOnlyList<CourseCardViewModel> cards, string pattern, CancellationToken cancellationToken)
    {
        const int maxPerCourse = 3;
        var courseIds = cards.Select(card => card.Id).ToList();
        var lessons = await db.LearningResources.AsNoTracking()
            .Where(r => courseIds.Contains(r.CourseId) && r.IsPublished && EF.Functions.Like(r.Title, pattern, SearchPattern.EscapeCharacter))
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => new { r.CourseId, r.Title })
            .ToListAsync(cancellationToken);

        var byCourse = lessons.ToLookup(lesson => lesson.CourseId, lesson => lesson.Title);
        foreach (var card in cards)
        {
            card.MatchingLessons = byCourse[card.Id].Take(maxPerCourse).ToList();
        }
    }

    public async Task<CourseDetailsViewModel?> GetDetailsAsync(int id, string? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(c => c.Id == id && (c.IsPublished || isAdmin))
            .Select(c => new CourseDetailsViewModel
            {
                Id = c.Id,
                Title = c.Title,
                ShortDescription = c.ShortDescription,
                Description = c.Description,
                LearningOutcomes = c.LearningOutcomes,
                InstructorName = c.InstructorName,
                Difficulty = c.Difficulty,
                DurationMinutes = c.DurationMinutes,
                ThumbnailPath = c.ThumbnailPath,
                CategoryId = c.CategoryId,
                CategoryName = c.Category.Name,
                CategoryIcon = c.Category.IconName,
                IsPublished = c.IsPublished,
                UpdatedAt = c.UpdatedAt,
                EnrollmentCount = c.Enrollments.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (course is null)
        {
            return null;
        }

        course.IsAdminView = isAdmin;

        course.Resources = await db.LearningResources.AsNoTracking()
            .Where(r => r.CourseId == id && (r.IsPublished || isAdmin))
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => new CourseResourceItem
            {
                Id = r.Id,
                Title = r.Title,
                Summary = r.Summary,
                Type = r.Type,
                EstimatedMinutes = r.EstimatedMinutes,
                IsPreview = r.IsPreview,
                IsPublished = r.IsPublished,
                IsCompleted = r.Completions.Any(done => done.UserId == userId)
            })
            .ToListAsync(cancellationToken);

        course.Quizzes = await db.Quizzes.AsNoTracking()
            .Where(q => q.CourseId == id && q.IsPublished && q.Questions.Any())
            .OrderBy(q => q.Id)
            .Select(q => new CourseQuizItem
            {
                Id = q.Id,
                Title = q.Title,
                Description = q.Description,
                QuestionCount = q.Questions.Count,
                PassMarkPercent = q.PassMarkPercent,
                AttemptCount = q.Attempts.Count(a => a.UserId == userId),
                BestScorePercent = q.Attempts.Where(a => a.UserId == userId).Max(a => (int?)a.ScorePercent),
                HasPassed = q.Attempts.Any(a => a.UserId == userId && a.Passed)
            })
            .ToListAsync(cancellationToken);

        if (userId is not null)
        {
            var enrolledAt = await db.Enrollments.AsNoTracking()
                .Where(e => e.CourseId == id && e.UserId == userId)
                .Select(e => (DateTime?)e.EnrolledAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (enrolledAt is not null)
            {
                course.IsEnrolled = true;
                course.EnrolledAt = enrolledAt;
                // Same definition as ProgressService, derived from the rows already loaded (drafts never count).
                var lessons = course.Resources.Where(r => r.IsPublished).ToList();
                course.Progress = new CourseProgress(
                    lessons.Count(r => r.IsCompleted) + course.Quizzes.Count(q => q.HasPassed),
                    lessons.Count + course.Quizzes.Count);
                course.NextResourceId = (lessons.FirstOrDefault(r => !r.IsCompleted) ?? lessons.FirstOrDefault())?.Id;
            }
        }

        course.RelatedCourses = await db.Courses.AsNoTracking()
            .Where(c => c.IsPublished && c.CategoryId == course.CategoryId && c.Id != id)
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(3)
            .Select(CourseProjections.ToCard(userId))
            .ToListAsync(cancellationToken);

        return course;
    }
}
