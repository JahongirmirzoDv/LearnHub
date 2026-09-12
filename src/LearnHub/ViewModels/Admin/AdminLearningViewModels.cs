using System.ComponentModel.DataAnnotations;
using System.Globalization;
using LearnHub.Models;
using LearnHub.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LearnHub.ViewModels.Admin;

// ---------------------------------------------------------------- Quizzes

public sealed class AdminQuizQuery
{
    [StringLength(100)]
    public string? Q { get; set; }

    public int? CourseId { get; set; }

    public int Page { get; set; } = 1;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["q"] = Q,
        ["courseId"] = CourseId?.ToString(CultureInfo.InvariantCulture)
    };
}

public sealed record AdminQuizListItem(
    int Id,
    string Title,
    int CourseId,
    string CourseTitle,
    int QuestionCount,
    int AttemptCount,
    int PassMarkPercent,
    bool IsPublished,
    DateTime UpdatedAt);

public sealed class AdminQuizListViewModel
{
    public required AdminQuizQuery Query { get; init; }

    public required PagedResult<AdminQuizListItem> Results { get; init; }

    public IReadOnlyList<SelectOption> Courses { get; init; } = [];
}

public sealed class QuizFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please choose a course.")]
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    [Required(ErrorMessage = "Please enter a quiz title.")]
    [StringLength(FieldLengths.QuizTitle, MinimumLength = 3, ErrorMessage = "Title must be between {2} and {1} characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.QuizDescription, ErrorMessage = "Description can be at most {1} characters.")]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [Range(0, 100, ErrorMessage = "Pass mark must be between {1} and {2} percent.")]
    [Display(Name = "Pass mark (%)")]
    public int PassMarkPercent { get; set; } = 70;

    [Display(Name = "Publish this quiz (students can take it)")]
    public bool IsPublished { get; set; }

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<SelectOption> CourseOptions { get; set; } = [];
}

public sealed class AdminQuizDetailsViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public int PassMarkPercent { get; init; }

    public bool IsPublished { get; init; }

    public int AttemptCount { get; init; }

    public int? AverageScorePercent { get; init; }

    public DateTime UpdatedAt { get; init; }

    public IReadOnlyList<AdminQuestionItem> Questions { get; init; } = [];
}

public sealed record AdminQuestionItem(int Id, int SortOrder, string Text, string? Explanation, int AnswerCount, IReadOnlyList<AdminOptionItem> Options);

public sealed record AdminOptionItem(int Id, string Text, bool IsCorrect);

public sealed record QuizDeleteViewModel(int Id, string Title, int CourseId, string CourseTitle, int QuestionCount, int AttemptCount);

public sealed class AnswerOptionInput
{
    /// <summary>Existing option id when editing; verified by the service to belong to the question.</summary>
    public int? Id { get; set; }

    [StringLength(FieldLengths.AnswerOptionText, ErrorMessage = "An answer can be at most {1} characters.")]
    public string? Text { get; set; }
}

public sealed class QuestionFormViewModel : IValidatableObject
{
    public const int MinOptions = 2;
    public const int MaxOptions = 6;

    public int? Id { get; set; }

    public int QuizId { get; set; }

    [BindNever]
    [ValidateNever]
    public string QuizTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the question.")]
    [StringLength(FieldLengths.QuestionText, MinimumLength = 5, ErrorMessage = "Question must be between {2} and {1} characters.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Question")]
    public string Text { get; set; } = string.Empty;

    [StringLength(FieldLengths.QuestionExplanation, ErrorMessage = "Explanation can be at most {1} characters.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Explanation (shown to students after they submit)")]
    public string? Explanation { get; set; }

    [Range(0, 999, ErrorMessage = "Order must be between {1} and {2}.")]
    [Display(Name = "Order in quiz")]
    public int SortOrder { get; set; }

    public List<AnswerOptionInput> Options { get; set; } = [];

    [Display(Name = "Correct answer")]
    public int? CorrectOptionIndex { get; set; }

    /// <summary>Ensures the form always renders <see cref="MaxOptions"/> answer rows.</summary>
    public QuestionFormViewModel PadOptions()
    {
        while (Options.Count < MaxOptions)
        {
            Options.Add(new AnswerOptionInput());
        }

        return this;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Options.Count > MaxOptions)
        {
            yield return new ValidationResult($"A question can have at most {MaxOptions} answers.", [nameof(Options)]);
            yield break;
        }

        var filled = Options
            .Select((option, index) => (option, index))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.option.Text))
            .ToList();

        if (filled.Count < MinOptions)
        {
            yield return new ValidationResult($"Please provide at least {MinOptions} answers.", [nameof(Options)]);
        }

        if (CorrectOptionIndex is null || filled.All(pair => pair.index != CorrectOptionIndex))
        {
            yield return new ValidationResult("Select which of the filled-in answers is correct.", [nameof(CorrectOptionIndex)]);
        }

        var duplicates = filled
            .GroupBy(pair => pair.option.Text!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);
        if (duplicates)
        {
            yield return new ValidationResult("Each answer must be different.", [nameof(Options)]);
        }
    }
}

public sealed record QuestionDeleteViewModel(int Id, int QuizId, string QuizTitle, string Text, int AnswerCount);

// ---------------------------------------------------------------- Quiz attempts (results)

public sealed class AdminAttemptQuery
{
    [StringLength(100)]
    public string? Q { get; set; }

    public int? QuizId { get; set; }

    public int Page { get; set; } = 1;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["q"] = Q,
        ["quizId"] = QuizId?.ToString(CultureInfo.InvariantCulture)
    };
}

public sealed record AdminAttemptListItem(
    int Id,
    string StudentName,
    string StudentEmail,
    int QuizId,
    string QuizTitle,
    string CourseTitle,
    int ScorePercent,
    bool Passed,
    DateTime SubmittedAt);

public sealed class AdminAttemptListViewModel
{
    public required AdminAttemptQuery Query { get; init; }

    public required PagedResult<AdminAttemptListItem> Results { get; init; }

    public IReadOnlyList<SelectOption> Quizzes { get; init; } = [];
}

public sealed record AttemptDeleteViewModel(int Id, string StudentName, string QuizTitle, int ScorePercent, DateTime SubmittedAt);

// ---------------------------------------------------------------- Enrolments

public sealed class AdminEnrollmentQuery
{
    [StringLength(100)]
    public string? Q { get; set; }

    public int? CourseId { get; set; }

    public int Page { get; set; } = 1;

    public IDictionary<string, string?> ToRouteValues() => new Dictionary<string, string?>
    {
        ["q"] = Q,
        ["courseId"] = CourseId?.ToString(CultureInfo.InvariantCulture)
    };
}

public sealed record AdminEnrollmentListItem(
    int Id,
    string UserId,
    string StudentName,
    string StudentEmail,
    int CourseId,
    string CourseTitle,
    DateTime EnrolledAt,
    DateTime? LastAccessedAt,
    int ProgressPercent);

public sealed class AdminEnrollmentListViewModel
{
    public required AdminEnrollmentQuery Query { get; init; }

    public required PagedResult<AdminEnrollmentListItem> Results { get; init; }

    public IReadOnlyList<SelectOption> Courses { get; init; } = [];
}

public sealed class EnrollmentCreateViewModel
{
    [Required(ErrorMessage = "Please enter the student's email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Student email")]
    public string StudentEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a course.")]
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<SelectOption> CourseOptions { get; set; } = [];
}

public sealed record EnrollmentDeleteViewModel(int Id, string StudentName, string StudentEmail, string CourseTitle, DateTime EnrolledAt);
