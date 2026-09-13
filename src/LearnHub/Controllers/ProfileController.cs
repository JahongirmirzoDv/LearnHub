using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearnHub.Controllers;

[Authorize]
public sealed class ProfileController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<ProfileController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = new ProfileViewModel { FullName = user.FullName, Bio = user.Bio };
        await FillDisplayFieldsAsync(model, user);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await FillDisplayFieldsAsync(model, user);
            return View(model);
        }

        // Only the editable fields are copied; e-mail and roles can never be changed through this form.
        user.FullName = model.FullName.Trim();
        user.Bio = TextInput.Clean(model.Bio);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await FillDisplayFieldsAsync(model, user);
            return View(model);
        }

        // Re-issue the cookie so the navigation shows the new name immediately.
        await signInManager.RefreshSignInAsync(user);
        TempData.SetStatus("Profile saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Forms)]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                if (error.Code == "PasswordMismatch")
                {
                    ModelState.AddModelError(nameof(ChangePasswordViewModel.CurrentPassword), "Your current password is incorrect.");
                }
                else
                {
                    ModelState.AddModelError(nameof(ChangePasswordViewModel.NewPassword), error.Description);
                }
            }

            return View(model);
        }

        await signInManager.RefreshSignInAsync(user);
        logger.LogInformation("User {UserId} changed their password.", user.Id);
        TempData.SetStatus("Password changed.");
        return RedirectToAction(nameof(Index));
    }

    private async Task FillDisplayFieldsAsync(ProfileViewModel model, ApplicationUser user)
    {
        model.Email = user.Email ?? string.Empty;
        model.MemberSince = user.CreatedAt;
        model.Roles = (await userManager.GetRolesAsync(user)).OrderBy(role => role).ToList();
    }
}
