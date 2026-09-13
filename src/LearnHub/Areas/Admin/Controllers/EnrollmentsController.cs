using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

public sealed class EnrollmentsController(IEnrollmentService enrollments, ILookupService lookups) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminEnrollmentQuery query, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        return View(await enrollments.GetAdminListAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? courseId, CancellationToken cancellationToken) =>
        View(new EnrollmentCreateViewModel { CourseId = courseId, CourseOptions = await lookups.GetCoursesAsync(cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Create(EnrollmentCreateViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await enrollments.AdminEnrollAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Student enrolled.");
                return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
            }

            AddError(result);
        }

        model.CourseOptions = await lookups.GetCoursesAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await enrollments.GetForDeleteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await enrollments.RemoveAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Enrolment removed.");
        return RedirectToAction(nameof(Index));
    }
}
