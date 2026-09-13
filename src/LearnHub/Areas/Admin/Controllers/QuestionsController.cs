using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

public sealed class QuestionsController(IQuestionManagementService questions) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Create(int quizId, CancellationToken cancellationToken)
    {
        var model = await questions.GetNewAsync(quizId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(QuestionFormViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await questions.CreateAsync(model, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            Success("Question added.");
            return RedirectToAction("Details", "Quizzes", new { id = result.Value });
        }

        return await RedisplayAsync(model, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await questions.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, QuestionFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            var result = await questions.UpdateAsync(id, model, cancellationToken);
            if (result.IsNotFound)
            {
                return NotFound();
            }

            Success("Question saved.");
            return RedirectToAction("Details", "Quizzes", new { id = result.Value });
        }

        return await RedisplayAsync(model, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await questions.GetForDeleteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await questions.DeleteAsync(id, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        Success("Question deleted.");
        return RedirectToAction("Details", "Quizzes", new { id = result.Value });
    }

    private async Task<IActionResult> RedisplayAsync(QuestionFormViewModel model, CancellationToken cancellationToken)
    {
        var quizTitle = await questions.GetQuizTitleAsync(model.QuizId, cancellationToken);
        if (quizTitle is null)
        {
            return NotFound();
        }

        model.QuizTitle = quizTitle;
        return View(model.PadOptions());
    }
}
