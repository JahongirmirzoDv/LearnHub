using Microsoft.AspNetCore.Identity;

namespace LearnHub.Models;

/// <summary>
/// A registered LearnHub member. Extends the Identity user (email, password hash, lockout …)
/// with profile information. Deactivation uses Identity lockout, so there is no separate flag.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string FullName { get; set; } = string.Empty;

    [PersonalData]
    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Enrollment> Enrollments { get; set; } = [];

    public ICollection<QuizAttempt> QuizAttempts { get; set; } = [];

    public ICollection<ResourceCompletion> ResourceCompletions { get; set; } = [];

    /// <summary>Accounts locked until the maximum date were deactivated by an administrator.</summary>
    public bool IsDeactivated => LockoutEnd.HasValue && LockoutEnd.Value.UtcDateTime.Year >= 9999;
}
