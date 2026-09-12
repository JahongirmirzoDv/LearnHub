using System.Security.Claims;
using LearnHub.Models;

namespace LearnHub.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public const string FullNameClaimType = "learnhub:full_name";

    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string GetRequiredUserId(this ClaimsPrincipal user) =>
        user.GetUserId() ?? throw new InvalidOperationException("The current request is not authenticated.");

    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(FullNameClaimType) ?? user.Identity?.Name ?? "Learner";

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(AppRoles.Admin);

    public static bool IsStudent(this ClaimsPrincipal user) => user.IsInRole(AppRoles.Student);
}
