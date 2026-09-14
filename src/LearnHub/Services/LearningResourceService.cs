using LearnHub.Data;
using LearnHub.Models;
using LearnHub.Services.Content;
using LearnHub.Services.Storage;
using LearnHub.ViewModels.Learning;
using LearnHub.ViewModels.Shared;
using Microsoft.AspNetCore.Html;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

/// <summary>
/// Student-facing access to lessons. Every method enforces the same access rule; draft lessons and lessons of
/// unpublished courses exist only for administrators.
/// </summary>
public interface ILearningResourceService
{
    Task<ResourceViewResult> GetForViewingAsync(int id, string? userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<ResourceFileResult> GetFileAsync(int id, string? userId, bool isAdmin, CancellationToken cancellationToken = default);

    /// <summary>Marks the resource complete (or not complete). The value is the new completion state.</summary>
    Task<OperationResult<bool>> ToggleCompletionAsync(int id, string userId, CancellationToken cancellationToken = default);
}

public sealed class LearningResourceService(
    ApplicationDbContext db,
    IEnrollmentService enrollments,
    IProgressService progress,
    IFileStorageService storage,
    TimeProvider clock,
    ILogger<LearningResourceService> logger) : ILearningResourceService
{
    public async Task<ResourceViewResult> GetForViewingAsync(int id, string? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var resource = await db.LearningResources.AsNoTracking()
            .Where(r => r.Id == id && ((r.Course.IsPublished && r.IsPublished) || isAdmin))
            .Select(r => new
            {
                r.Id,
                r.CourseId,
                CourseTitle = r.Course.Title,
                r.Course.CategoryId,
                r.Title,
                r.Summary,
                r.Type,
                r.Body,
                r.Solution,
                r.ExternalUrl,
                r.FileName,
                r.FileSizeBytes,
                r.EstimatedMinutes,
                r.IsPreview,
                r.IsPublished
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (resource is null)
        {
            return new ResourceViewResult(ResourceAccess.NotFound, null, null);
        }

        var isEnrolled = userId is not null && await enrollments.IsEnrolledAsync(userId, resource.CourseId, cancellationToken);
        var access = Evaluate(resource.IsPreview, isEnrolled, isAdmin, userId);
        if (access != ResourceAccess.Allowed)
        {
            return new ResourceViewResult(access, resource.CourseId, null);
        }

        var outlineRows = await db.LearningResources.AsNoTracking()
            .Where(r => r.CourseId == resource.CourseId && (r.IsPublished || isAdmin))
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Select(r => new { r.Id, r.Title, r.Type, r.IsPreview, IsCompleted = r.Completions.Any(done => done.UserId == userId) })
            .ToListAsync(cancellationToken);

        var outline = outlineRows
            .Select(r => new ResourceOutlineItem(r.Id, r.Title, r.Type, r.IsCompleted, isEnrolled || isAdmin || r.IsPreview))
            .ToList();
        var index = outline.FindIndex(item => item.Id == id);

        VideoEmbed? video = null;
        if (resource.Type == ResourceType.Video)
        {
            VideoEmbedParser.TryParse(resource.ExternalUrl, out video);
        }

        string? externalHost = null;
        if (resource.Type == ResourceType.Link && Uri.TryCreate(resource.ExternalUrl, UriKind.Absolute, out var externalUri))
        {
            externalHost = externalUri.Host;
        }

        var courseProgress = CourseProgress.Empty;
        if (isEnrolled)
        {
            await enrollments.RecordAccessAsync(userId!, resource.CourseId, cancellationToken);
            courseProgress = await progress.GetForCourseAsync(userId!, resource.CourseId, cancellationToken);
        }

        var model = new ResourceDetailsViewModel
        {
            Id = resource.Id,
            CourseId = resource.CourseId,
            CourseTitle = resource.CourseTitle,
            CategoryId = resource.CategoryId,
            Title = resource.Title,
            Summary = resource.Summary,
            Type = resource.Type,
            EstimatedMinutes = resource.EstimatedMinutes,
            IsPreview = resource.IsPreview,
            IsPublished = resource.IsPublished,
            Body = resource.Type is ResourceType.Article or ResourceType.Exercise ? LessonContentRenderer.Render(resource.Body) : HtmlString.Empty,
            Solution = resource.Type == ResourceType.Exercise ? LessonContentRenderer.Render(resource.Solution) : HtmlString.Empty,
            HasSolution = resource.Type == ResourceType.Exercise && !string.IsNullOrWhiteSpace(resource.Solution),
            VideoProvider = video?.Provider,
            VideoEmbedUrl = video?.EmbedUrl,
            ExternalUrl = resource.Type == ResourceType.Link ? resource.ExternalUrl : video?.WatchUrl,
            ExternalHost = externalHost,
            FileName = resource.FileName,
            FileSizeBytes = resource.FileSizeBytes,
            CanTrackProgress = isEnrolled,
            IsCompleted = index >= 0 && outline[index].IsCompleted,
            Progress = courseProgress,
            Outline = outline,
            Previous = index > 0 ? outline[index - 1] : null,
            Next = index >= 0 && index < outline.Count - 1 ? outline[index + 1] : null
        };

        return new ResourceViewResult(ResourceAccess.Allowed, resource.CourseId, model);
    }

    public async Task<ResourceFileResult> GetFileAsync(int id, string? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var resource = await db.LearningResources.AsNoTracking()
            .Where(r => r.Id == id
                && (r.Type == ResourceType.Pdf || r.Type == ResourceType.Image)
                && ((r.Course.IsPublished && r.IsPublished) || isAdmin))
            .Select(r => new { r.CourseId, r.IsPreview, r.FilePath, r.FileContentType, r.FileName })
            .FirstOrDefaultAsync(cancellationToken);

        if (resource is null)
        {
            return new ResourceFileResult(ResourceAccess.NotFound, null, null, null, null);
        }

        var isEnrolled = userId is not null && await enrollments.IsEnrolledAsync(userId, resource.CourseId, cancellationToken);
        var access = Evaluate(resource.IsPreview, isEnrolled, isAdmin, userId);
        if (access != ResourceAccess.Allowed)
        {
            return new ResourceFileResult(access, resource.CourseId, null, null, null);
        }

        var physicalPath = storage.GetPhysicalPath(resource.FilePath);
        if (physicalPath is null || resource.FileContentType is null)
        {
            logger.LogWarning("File for resource {ResourceId} is missing from storage.", id);
            return new ResourceFileResult(ResourceAccess.NotFound, resource.CourseId, null, null, null);
        }

        return new ResourceFileResult(ResourceAccess.Allowed, resource.CourseId, physicalPath, resource.FileContentType, resource.FileName);
    }

    public async Task<OperationResult<bool>> ToggleCompletionAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var courseId = await db.LearningResources.AsNoTracking()
            .Where(r => r.Id == id && r.Course.IsPublished && r.IsPublished)
            .Select(r => (int?)r.CourseId)
            .FirstOrDefaultAsync(cancellationToken);

        if (courseId is null)
        {
            return OperationResult<bool>.NotFound();
        }

        if (!await enrollments.IsEnrolledAsync(userId, courseId.Value, cancellationToken))
        {
            return OperationResult<bool>.Failure("Enrol in this course to track your progress.");
        }

        var existing = await db.ResourceCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.LearningResourceId == id, cancellationToken);

        if (existing is null)
        {
            db.ResourceCompletions.Add(new ResourceCompletion
            {
                UserId = userId,
                LearningResourceId = id,
                CompletedAt = clock.GetUtcNow().UtcDateTime
            });
        }
        else
        {
            db.ResourceCompletions.Remove(existing);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Double click: the unique index already holds the completion, which is the desired state.
            logger.LogWarning(exception, "Completion toggle for resource {ResourceId} raced with another request.", id);
            db.ChangeTracker.Clear();
        }

        await progress.SyncAsync(courseId.Value, userId, cancellationToken);
        return OperationResult<bool>.Success(existing is null);
    }

    /// <summary>Admins and enrolled students see everything; guests and other students only see previews.</summary>
    private static ResourceAccess Evaluate(bool isPreview, bool isEnrolled, bool isAdmin, string? userId) =>
        isAdmin || isEnrolled || isPreview
            ? ResourceAccess.Allowed
            : userId is null ? ResourceAccess.RequiresLogin : ResourceAccess.RequiresEnrollment;
}
