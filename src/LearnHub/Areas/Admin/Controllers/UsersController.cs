using LearnHub.Services;
using LearnHub.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Areas.Admin.Controllers;

public sealed class UsersController(IUserManagementService users) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminUserQuery query, CancellationToken cancellationToken)
    {
        ModelState.Clear();
        return View(await users.GetListAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var model = await users.GetDetailsAsync(id, CurrentUserId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SetRole(string id, string role) =>
        Report(await users.SetRoleAsync(id, role, CurrentUserId), $"Role changed to {role}.", id);

    [HttpPost]
    public async Task<IActionResult> Deactivate(string id) =>
        Report(await users.DeactivateAsync(id, CurrentUserId), "Account deactivated. The user is signed out within five minutes.", id);

    [HttpPost]
    public async Task<IActionResult> Reactivate(string id) =>
        Report(await users.ReactivateAsync(id), "Account reactivated.", id);

    [HttpGet]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var model = await users.GetForDeleteAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var result = await users.DeleteAsync(id, CurrentUserId);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            Failure(result.Error!);
            return RedirectToAction(nameof(Details), new { id });
        }

        Success("Account deleted.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Report(OperationResult result, string successMessage, string id)
    {
        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (result.Succeeded)
        {
            Success(successMessage);
        }
        else
        {
            Failure(result.Error!);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
