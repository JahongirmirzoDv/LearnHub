using LearnHub.Models;

namespace LearnHub.ViewModels.Shared;

/// <summary>Calculated course progress: completed lessons + passed quizzes out of all lessons + available quizzes.</summary>
public sealed record CourseProgress(int CompletedItems, int TotalItems)
{
    public static CourseProgress Empty { get; } = new(0, 0);

    public int Percent => TotalItems == 0 ? 0 : (int)Math.Floor(CompletedItems * 100.0 / TotalItems);

    public bool IsCompleted => TotalItems > 0 && CompletedItems >= TotalItems;

    public bool IsStarted => CompletedItems > 0;
}

public sealed class CourseCardViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = CategoryIcons.Default;

    public DifficultyLevel Difficulty { get; init; }

    public int DurationMinutes { get; init; }

    public string? ThumbnailPath { get; init; }

    public string InstructorName { get; init; } = string.Empty;

    public int ResourceCount { get; init; }

    public int QuizCount { get; init; }

    public int EnrollmentCount { get; init; }

    public bool IsEnrolled { get; init; }

    /// <summary>Filled in for enrolled courses only.</summary>
    public CourseProgress? Progress { get; set; }
}

public sealed record CategoryOption(int Id, string Name, string IconName, int CourseCount);

public sealed record SelectOption(int Id, string Name);

public enum ActivityKind
{
    Enrolled,
    CompletedResource,
    PassedQuiz,
    AttemptedQuiz
}

/// <summary>A dashboard timeline entry; the view turns <see cref="Kind"/> and <see cref="TargetId"/> into a link.</summary>
public sealed record ActivityItem(DateTime OccurredAt, ActivityKind Kind, string Text, int TargetId);

/// <summary>Data for the shared <c>_Pagination</c> partial.</summary>
public sealed class PaginationViewModel
{
    public PaginationViewModel(int page, int totalPages, string action, IDictionary<string, string?>? routeValues = null)
    {
        Page = page;
        TotalPages = totalPages;
        Action = action;
        RouteValues = (routeValues ?? new Dictionary<string, string?>())
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value!);
    }

    public int Page { get; }

    public int TotalPages { get; }

    public string Action { get; }

    public IDictionary<string, string> RouteValues { get; }

    public IDictionary<string, string> ForPage(int page) =>
        new Dictionary<string, string>(RouteValues) { ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture) };
}
