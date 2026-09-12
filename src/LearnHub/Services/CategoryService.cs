using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Categories with their number of published courses (public catalogue filters).</summary>
    Task<IReadOnlyList<CategoryOption>> GetPublicOptionsAsync(CancellationToken cancellationToken = default);

    Task<CategoryFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<CategoryDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(CategoryFormViewModel model, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(int id, CategoryFormViewModel model, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class CategoryService(ApplicationDbContext db, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItem(
                c.Id, c.Name, c.Description, c.IconName, c.Courses.Count, c.Courses.Count(course => course.IsPublished), c.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategoryOption>> GetPublicOptionsAsync(CancellationToken cancellationToken = default) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption(c.Id, c.Name, c.IconName, c.Courses.Count(course => course.IsPublished)))
            .ToListAsync(cancellationToken);

    public async Task<CategoryFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryFormViewModel { Id = c.Id, Name = c.Name, Description = c.Description, IconName = c.IconName })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CategoryDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDeleteViewModel(c.Id, c.Name, c.Description, c.Courses.Count))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult<int>> CreateAsync(CategoryFormViewModel model, CancellationToken cancellationToken = default)
    {
        var name = model.Name.Trim();
        if (await NameExistsAsync(name, excludeId: null, cancellationToken))
        {
            return OperationResult<int>.Failure($"A category named \"{name}\" already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = TextInput.Clean(model.Description),
            IconName = model.IconName
        };
        db.Categories.Add(category);

        if (!await TrySaveAsync(cancellationToken))
        {
            return OperationResult<int>.Failure("The category could not be saved because the name is already in use.");
        }

        logger.LogInformation("Category {CategoryId} \"{CategoryName}\" created.", category.Id, category.Name);
        return OperationResult<int>.Success(category.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, CategoryFormViewModel model, CancellationToken cancellationToken = default)
    {
        var category = await db.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return OperationResult.NotFound();
        }

        var name = model.Name.Trim();
        if (await NameExistsAsync(name, id, cancellationToken))
        {
            return OperationResult.Failure($"A category named \"{name}\" already exists.");
        }

        category.Name = name;
        category.Description = TextInput.Clean(model.Description);
        category.IconName = model.IconName;

        if (!await TrySaveAsync(cancellationToken))
        {
            return OperationResult.Failure("The category could not be saved because the name is already in use.");
        }

        logger.LogInformation("Category {CategoryId} updated.", id);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await db.Categories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return OperationResult.NotFound();
        }

        // Mirrors the Restrict foreign key with a friendly message instead of a database error.
        var courseCount = await db.Courses.CountAsync(c => c.CategoryId == id, cancellationToken);
        if (courseCount > 0)
        {
            return OperationResult.Failure(
                $"\"{category.Name}\" still contains {courseCount} course{(courseCount == 1 ? string.Empty : "s")}. Move or delete those courses first.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Category {CategoryId} \"{CategoryName}\" deleted.", id, category.Name);
        return OperationResult.Success();
    }

    private Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        // Case-insensitive on every provider (SQLite compares case-sensitively by default).
        var normalized = name.ToUpperInvariant();
        return db.Categories.AnyAsync(c => c.Name.ToUpper() == normalized && c.Id != excludeId, cancellationToken);
    }

    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
        {
            // A concurrent request created the same name between the check and the insert (unique index).
            logger.LogWarning(exception, "Saving a category failed.");
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
