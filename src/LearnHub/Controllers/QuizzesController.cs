using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.ViewModels.Learning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers;

[Authorize(Roles = AppRoles.Student)]
public sealed class QuizzesController(IQuizService quizzes) : Controller
{
    /// <summary>The quizzes of every course the student is enrolled in, with their best scores.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await quizzes.GetOverviewAsync(User.GetRequiredUserId(), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Take(int id, CancellationToken cancellationToken)
    {
        var result = await quizzes.GetQuizToTakeAsync(id, User.GetRequiredUserId(), cancellationToken);
        return result.Access switch
        {
            ResourceAccess.Allowed => View(result.Model),
            ResourceAccess.RequiresEnrollment => RedirectToCourse(result.CourseId!.Value),
            _ => NotFound()
        };
    }

    /// <summary>Grades the posted answers on the server; the browser never receives the correct answers beforehand.</summary>
    [HttpPost]
    [ActionName("Take")]
    public async Task<IActionResult> Submit(int id, QuizSubmissionModel submission, CancellationToken cancellationToken)
    {
        var result = await quizzes.SubmitAsync(id, User.GetRequiredUserId(), submission.Answers, submission.StartToken, cancellationToken);
        return result.Access switch
        {
            ResourceAccess.Allowed => RedirectToAction(nameof(Result), new { id = result.AttemptId }),
            ResourceAccess.RequiresEnrollment => RedirectToCourse(result.CourseId!.Value),
            _ => NotFound()
        };
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id, CancellationToken cancellationToken)
    {
        // Returns 404 (not 403) for other students' attempts so attempt ids cannot be probed.
        var model = await quizzes.GetResultAsync(id, User.GetRequiredUserId(), isAdmin: false, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken cancellationToken) =>
        View(await quizzes.GetHistoryAsync(User.GetRequiredUserId(), cancellationToken: cancellationToken));

    private RedirectToActionResult RedirectToCourse(int courseId)
    {
        TempData.SetStatus("Enrol in this course to take its quizzes.", StatusKind.Info);
        return RedirectToAction("Details", "Courses", new { id = courseId });
    }
}
