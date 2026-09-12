using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Learning;
using LearnHub.ViewModels.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

public interface IUserManagementService
{
    Task<AdminUserListViewModel> GetListAsync(AdminUserQuery query, CancellationToken cancellationToken = default);

    Task<AdminUserDetailsViewModel?> GetDetailsAsync(string id, string currentUserId, CancellationToken cancellationToken = default);

    Task<OperationResult> SetRoleAsync(string id, string role, string currentUserId);

    Task<OperationResult> DeactivateAsync(string id, string currentUserId);

    Task<OperationResult> ReactivateAsync(string id);

    Task<UserDeleteViewModel?> GetForDeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(string id, string currentUserId);
}

/// <summary>
/// Admin user management built on Identity's <see cref="UserManager{TUser}"/>. Safety rules: an admin cannot
/// change, deactivate or delete their own account, and the last active administrator can never be removed.
/// </summary>
public sealed class UserManagementService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IProgressService progress,
    ILogger<UserManagementService> logger) : IUserManagementService
{
    public const int PageSize = 20;

    public async Task<AdminUserListViewModel> GetListAsync(AdminUserQuery query, CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = SearchPattern.Contains(query.Q);
            users = users.Where(u =>
                EF.Functions.Like(u.FullName, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(u.Email!, pattern, SearchPattern.EscapeCharacter));
        }

        if (query.Role is not null && AppRoles.All.Contains(query.Role))
        {
            var roleName = query.Role;
            users = users.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == roleName)));
        }
        else
        {
            query.Role = null;
        }

        var page = await users
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, u.Email, u.LockoutEnd, u.CreatedAt, EnrollmentCount = u.Enrollments.Count })
            .ToPagedResultAsync(query.Page, PageSize, cancellationToken);

        var ids = page.Items.Select(u => u.Id).ToList();
        var roles = await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                where ids.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var items = page.Items
            .Select(u => new AdminUserListItem(
                u.Id,
                u.FullName,
                u.Email ?? string.Empty,
                roles.Where(r => r.UserId == u.Id).Select(r => r.Name ?? string.Empty).OrderBy(name => name).ToList(),
                IsDeactivated(u.LockoutEnd),
                !IsDeactivated(u.LockoutEnd) && u.LockoutEnd > now,
                u.CreatedAt,
                u.EnrollmentCount))
            .ToList();

        query.Page = page.Page;
        return new AdminUserListViewModel
        {
            Query = query,
            Results = new PagedResult<AdminUserListItem>(items, page.Page, page.PageSize, page.TotalCount)
        };
    }

    public async Task<AdminUserDetailsViewModel?> GetDetailsAsync(string id, string currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);

        var enrollments = await db.Enrollments.AsNoTracking()
            .Where(e => e.UserId == id)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new { e.Id, e.CourseId, e.Course.Title, e.EnrolledAt })
            .ToListAsync(cancellationToken);
        var progressByCourse = await progress.GetForCoursesAsync(id, enrollments.Select(e => e.CourseId).ToList(), cancellationToken);

        var attempts = await db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.SubmittedAt)
            .Take(10)
            .Select(a => new QuizAttemptSummary(a.Id, a.QuizId, a.Quiz.Title, a.Quiz.CourseId, a.Quiz.Course.Title, a.SubmittedAt, a.ScorePercent, a.Passed))
            .ToListAsync(cancellationToken);

        return new AdminUserDetailsViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            Roles = roles.OrderBy(role => role).ToList(),
            IsAdmin = roles.Contains(AppRoles.Admin),
            IsDeactivated = user.IsDeactivated,
            LockoutEnd = user.LockoutEnd,
            IsCurrentUser = user.Id == currentUserId,
            CompletedResourceCount = await db.ResourceCompletions.CountAsync(c => c.UserId == id, cancellationToken),
            Enrollments = enrollments
                .Select(e => new AdminUserEnrollmentItem(e.Id, e.CourseId, e.Title, e.EnrolledAt, progressByCourse.GetValueOrDefault(e.CourseId, CourseProgress.Empty).Percent))
                .ToList(),
            RecentAttempts = attempts
        };
    }

    public async Task<OperationResult> SetRoleAsync(string id, string role, string currentUserId)
    {
        if (!AppRoles.All.Contains(role))
        {
            return OperationResult.Failure("Unknown role.");
        }

        if (id == currentUserId)
        {
            return OperationResult.Failure("You cannot change the role of your own account.");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return OperationResult.NotFound();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count == 1 && currentRoles[0] == role)
        {
            return OperationResult.Success();
        }

        if (currentRoles.Contains(AppRoles.Admin) && role != AppRoles.Admin && await IsLastActiveAdminAsync(user))
        {
            return OperationResult.Failure("This is the last active administrator. Promote another user first.");
        }

        var removed = await userManager.RemoveFromRolesAsync(user, currentRoles);
        var added = removed.Succeeded ? await userManager.AddToRoleAsync(user, role) : removed;
        if (!added.Succeeded)
        {
            return OperationResult.Failure(DescribeErrors(added));
        }

        // Changing the security stamp makes the user's existing cookie pick up the new role within minutes.
        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation("User {UserId} role changed to {Role} by {AdminId}.", id, role, currentUserId);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeactivateAsync(string id, string currentUserId)
    {
        if (id == currentUserId)
        {
            return OperationResult.Failure("You cannot deactivate your own account.");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return OperationResult.NotFound();
        }

        if (await userManager.IsInRoleAsync(user, AppRoles.Admin) && await IsLastActiveAdminAsync(user))
        {
            return OperationResult.Failure("This is the last active administrator and cannot be deactivated.");
        }

        await userManager.SetLockoutEnabledAsync(user, true);
        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
            return OperationResult.Failure(DescribeErrors(result));
        }

        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation("User {UserId} deactivated by {AdminId}.", id, currentUserId);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ReactivateAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return OperationResult.NotFound();
        }

        var result = await userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            return OperationResult.Failure(DescribeErrors(result));
        }

        await userManager.ResetAccessFailedCountAsync(user);
        logger.LogInformation("User {UserId} reactivated.", id);
        return OperationResult.Success();
    }

    public async Task<UserDeleteViewModel?> GetForDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.FullName, u.Email, Enrollments = u.Enrollments.Count, Attempts = u.QuizAttempts.Count })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var isAdmin = await db.UserRoles.AnyAsync(
            ur => ur.UserId == id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Admin), cancellationToken);
        return new UserDeleteViewModel(user.Id, user.FullName, user.Email ?? string.Empty, isAdmin, user.Enrollments, user.Attempts);
    }

    public async Task<OperationResult> DeleteAsync(string id, string currentUserId)
    {
        if (id == currentUserId)
        {
            return OperationResult.Failure("You cannot delete your own account.");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return OperationResult.NotFound();
        }

        if (await userManager.IsInRoleAsync(user, AppRoles.Admin) && await IsLastActiveAdminAsync(user))
        {
            return OperationResult.Failure("This is the last active administrator and cannot be deleted.");
        }

        // The database cascades the account to its enrolments, completions, quiz attempts and answers.
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return OperationResult.Failure(DescribeErrors(result));
        }

        logger.LogInformation("User {UserId} deleted by {AdminId}.", id, currentUserId);
        return OperationResult.Success();
    }

    private async Task<bool> IsLastActiveAdminAsync(ApplicationUser user)
    {
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        return admins.Count(admin => !admin.IsDeactivated && admin.Id != user.Id) == 0;
    }

    private static bool IsDeactivated(DateTimeOffset? lockoutEnd) =>
        lockoutEnd.HasValue && lockoutEnd.Value.UtcDateTime.Year >= 9999;

    private static string DescribeErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));
}
