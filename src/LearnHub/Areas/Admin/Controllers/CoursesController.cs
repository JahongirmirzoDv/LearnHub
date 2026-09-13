using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

public sealed class CoursesController(ICourseManagementService courses, ILookupService lookups) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminCourseQuery query, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        return View(await courses.GetListAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await courses.GetDetailsAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(new CourseFormViewModel { CategoryOptions = await lookups.GetCategoriesAsync(cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Create(CourseFormViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await courses.CreateAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success($"Course \"{model.Title.Trim()}\" created. Add learning resources and a quiz next.");
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }

            AddError(result);
        }

        model.CategoryOptions = await lookups.GetCategoriesAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await courses.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CourseFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            var result = await courses.UpdateAsync(id, model, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Success("Course saved.");
                return RedirectToAction(nameof(Details), new { id });
            }

            AddError(result);
        }

        var stored = await courses.GetForEditAsync(id, cancellationToken);
        if (stored is null)
        {
            return NotFound();
        }

        model.ExistingThumbnailPath = stored.ExistingThumbnailPath;
        model.CategoryOptions = stored.CategoryOptions;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TogglePublished(int id, CancellationToken cancellationToken)
    {
        var result = await courses.TogglePublishedAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success(result.Value ? "Course published. Students can now find and enrol in it." : "Course moved to drafts and hidden from students.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await courses.GetForDeleteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await courses.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Course deleted together with its resources, quizzes and enrolments.");
        return RedirectToAction(nameof(Index));
    }
}
