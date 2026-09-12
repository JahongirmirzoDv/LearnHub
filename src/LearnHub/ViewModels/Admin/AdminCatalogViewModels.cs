using System.ComponentModel.DataAnnotations;
using System.Globalization;
using LearnHub.Infrastructure;
using LearnHub.Models;
using LearnHub.Services.Content;
using LearnHub.Services.Storage;
using LearnHub.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LearnHub.ViewModels.Admin;

public sealed class AdminDashboardViewModel
{
    public int PublishedCourseCount { get; init; }

    public int DraftCourseCount { get; init; }

    public int CategoryCount { get; init; }

    public int ResourceCount { get; init; }

    public int QuizCount { get; init; }

    public int StudentCount { get; init; }

    public int NewStudentsLast30Days { get; init; }

    public int EnrollmentCount { get; init; }

    public int EnrollmentsLast30Days { get; init; }

    public int AttemptCount { get; init; }

    public int? AverageScorePercent { get; init; }

    public int? PassRatePercent { get; init; }

    public int UnreadMessageCount { get; init; }

    public IReadOnlyList<RecentEnrollmentItem> RecentEnrollments { get; init; } = [];

    public IReadOnlyList<RecentAttemptItem> RecentAttempts { get; init; } = [];

    public IReadOnlyList<ChartBar> PopularCourses { get; init; } = [];

    public IReadOnlyList<ChartBar> EnrollmentsByCategory { get; init; } = [];

    public IReadOnlyList<ContentWarning> ContentWarnings { get; init; } = [];
}

public sealed record RecentEnrollmentItem(string StudentName, int CourseId, string CourseTitle, DateTime EnrolledAt);

public sealed record RecentAttemptItem(int AttemptId, string StudentName, string QuizTitle, int ScorePercent, bool Passed, DateTime SubmittedAt);

public sealed record ChartBar(string Label, int Value, int PercentOfMax);

public sealed record ContentWarning(string Message, string Controller, string Action, int Id);

// ---------------------------------------------------------------- Categories

public sealed record CategoryListItem(int Id, string Name, string? Description, string IconName, int CourseCount, int PublishedCourseCount, DateTime UpdatedAt);

public sealed class CategoryFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(FieldLengths.CategoryName, MinimumLength = 2, ErrorMessage = "Name must be between {2} and {1} characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(FieldLengths.CategoryDescription, ErrorMessage = "Description can be at most {1} characters.")]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose an icon.")]
    [Display(Name = "Icon")]
    public string IconName { get; set; } = CategoryIcons.Default;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CategoryIcons.IsAllowed(IconName))
        {
            yield return new ValidationResult("Please choose one of the available icons.", [nameof(IconName)]);
        }
    }
}

public sealed record CategoryDeleteViewModel(int Id, string Name, string? Description, int CourseCount);

// ---------------------------------------------------------------- Courses

public enum PublishStatusFilter
{
    All,
    Published,
    Draft
}

public sealed class AdminCourseQuery
{
    [StringLength(100)]
    public string? Q { get; set; }

    public int? CategoryId { get; set; }

    public PublishStatusFilter Status { get; set; }

    public int Page { get; set; } = 1;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["q"] = Q,
        ["categoryId"] = CategoryId?.ToString(CultureInfo.InvariantCulture),
        ["status"] = Status == PublishStatusFilter.All ? null : Status.ToString()
    };
}

public sealed record AdminCourseListItem(
    int Id,
    string Title,
    string CategoryName,
    DifficultyLevel Difficulty,
    bool IsPublished,
    int ResourceCount,
    int QuizCount,
    int EnrollmentCount,
    string? ThumbnailPath,
    DateTime UpdatedAt);

public sealed class AdminCourseListViewModel
{
    public required AdminCourseQuery Query { get; init; }

    public required PagedResult<AdminCourseListItem> Results { get; init; }

    public IReadOnlyList<SelectOption> Categories { get; init; } = [];
}

