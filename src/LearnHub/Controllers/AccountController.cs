using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearnHub.Controllers;

/// <summary>Registration, login and logout using ASP.NET Core Identity (passwords are hashed by Identity).</summary>
public sealed class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    TimeProvider clock,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Forms)]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await userManager.AddToRoleAsync(user, AppRoles.Student);
        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogInformation("Student account {UserId} registered.", user.Id);

        TempData.SetStatus($"Welcome to LearnHub, {DisplayFormat.StudentFirstName(user.FullName)}. Pick a course to start your first route.");
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Forms)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var result = await signInManager.PasswordSignInAsync(email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User {Email} signed in.", email);
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            var user = await userManager.FindByNameAsync(email);
            var isAdmin = user is not null && await userManager.IsInRoleAsync(user, AppRoles.Admin);
            return isAdmin
                ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
                : RedirectToAction("Dashboard", "Student");
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Sign-in blocked for locked account {Email}.", email);
            // One message for temporary lockout and deactivation, so the response reveals as little as possible.
            ModelState.AddModelError(string.Empty,
                "This account is locked. Try again in 15 minutes, or contact the LearnHub team if it has been deactivated.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "The email address or password is incorrect.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        TempData.SetStatus("You are logged out.", StatusKind.Info);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        var duplicateReported = false;
        foreach (var error in result.Errors)
        {
            if (error.Code is "DuplicateUserName" or "DuplicateEmail")
            {
                // Identity reports both codes for the same e-mail address; show a single message.
                if (!duplicateReported)
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.Email), "An account with this email address already exists. Log in instead.");
                    duplicateReported = true;
                }

                continue;
            }

            var key = error.Code.StartsWith("Password", StringComparison.Ordinal) ? nameof(RegisterViewModel.Password) : string.Empty;
            ModelState.AddModelError(key, error.Description);
        }
    }

    /// <summary>Only local return URLs are followed, which prevents open-redirect attacks.</summary>
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return User.IsAdmin()
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Dashboard", "Student");
    }
}
