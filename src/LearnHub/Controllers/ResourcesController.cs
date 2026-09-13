using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.ViewModels.Learning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers;

/// <summary>Lesson player. Guests may open preview resources; everything else needs an enrolment (or the Admin role).</summary>
public sealed class ResourcesController(ILearningResourceService resources) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await resources.GetForViewingAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken);
        return result.Access switch
        {
            ResourceAccess.Allowed => View(result.Model),
            ResourceAccess.RequiresLogin => Challenge(),
            ResourceAccess.RequiresEnrollment => RedirectToCourse(result.CourseId!.Value),
            _ => NotFound()
        };
    }

    /// <summary>Streams a PDF or image after the same access check, so private files cannot be fetched by URL guessing.</summary>
    [HttpGet]
    public async Task<IActionResult> Open(int id, bool download = false, CancellationToken cancellationToken = default)
    {
        var file = await resources.GetFileAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken);
        switch (file.Access)
        {
            case ResourceAccess.Allowed:
                Response.Headers.CacheControl = "private, max-age=3600";
                return PhysicalFile(file.PhysicalPath!, file.ContentType!, download ? file.FileName : null, enableRangeProcessing: true);
            case ResourceAccess.RequiresLogin:
                return Challenge();
            case ResourceAccess.RequiresEnrollment:
                return RedirectToCourse(file.CourseId!.Value);
            default:
                return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> ToggleComplete(int id, int? nextId, CancellationToken cancellationToken)
    {
        var result = await resources.ToggleCompletionAsync(id, User.GetRequiredUserId(), cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            TempData.SetStatus(result.Error!, StatusKind.Warning);
            return RedirectToAction(nameof(Details), new { id });
        }

        if (result.Value && nextId is int next)
        {
            TempData.SetStatus("Lesson complete. On to the next stop.");
            return RedirectToAction(nameof(Details), new { id = next });
        }

        TempData.SetStatus(result.Value ? "Lesson marked as complete." : "Lesson marked as not complete.", result.Value ? StatusKind.Success : StatusKind.Info);
        return RedirectToAction(nameof(Details), new { id });
    }

    private RedirectToActionResult RedirectToCourse(int courseId)
    {
        TempData.SetStatus("Enrol in this course to open its lessons. Free preview lessons are marked in the route.", StatusKind.Info);
        return RedirectToAction("Details", "Courses", new { id = courseId });
    }
}
