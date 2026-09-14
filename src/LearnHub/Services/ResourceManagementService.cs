using LearnHub.Data;
using LearnHub.Models;
using LearnHub.Services.Content;
using LearnHub.Services.Storage;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface IResourceManagementService
{
    Task<AdminResourceListViewModel> GetListAsync(AdminResourceQuery query, CancellationToken cancellationToken = default);

    Task<ResourceFormViewModel> GetNewFormAsync(int? courseId, CancellationToken cancellationToken = default);

    Task<ResourceFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Fills the display-only properties of a form that is being shown again after a validation error.</summary>
    Task PopulateFormAsync(ResourceFormViewModel model, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(ResourceFormViewModel model, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(int id, ResourceFormViewModel model, CancellationToken cancellationToken = default);

    Task<ResourceDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the resource and its stored file. The value is the course id, for redirecting.</summary>
    Task<OperationResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ResourceManagementService(
    ApplicationDbContext db,
    ILookupService lookups,
    IFileStorageService storage,
    IProgressService progress,
    ILogger<ResourceManagementService> logger) : IResourceManagementService
{
    public const int PageSize = 20;

    public async Task<AdminResourceListViewModel> GetListAsync(AdminResourceQuery query, CancellationToken cancellationToken = default)
    {
        var resources = db.LearningResources.AsNoTracking();

        if (query.CourseId is int courseId)
        {
            resources = resources.Where(r => r.CourseId == courseId);
        }

        if (query.Type is ResourceType type)
        {
            resources = resources.Where(r => r.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = SearchPattern.Contains(query.Q);
            resources = resources.Where(r => EF.Functions.Like(r.Title, pattern, SearchPattern.EscapeCharacter));
        }

        var results = await resources
            .OrderBy(r => r.Course.Title).ThenBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => new AdminResourceListItem(
                r.Id, r.Title, r.Type, r.CourseId, r.Course.Title, r.SortOrder, r.IsPreview, r.IsPublished, r.Completions.Count, r.UpdatedAt))
            .ToPagedResultAsync(query.Page, PageSize, cancellationToken);

        query.Page = results.Page;
        return new AdminResourceListViewModel
        {
            Query = query,
            Results = results,
            Courses = await lookups.GetCoursesAsync(cancellationToken)
        };
    }

    public async Task<ResourceFormViewModel> GetNewFormAsync(int? courseId, CancellationToken cancellationToken = default)
    {
        var model = new ResourceFormViewModel { CourseId = courseId };
        if (courseId is int id)
        {
            var maxOrder = await db.LearningResources
                .Where(r => r.CourseId == id)
                .MaxAsync(r => (int?)r.SortOrder, cancellationToken);
            model.SortOrder = (maxOrder ?? 0) + 1;
        }

        await PopulateFormAsync(model, cancellationToken);
        return model;
    }

    public async Task<ResourceFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var model = await db.LearningResources.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ResourceFormViewModel
            {
                Id = r.Id,
                CourseId = r.CourseId,
                Title = r.Title,
                Summary = r.Summary,
                Type = r.Type,
                Body = r.Body,
                Solution = r.Solution,
                ExternalUrl = r.ExternalUrl,
                EstimatedMinutes = r.EstimatedMinutes,
                SortOrder = r.SortOrder,
                IsPreview = r.IsPreview,
                IsPublished = r.IsPublished,
                ExistingFileName = r.FileName,
                ExistingFileSizeBytes = r.FileSizeBytes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (model is not null)
        {
            await PopulateFormAsync(model, cancellationToken);
        }

        return model;
    }

    public async Task PopulateFormAsync(ResourceFormViewModel model, CancellationToken cancellationToken = default)
    {
        model.CourseOptions = await lookups.GetCoursesAsync(cancellationToken);
        if (model.Id is int id && model.ExistingFileName is null)
        {
            var file = await db.LearningResources.AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new { r.FileName, r.FileSizeBytes })
                .FirstOrDefaultAsync(cancellationToken);
            model.ExistingFileName = file?.FileName;
            model.ExistingFileSizeBytes = file?.FileSizeBytes;
        }
    }

    public async Task<OperationResult<int>> CreateAsync(ResourceFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == model.CourseId, cancellationToken))
        {
            return OperationResult<int>.Failure("The selected course no longer exists.");
        }

        var resource = new LearningResource();
        ApplyForm(resource, model);

        StoredFile? uploaded = null;
        if (model.RequiresFile)
        {
            var saved = await SaveUploadAsync(model, cancellationToken);
            if (!saved.Succeeded)
            {
                return OperationResult<int>.Failure(saved.Error!);
            }

            uploaded = saved.Value!;
            AttachFile(resource, uploaded);
        }

        db.LearningResources.Add(resource);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            storage.Delete(uploaded?.RelativePath);
            throw;
        }

        await progress.SyncAsync(resource.CourseId, cancellationToken: cancellationToken);
        logger.LogInformation("Resource {ResourceId} ({Type}) created in course {CourseId}.", resource.Id, resource.Type, resource.CourseId);
        return OperationResult<int>.Success(resource.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, ResourceFormViewModel model, CancellationToken cancellationToken = default)
    {
        var resource = await db.LearningResources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (resource is null)
        {
            return OperationResult.NotFound();
        }

        if (!await db.Courses.AnyAsync(c => c.Id == model.CourseId, cancellationToken))
        {
            return OperationResult.Failure("The selected course no longer exists.");
        }

        var previousFile = resource.FilePath;
        var previousCourseId = resource.CourseId;
        var existingFileFits = model.Type switch
        {
            ResourceType.Pdf => resource.FileContentType == "application/pdf",
            ResourceType.Image => resource.FileContentType?.StartsWith("image/", StringComparison.Ordinal) == true,
            _ => false
        };

        StoredFile? uploaded = null;
        if (model.RequiresFile && model.UploadFile is null && !existingFileFits)
        {
            return OperationResult.Failure(model.Type == ResourceType.Pdf
                ? "Please upload a PDF file for this resource."
                : "Please upload a JPG, PNG or WebP image for this resource.");
        }

        if (model.RequiresFile && model.UploadFile is not null)
        {
            var saved = await SaveUploadAsync(model, cancellationToken);
            if (!saved.Succeeded)
            {
                return OperationResult.Failure(saved.Error!);
            }

            uploaded = saved.Value!;
        }

        ApplyForm(resource, model);
        if (uploaded is not null)
        {
            AttachFile(resource, uploaded);
        }
        else if (!model.RequiresFile)
        {
            AttachFile(resource, null);
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

        if (previousFile is not null && previousFile != resource.FilePath)
        {
            storage.Delete(previousFile);
        }

        // Publishing, unpublishing or moving a lesson changes what counts towards progress.
        await progress.SyncAsync(resource.CourseId, cancellationToken: cancellationToken);
        if (previousCourseId != resource.CourseId)
        {
            await progress.SyncAsync(previousCourseId, cancellationToken: cancellationToken);
        }

        logger.LogInformation("Resource {ResourceId} updated.", id);
        return OperationResult.Success();
    }

    public async Task<ResourceDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await db.LearningResources.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ResourceDeleteViewModel(r.Id, r.Title, r.Type, r.CourseId, r.Course.Title, r.Completions.Count))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var resource = await db.LearningResources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (resource is null)
        {
            return OperationResult<int>.NotFound();
        }

        db.LearningResources.Remove(resource);
        await db.SaveChangesAsync(cancellationToken);
        storage.Delete(resource.FilePath);
        await progress.SyncAsync(resource.CourseId, cancellationToken: cancellationToken);

        logger.LogInformation("Resource {ResourceId} deleted from course {CourseId}.", id, resource.CourseId);
        return OperationResult<int>.Success(resource.CourseId);
    }

    private Task<OperationResult<StoredFile>> SaveUploadAsync(ResourceFormViewModel model, CancellationToken cancellationToken)
    {
        var kind = model.Type == ResourceType.Pdf ? UploadKind.ResourceDocument : UploadKind.ResourceImage;
        return model.UploadFile is null
            ? Task.FromResult(OperationResult<StoredFile>.Failure("Please choose a file to upload."))
            : storage.SaveAsync(model.UploadFile, kind, cancellationToken);
    }

    /// <summary>Copies the form onto the entity and clears the fields that do not apply to the chosen type.</summary>
    private static void ApplyForm(LearningResource resource, ResourceFormViewModel model)
    {
        resource.CourseId = model.CourseId!.Value;
        resource.Title = TextInput.Required(model.Title);
        resource.Summary = TextInput.Clean(model.Summary);
        resource.Type = model.Type;
        resource.EstimatedMinutes = model.EstimatedMinutes;
        resource.SortOrder = model.SortOrder;
        resource.IsPreview = model.IsPreview;
        resource.IsPublished = model.IsPublished;

        resource.Body = model.Type is ResourceType.Article or ResourceType.Exercise ? TextInput.Clean(model.Body) : null;
        resource.Solution = model.Type == ResourceType.Exercise ? TextInput.Clean(model.Solution) : null;
        resource.ExternalUrl = model.Type switch
        {
            // Store the canonical watch URL; the embed URL is always rebuilt from the parsed id.
            ResourceType.Video => VideoEmbedParser.TryParse(model.ExternalUrl, out var video) ? video.WatchUrl : null,
            ResourceType.Link => model.ExternalUrl?.Trim(),
            _ => null
        };
    }

    private static void AttachFile(LearningResource resource, StoredFile? file)
    {
        resource.FilePath = file?.RelativePath;
        resource.FileName = file?.FileName;
        resource.FileContentType = file?.ContentType;
        resource.FileSizeBytes = file?.SizeBytes;
    }
}
