using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

/// <summary>
/// Base class for every admin controller. Authorisation lives here once, so a new admin page cannot be
/// added without the Admin role requirement.
/// </summary>
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminControllerBase : Controller
{
    protected string CurrentUserId => User.GetRequiredUserId();

    protected void Success(string message) => TempData.SetStatus(message);

    protected void Failure(string message) => TempData.SetStatus(message, StatusKind.Danger);

    /// <summary>Adds a failed service result to ModelState so the form is shown again with the message.</summary>
    protected void AddError(OperationResult result) =>
        ModelState.AddModelError(string.Empty, result.Error ?? "The change could not be saved.");
}
