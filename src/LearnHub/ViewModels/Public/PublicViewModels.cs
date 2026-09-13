using System.ComponentModel.DataAnnotations;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.ViewModels.Shared;

namespace LearnHub.ViewModels.Public;

public sealed record ErrorViewModel(int StatusCode, string Title, string Message, string? RequestId)
{
    public static ErrorViewModel For(int statusCode, string? requestId) => statusCode switch
    {
        404 => new(404, "This page isn't on the map", "The link may be out of date, or the course or lesson may have been removed.", null),
        403 => new(403, "You don't have access to this page", "Your account doesn't have permission to open it.", null),
        429 => new(429, "Too many attempts in a short time", "Wait a minute, then try again.", null),
        500 => new(500, "Something went wrong on our side", "The problem has been logged. Try again in a moment.", requestId),
        _ => new(statusCode, "The request couldn't be completed", "Go back and try again.", requestId)
    };
}

public sealed class HomeViewModel
{
    public IReadOnlyList<CourseCardViewModel> PopularCourses { get; init; } = [];

    public IReadOnlyList<CategoryOption> Categories { get; init; } = [];

    public int PublishedCourseCount { get; init; }

    public int LearnerCount { get; init; }

    public int ResourceCount { get; init; }

    public int QuizCount { get; init; }
}

public sealed class ContactViewModel
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(FieldLengths.PersonName, MinimumLength = 2, ErrorMessage = "Name must be between {2} and {1} characters.")]
    [Display(Name = "Full name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(FieldLengths.Email)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a subject.")]
    [StringLength(FieldLengths.ContactSubject, MinimumLength = 3, ErrorMessage = "Subject must be between {2} and {1} characters.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write a message.")]
    [StringLength(FieldLengths.ContactMessage, MinimumLength = 20, ErrorMessage = "Your message must be between {2} and {1} characters.")]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Honeypot: hidden from people, often filled in by spam bots.</summary>
    [Display(Name = "Leave this field empty")]
    public string? Website { get; set; }
}

public enum CourseSort
{
    [Display(Name = "Newest")]
    Newest,

    [Display(Name = "Most popular")]
    Popular,

    [Display(Name = "Title (A–Z)")]
    Title,

    [Display(Name = "Shortest first")]
    Shortest
}

/// <summary>Query-string model for <c>/Courses</c>; every value is optional.</summary>
public sealed class CourseSearchQuery
{
    [StringLength(SearchPattern.MaxTermLength)]
    [Display(Name = "Search courses")]
    public string? Q { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Display(Name = "Level")]
    public DifficultyLevel? Difficulty { get; set; }

    [Display(Name = "Sort by")]
    public CourseSort Sort { get; set; } = CourseSort.Newest;

    public int Page { get; set; } = 1;

    public bool HasFilters => !string.IsNullOrWhiteSpace(Q) || CategoryId.HasValue || Difficulty.HasValue;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["q"] = Q,
        ["categoryId"] = CategoryId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["difficulty"] = Difficulty?.ToString(),
        ["sort"] = Sort == CourseSort.Newest ? null : Sort.ToString()
    };
}

public sealed class CourseListViewModel
{
    public required CourseSearchQuery Query { get; init; }

    public required PagedResult<CourseCardViewModel> Results { get; init; }

    public IReadOnlyList<CategoryOption> Categories { get; init; } = [];
}

public sealed class CourseDetailsViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? LearningOutcomes { get; init; }

    public string InstructorName { get; init; } = string.Empty;

    public DifficultyLevel Difficulty { get; init; }

    public int DurationMinutes { get; init; }

    public string? ThumbnailPath { get; init; }

    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = CategoryIcons.Default;

    public bool IsPublished { get; init; }

    public DateTime UpdatedAt { get; init; }

    public int EnrollmentCount { get; init; }

    public IReadOnlyList<CourseResourceItem> Resources { get; set; } = [];

    public IReadOnlyList<CourseQuizItem> Quizzes { get; set; } = [];

    public IReadOnlyList<CourseCardViewModel> RelatedCourses { get; set; } = [];

    public bool IsEnrolled { get; set; }

    public DateTime? EnrolledAt { get; set; }

    public CourseProgress Progress { get; set; } = CourseProgress.Empty;

    /// <summary>True when an administrator views the page (full access without enrolling).</summary>
    public bool IsAdminView { get; set; }

    public bool CanAccessAllContent => IsEnrolled || IsAdminView;

    public int? NextResourceId { get; set; }
}

public sealed class CourseResourceItem
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Summary { get; init; }

    public ResourceType Type { get; init; }

    public int? EstimatedMinutes { get; init; }

    public bool IsPreview { get; init; }

    public bool IsCompleted { get; set; }
}

public sealed class CourseQuizItem
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int QuestionCount { get; init; }

    public int PassMarkPercent { get; init; }

    public int AttemptCount { get; set; }

    public int? BestScorePercent { get; set; }

    public bool HasPassed { get; set; }
}