public sealed class CourseFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please enter a course title.")]
    [StringLength(FieldLengths.CourseTitle, MinimumLength = 5, ErrorMessage = "Title must be between {2} and {1} characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a short description.")]
    [StringLength(FieldLengths.CourseShortDescription, MinimumLength = 20, ErrorMessage = "Short description must be between {2} and {1} characters.")]
    [Display(Name = "Short description")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a full description.")]
    [StringLength(FieldLengths.CourseDescription, MinimumLength = 50, ErrorMessage = "Description must be between {2} and {1} characters.")]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [StringLength(FieldLengths.CourseLearningOutcomes, ErrorMessage = "Learning outcomes can be at most {1} characters.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "What students will learn (one per line)")]
    public string? LearningOutcomes { get; set; }

    [Required(ErrorMessage = "Please enter the instructor's name.")]
    [StringLength(FieldLengths.PersonName, MinimumLength = 2, ErrorMessage = "Instructor name must be between {2} and {1} characters.")]
    [Display(Name = "Instructor")]
    public string InstructorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Display(Name = "Level")]
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    [Range(10, 6000, ErrorMessage = "Duration must be between {1} and {2} minutes.")]
    [Display(Name = "Estimated duration (minutes)")]
    public int DurationMinutes { get; set; } = 60;

    [Display(Name = "Publish this course (visible to students)")]
    public bool IsPublished { get; set; }

    [MaxFileSize(UploadRules.MaxImageBytes)]
    [AllowedExtensions(UploadRules.ImageExtensions)]
    [Display(Name = "Cover image")]
    public IFormFile? ThumbnailFile { get; set; }

    [Display(Name = "Remove the current cover image")]
    public bool RemoveThumbnail { get; set; }

    [BindNever]
    [ValidateNever]
    public string? ExistingThumbnailPath { get; set; }

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<SelectOption> CategoryOptions { get; set; } = [];
}

public sealed class AdminCourseDetailsViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public DifficultyLevel Difficulty { get; init; }

    public int DurationMinutes { get; init; }

    public string InstructorName { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public string? ThumbnailPath { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public int EnrollmentCount { get; init; }

    public int AttemptCount { get; init; }

    public IReadOnlyList<AdminResourceListItem> Resources { get; init; } = [];

    public IReadOnlyList<AdminQuizListItem> Quizzes { get; init; } = [];

    public IReadOnlyList<RecentEnrollmentItem> RecentEnrollments { get; init; } = [];
}

public sealed record CourseDeleteViewModel(int Id, string Title, bool IsPublished, int ResourceCount, int QuizCount, int EnrollmentCount, int AttemptCount);

// ---------------------------------------------------------------- Learning resources

public sealed class AdminResourceQuery
{
    [StringLength(100)]
    public string? Q { get; set; }

    public int? CourseId { get; set; }

    public ResourceType? Type { get; set; }

    public int Page { get; set; } = 1;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["q"] = Q,
        ["courseId"] = CourseId?.ToString(CultureInfo.InvariantCulture),
        ["type"] = Type?.ToString()
    };
}

public sealed record AdminResourceListItem(
    int Id,
    string Title,
    ResourceType Type,
    int CourseId,
    string CourseTitle,
    int SortOrder,
    bool IsPreview,
    int CompletionCount,
    DateTime UpdatedAt);

public sealed class AdminResourceListViewModel
{
    public required AdminResourceQuery Query { get; init; }

    public required PagedResult<AdminResourceListItem> Results { get; init; }

    public IReadOnlyList<SelectOption> Courses { get; init; } = [];
}

public sealed class ResourceFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please choose a course.")]
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    [Required(ErrorMessage = "Please enter a title.")]
    [StringLength(FieldLengths.ResourceTitle, MinimumLength = 3, ErrorMessage = "Title must be between {2} and {1} characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.ResourceSummary, ErrorMessage = "Summary can be at most {1} characters.")]
    [Display(Name = "Summary (used as the description for images)")]
    public string? Summary { get; set; }

    [Display(Name = "Resource type")]
    public ResourceType Type { get; set; } = ResourceType.Article;

    [StringLength(FieldLengths.ResourceBody, ErrorMessage = "Lesson content can be at most {1} characters.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Lesson content")]
    public string? Body { get; set; }

    [StringLength(FieldLengths.Url, ErrorMessage = "The link can be at most {1} characters.")]
    [Display(Name = "Link")]
    public string? ExternalUrl { get; set; }

    [MaxFileSize(UploadRules.MaxDocumentBytes)]
    [AllowedExtensions(UploadRules.ResourceFileExtensions)]
    [Display(Name = "File")]
    public IFormFile? UploadFile { get; set; }

    [Range(1, 600, ErrorMessage = "Estimated time must be between {1} and {2} minutes.")]
    [Display(Name = "Estimated minutes")]
    public int? EstimatedMinutes { get; set; }

    [Range(0, 999, ErrorMessage = "Order must be between {1} and {2}.")]
    [Display(Name = "Order in course")]
    public int SortOrder { get; set; }

    [Display(Name = "Free preview (guests can open it without enrolling)")]
    public bool IsPreview { get; set; }

    [BindNever]
    [ValidateNever]
    public string? ExistingFileName { get; set; }

    [BindNever]
    [ValidateNever]
    public long? ExistingFileSizeBytes { get; set; }

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<SelectOption> CourseOptions { get; set; } = [];

    public bool RequiresFile => Type is ResourceType.Pdf or ResourceType.Image;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (Type)
        {
            case ResourceType.Article when string.IsNullOrWhiteSpace(Body) || Body.Trim().Length < 50:
                yield return new ValidationResult("Article lessons need at least 50 characters of content.", [nameof(Body)]);
                break;
            case ResourceType.Video when !VideoEmbedParser.TryParse(ExternalUrl, out _):
                yield return new ValidationResult(
                    "Enter a full YouTube or Vimeo link, e.g. https://www.youtube.com/watch?v=abc123XYZ00.", [nameof(ExternalUrl)]);
                break;
            case ResourceType.Link when !IsHttpsUrl(ExternalUrl):
                yield return new ValidationResult("Enter a full link starting with https://.", [nameof(ExternalUrl)]);
                break;
            case ResourceType.Image when string.IsNullOrWhiteSpace(Summary):
                yield return new ValidationResult("Describe the image in the summary so screen-reader users understand it.", [nameof(Summary)]);
                break;
        }

        if (RequiresFile && Id is null && UploadFile is null)
        {
            yield return new ValidationResult($"Please upload a {(Type == ResourceType.Pdf ? "PDF" : "JPG, PNG or WebP")} file.", [nameof(UploadFile)]);
        }

        if (UploadFile is not null)
        {
            var extension = Path.GetExtension(UploadFile.FileName);
            if (!RequiresFile)
            {
                yield return new ValidationResult("Files can only be uploaded for PDF and image resources.", [nameof(UploadFile)]);
            }
            else if (!UploadRules.Extensions(Type == ResourceType.Pdf ? UploadKind.ResourceDocument : UploadKind.ResourceImage).Contains(extension))
            {
                yield return new ValidationResult($"The uploaded file does not match the selected type ({DisplayFormat.ResourceTypeName(Type)}).", [nameof(UploadFile)]);
            }
            else if (Type == ResourceType.Image && UploadFile.Length > UploadRules.MaxImageBytes)
            {
                yield return new ValidationResult($"Images must be {DisplayFormat.FileSize(UploadRules.MaxImageBytes)} or smaller.", [nameof(UploadFile)]);
            }
        }
    }

    private static bool IsHttpsUrl(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host);
}

public sealed record ResourceDeleteViewModel(int Id, string Title, ResourceType Type, int CourseId, string CourseTitle, int CompletionCount);
