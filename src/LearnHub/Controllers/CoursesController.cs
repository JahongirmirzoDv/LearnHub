using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.ViewModels.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers;

public sealed class CoursesController(ICourseCatalogService catalog, IEnrollmentService enrollments) : Controller
{
    /// <summary>Server-side search and filtering: <c>/Courses?q=sql&amp;categoryId=3&amp;difficulty=Beginner&amp;sort=Popular&amp;page=2</c>.</summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CourseSearchQuery query, CancellationToken cancellationToken)
    {
        // Unknown filter values in a hand-edited URL are simply ignored instead of being reported as errors.
        ModelState.Clear();
        return View(await catalog.SearchAsync(query, User.GetUserId(), cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var course = await catalog.GetDetailsAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken);
        return course is null ? NotFound() : View(course);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Enroll(int id, CancellationToken cancellationToken)
    {
        var result = await enrollments.EnrollAsync(User.GetRequiredUserId(), id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (result.Succeeded)
        {
            TempData.SetStatus("You're enrolled. Your route starts at the first lesson.");
        }
        else
        {
            TempData.SetStatus(result.Error!, StatusKind.Info);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> Leave(int id, CancellationToken cancellationToken)
    {
        var result = await enrollments.LeaveAsync(User.GetRequiredUserId(), id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        TempData.SetStatus("You left the course. Completed lessons are remembered if you enrol again.", StatusKind.Info);
        return RedirectToAction(nameof(Details), new { id });
    }
}
