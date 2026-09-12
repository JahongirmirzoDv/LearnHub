using LearnHub.Data;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

/// <summary>Drop-down options shared by several admin forms and filters.</summary>
public interface ILookupService
{
    Task<IReadOnlyList<SelectOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelectOption>> GetCoursesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelectOption>> GetQuizzesAsync(CancellationToken cancellationToken = default);
}

public sealed class LookupService(ApplicationDbContext db) : ILookupService
{
    public async Task<IReadOnlyList<SelectOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectOption(c.Id, c.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SelectOption>> GetCoursesAsync(CancellationToken cancellationToken = default) =>
        await db.Courses.AsNoTracking()
            .OrderBy(c => c.Title)
            .Select(c => new SelectOption(c.Id, c.Title))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SelectOption>> GetQuizzesAsync(CancellationToken cancellationToken = default) =>
        await db.Quizzes.AsNoTracking()
            .OrderBy(q => q.Course.Title).ThenBy(q => q.Title)
            .Select(q => new SelectOption(q.Id, q.Course.Title + " — " + q.Title))
            .ToListAsync(cancellationToken);
}
