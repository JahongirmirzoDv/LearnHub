using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface IProgressService
{
    Task<IReadOnlyDictionary<int, CourseProgress>> GetForCoursesAsync(string userId, IReadOnlyCollection<int> courseIds, CancellationToken cancellationToken = default);

    Task<CourseProgress> GetForCourseAsync(string userId, int courseId, CancellationToken cancellationToken = default);

    /// <summary>Sets <see cref="CourseCardViewModel.Progress"/> on the cards the user is enrolled in.</summary>
    Task AttachAsync(IEnumerable<CourseCardViewModel> cards, string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates the stored <see cref="Enrollment.CompletionPercentage"/> and <see cref="Enrollment.CompletedAt"/> of
    /// every enrolment in the course, or only of <paramref name="userId"/>'s enrolment. Call it after anything that
    /// changes a student's activity or which lessons and quizzes count towards the course.
    /// </summary>
    Task SyncAsync(int courseId, string? userId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress = (completed published lessons + passed quizzes) / (published lessons + available quizzes), where a quiz is
/// available when it is published and has questions. Calculated with one SQL query using correlated sub-queries.
/// </summary>
public sealed class ProgressService(ApplicationDbContext db, TimeProvider clock) : IProgressService
{
    public async Task<IReadOnlyDictionary<int, CourseProgress>> GetForCoursesAsync(
        string userId, IReadOnlyCollection<int> courseIds, CancellationToken cancellationToken = default)
    {
        if (courseIds.Count == 0)
        {
            return new Dictionary<int, CourseProgress>();
        }

        var ids = courseIds.Distinct().ToList();
        var rows = await db.Courses.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                Total = c.Resources.Count(r => r.IsPublished)
                    + c.Quizzes.Count(q => q.IsPublished && q.Questions.Any()),
                Completed = c.Resources.Count(r => r.IsPublished && r.Completions.Any(done => done.UserId == userId))
                    + c.Quizzes.Count(q => q.IsPublished && q.Questions.Any() && q.Attempts.Any(a => a.UserId == userId && a.Passed))
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => new CourseProgress(r.Completed, r.Total));
    }

    public async Task<CourseProgress> GetForCourseAsync(string userId, int courseId, CancellationToken cancellationToken = default)
    {
        var progress = await GetForCoursesAsync(userId, [courseId], cancellationToken);
        return progress.GetValueOrDefault(courseId, CourseProgress.Empty);
    }

    public async Task AttachAsync(IEnumerable<CourseCardViewModel> cards, string? userId, CancellationToken cancellationToken = default)
    {
        if (userId is null)
        {
            return;
        }

        var enrolledCards = cards.Where(card => card.IsEnrolled).ToList();
        var progress = await GetForCoursesAsync(userId, enrolledCards.Select(card => card.Id).ToList(), cancellationToken);
        foreach (var card in enrolledCards)
        {
            card.Progress = progress.GetValueOrDefault(card.Id, CourseProgress.Empty);
        }
    }

    public async Task SyncAsync(int courseId, string? userId = null, CancellationToken cancellationToken = default)
    {
        var enrollments = await db.Enrollments
            .Where(e => e.CourseId == courseId && (userId == null || e.UserId == userId))
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            return;
        }

        // One progress query per enrolment keeps a single definition of progress. A course-wide sync only follows an
        // administrator's content change, and courses here have tens of enrolments, not thousands.
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var enrollment in enrollments)
        {
            var progress = await GetForCourseAsync(enrollment.UserId, courseId, cancellationToken);
            enrollment.CompletionPercentage = progress.Percent;
            enrollment.CompletedAt = progress.IsCompleted ? enrollment.CompletedAt ?? now : null;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
