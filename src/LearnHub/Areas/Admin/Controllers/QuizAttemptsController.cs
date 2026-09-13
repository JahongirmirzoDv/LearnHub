using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

/// <summary>Quiz results submitted by students.</summary>
public sealed class QuizAttemptsController(IQuizManagementService quizzes, IQuizService results) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminAttemptQuery query, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        return View(await quizzes.GetAttemptsAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await results.GetResultAsync(id, CurrentUserId, isAdmin: true, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await quizzes.GetAttemptForDeleteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await quizzes.DeleteAttemptAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Quiz attempt deleted.");
        return RedirectToAction(nameof(Index));
    }
}
