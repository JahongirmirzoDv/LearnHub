using System.Diagnostics;
using LearnHub.ViewModels.Public;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Controllers;

/// <summary>
/// Friendly error pages. <c>UseExceptionHandler("/Error")</c> re-executes unhandled exceptions here (the
/// middleware logs them), and <c>UseStatusCodePagesWithReExecute("/Error/{0}")</c> handles 404, 403, 429 …
/// Stack traces are never shown outside the Development environment.
/// </summary>
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErrorController : Controller
{
    [Route("Error")]
    public IActionResult ServerError()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Status", ErrorViewModel.For(StatusCodes.Status500InternalServerError, RequestId()));
    }

    [Route("Error/{statusCode:int}")]
    public IActionResult Status(int statusCode)
    {
        Response.StatusCode = statusCode is >= 400 and <= 599 ? statusCode : StatusCodes.Status404NotFound;
        return View("Status", ErrorViewModel.For(Response.StatusCode, RequestId()));
    }

    private string RequestId() => Activity.Current?.Id ?? HttpContext.TraceIdentifier;
}
