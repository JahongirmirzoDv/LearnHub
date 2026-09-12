using System.ComponentModel.DataAnnotations;
using System.Globalization;
using LearnHub.ViewModels.Learning;
using LearnHub.ViewModels.Shared;

namespace LearnHub.ViewModels.Admin;

// ---------------------------------------------------------------- Users

public sealed class AdminUserQuery
{
    [StringLength(100)]
    public string? Q { get; set; }

    /// <summary>"Admin", "Student" or empty for everyone.</summary>
    public string? Role { get; set; }

    public int Page { get; set; } = 1;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["q"] = Q,
        ["role"] = Role
    };
}

public sealed record AdminUserListItem(
    string Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsDeactivated,
    bool IsLockedOut,
    DateTime CreatedAt,
    int EnrollmentCount);

public sealed class AdminUserListViewModel
{
    public required AdminUserQuery Query { get; init; }

    public required PagedResult<AdminUserListItem> Results { get; init; }
}

public sealed class AdminUserDetailsViewModel
{
    public string Id { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Bio { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsAdmin { get; init; }

    public bool IsDeactivated { get; init; }

    public DateTimeOffset? LockoutEnd { get; init; }

    public bool IsTemporarilyLocked => !IsDeactivated && LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    public bool IsCurrentUser { get; init; }

    public int CompletedResourceCount { get; init; }

    public IReadOnlyList<AdminUserEnrollmentItem> Enrollments { get; init; } = [];

    public IReadOnlyList<QuizAttemptSummary> RecentAttempts { get; init; } = [];
}

public sealed record AdminUserEnrollmentItem(int EnrollmentId, int CourseId, string CourseTitle, DateTime EnrolledAt, int ProgressPercent);

public sealed record UserDeleteViewModel(string Id, string FullName, string Email, bool IsAdmin, int EnrollmentCount, int AttemptCount);

// ---------------------------------------------------------------- Contact messages

public sealed class AdminMessageQuery
{
    public bool UnreadOnly { get; set; }

    public int Page { get; set; } = 1;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["unreadOnly"] = UnreadOnly ? "true" : null,
        ["page"] = Page.ToString(CultureInfo.InvariantCulture)
    };
}

public sealed record ContactMessageListItem(int Id, string Name, string Email, string Subject, bool IsRead, DateTime CreatedAt);

public sealed class AdminMessageListViewModel
{
    public required AdminMessageQuery Query { get; init; }

    public required PagedResult<ContactMessageListItem> Results { get; init; }

    public int UnreadCount { get; init; }
}

public sealed record ContactMessageDetails(int Id, string Name, string Email, string Subject, string Message, bool IsRead, DateTime CreatedAt);
