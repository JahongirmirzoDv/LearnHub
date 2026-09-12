using System.Security.Claims;
using LearnHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LearnHub.Infrastructure;

/// <summary>Adds the user's full name to the authentication cookie so the navigation can greet them without a database query.</summary>
public sealed class LearnHubClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(ClaimsPrincipalExtensions.FullNameClaimType, user.FullName));
        return identity;
    }
}
