using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

public sealed class ResourcesController(IResourceManagementService resources) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminResourceQuery query, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        return View(await resources.GetListAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? courseId, CancellationToken cancellationToken) =>
        View(await resources.GetNewFormAsync(courseId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(ResourceFormViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await resources.CreateAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success($"Resource \"{model.Title.Trim()}\" added.");
                return RedirectToAction("Details", "Courses", new { id = model.CourseId });
            }

            AddError(result);
        }

        await resources.PopulateFormAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await resources.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ResourceFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            var result = await resources.UpdateAsync(id, model, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Success("Resource saved.");
                return RedirectToAction("Details", "Courses", new { id = model.CourseId });
            }

            AddError(result);
        }

        await resources.PopulateFormAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await resources.GetForDeleteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await resources.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Resource deleted.");
        return RedirectToAction("Details", "Courses", new { id = result.Value });
    }
}
