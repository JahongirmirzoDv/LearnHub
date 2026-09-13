using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

public sealed class QuizzesController(IQuizManagementService quizzes, ILookupService lookups) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminQuizQuery query, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        return View(await quizzes.GetListAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await quizzes.GetDetailsAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? courseId, CancellationToken cancellationToken) =>
        View(new QuizFormViewModel { CourseId = courseId, CourseOptions = await lookups.GetCoursesAsync(cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Create(QuizFormViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await quizzes.CreateAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Quiz created as a draft. Add questions, then publish it.");
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }

            AddError(result);
        }

        model.CourseOptions = await lookups.GetCoursesAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await quizzes.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, QuizFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            var result = await quizzes.UpdateAsync(id, model, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            if (result.Succeeded)
            {
                Success("Quiz saved.");
                return RedirectToAction(nameof(Details), new { id });
            }

            AddError(result);
        }

        model.CourseOptions = await lookups.GetCoursesAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SetPublished(int id, bool isPublished, CancellationToken cancellationToken)
    {
        var result = await quizzes.SetPublishedAsync(id, isPublished, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (result.Succeeded)
        {
            Success(isPublished ? "Quiz published. Enrolled students can take it now." : "Quiz unpublished.");
        }
        else
        {
            Failure(result.Error!);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await quizzes.GetForDeleteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await quizzes.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Quiz deleted together with its questions and attempts.");
        return RedirectToAction("Details", "Courses", new { id = result.Value });
    }
}
