using LearnHub.Data;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface IProgressService
{
    Task<IReadOnlyDictionary<int, CourseProgress>> GetForCoursesAsync(string userId, IReadOnlyCollection<int> courseIds, CancellationToken cancellationToken = default);

    Task<CourseProgress> GetForCourseAsync(string userId, int courseId, CancellationToken cancellationToken = default);

    /// <summary>Sets <see cref="CourseCardViewModel.Progress"/> on the cards the user is enrolled in.</summary>
    Task AttachAsync(IEnumerable<CourseCardViewModel> cards, string? userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress = (completed resources + passed quizzes) / (all resources + available quizzes).
/// Calculated with one SQL query per call using correlated sub-queries, never stored.
/// </summary>
public sealed class ProgressService(ApplicationDbContext db) : IProgressService
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
                Total = c.Resources.Count
                    + c.Quizzes.Count(q => q.IsPublished && q.Questions.Any()),
                Completed = c.Resources.Count(r => r.Completions.Any(done => done.UserId == userId))
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
}
