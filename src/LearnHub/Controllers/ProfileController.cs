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

        var model = new ProfileViewModel { FullName = user.FullName, Email = user.Email ?? string.Empty, Bio = user.Bio };
        await FillDisplayFieldsAsync(model, user);
        return View(model);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Forms)]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var email = model.Email.Trim();
        var emailChanged = ModelState.IsValid && !string.Equals(email, user.Email, StringComparison.Ordinal);
        if (emailChanged)
        {
            await ValidateEmailChangeAsync(user, email, model.CurrentPassword);
        }

        if (!ModelState.IsValid)
        {
            model.CurrentPassword = null;
            await FillDisplayFieldsAsync(model, user);
            return View(model);
        }

        // Only the editable fields are copied; roles can never be changed through this form.
        user.FullName = model.FullName.Trim();
        user.Bio = TextInput.Clean(model.Bio);
        if (emailChanged)
        {
            // The email address is also the sign-in name.
            user.Email = email;
            user.UserName = email;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var key = error.Code is "DuplicateEmail" or "DuplicateUserName" or "InvalidEmail" or "InvalidUserName"
                    ? nameof(ProfileViewModel.Email)
                    : string.Empty;
                ModelState.AddModelError(key, error.Description);
            }

            model.CurrentPassword = null;
            await FillDisplayFieldsAsync(model, user);
            return View(model);
        }

        if (emailChanged)
        {
            // A new security stamp signs the account out everywhere else.
            await userManager.UpdateSecurityStampAsync(user);
            logger.LogInformation("User {UserId} changed their email address.", user.Id);
        }

        // Re-issue the cookie so the navigation shows the new name and email immediately.
        await signInManager.RefreshSignInAsync(user);
        TempData.SetStatus(emailChanged ? "Profile saved. Use your new email address the next time you log in." : "Profile saved.");
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

    /// <summary>Changing the sign-in email needs the current password (failures count towards lockout) and a free address.</summary>
    private async Task ValidateEmailChangeAsync(ApplicationUser user, string email, string? currentPassword)
    {
        if (string.IsNullOrEmpty(currentPassword))
        {
            ModelState.AddModelError(nameof(ProfileViewModel.CurrentPassword), "Enter your current password to change your email address.");
            return;
        }

        var check = await signInManager.CheckPasswordSignInAsync(user, currentPassword, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            ModelState.AddModelError(nameof(ProfileViewModel.CurrentPassword), check.IsLockedOut
                ? "Too many incorrect attempts. Try again in 15 minutes."
                : "Your current password is incorrect.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is { } other && other.Id != user.Id)
        {
            ModelState.AddModelError(nameof(ProfileViewModel.Email), "Another account already uses this email address.");
        }
    }

    private async Task FillDisplayFieldsAsync(ProfileViewModel model, ApplicationUser user)
    {
        model.MemberSince = user.CreatedAt;
        model.Roles = (await userManager.GetRolesAsync(user)).OrderBy(role => role).ToList();
    }
}
