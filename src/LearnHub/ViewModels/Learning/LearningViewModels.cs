using LearnHub.Models;
using LearnHub.ViewModels.Shared;
using Microsoft.AspNetCore.Html;

namespace LearnHub.ViewModels.Learning;

public sealed class ResourceDetailsViewModel
{
    public int Id { get; init; }

    public int CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public int CategoryId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Summary { get; init; }

    public ResourceType Type { get; init; }

    public int? EstimatedMinutes { get; init; }

    public bool IsPreview { get; init; }

    /// <summary>Article body already encoded and formatted by <c>LessonContentRenderer</c>.</summary>
    public IHtmlContent Body { get; init; } = HtmlString.Empty;

    public string? VideoProvider { get; init; }

    public string? VideoEmbedUrl { get; init; }

    public string? ExternalUrl { get; init; }

    public string? ExternalHost { get; init; }

    public string? FileName { get; init; }

    public long? FileSizeBytes { get; init; }

    /// <summary>True when the student is enrolled and can mark the resource complete.</summary>
    public bool CanTrackProgress { get; init; }

    public bool IsCompleted { get; init; }

    public CourseProgress Progress { get; init; } = CourseProgress.Empty;

    public IReadOnlyList<ResourceOutlineItem> Outline { get; init; } = [];

    public ResourceOutlineItem? Previous { get; init; }

    public ResourceOutlineItem? Next { get; init; }
}

public sealed record ResourceOutlineItem(int Id, string Title, ResourceType Type, bool IsCompleted, bool IsAccessible);

public enum ResourceAccess
{
    Allowed,
    NotFound,
    RequiresLogin,
    RequiresEnrollment
}

public sealed record ResourceViewResult(ResourceAccess Access, int? CourseId, ResourceDetailsViewModel? Model);

public sealed record ResourceFileResult(ResourceAccess Access, int? CourseId, string? PhysicalPath, string? ContentType, string? FileName);

public sealed class TakeQuizViewModel
{
    public int QuizId { get; init; }

    public int CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int PassMarkPercent { get; init; }

    public int PreviousAttemptCount { get; init; }

    public int? BestScorePercent { get; init; }

    public IReadOnlyList<TakeQuizQuestion> Questions { get; init; } = [];
}

public sealed record TakeQuizQuestion(int Id, int Number, string Text, IReadOnlyList<TakeQuizOption> Options);

/// <summary>Deliberately has no "is correct" flag: correct answers never reach the browser before submission.</summary>
public sealed record TakeQuizOption(int Id, string Text);

/// <summary>Posted by the quiz form: <c>Answers[questionId] = selectedOptionId</c>. Unanswered questions are absent.</summary>
public sealed class QuizSubmissionModel
{
    public Dictionary<int, int> Answers { get; set; } = [];
}

public sealed record QuizTakeResult(ResourceAccess Access, int? CourseId, TakeQuizViewModel? Model);

public sealed record QuizSubmitResult(ResourceAccess Access, int? CourseId, int? AttemptId);

public sealed class QuizResultViewModel
{
    public int AttemptId { get; init; }

    public int QuizId { get; init; }

    public string QuizTitle { get; init; } = string.Empty;

    public int CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public DateTime SubmittedAt { get; init; }

    public int CorrectCount { get; init; }

    public int QuestionCount { get; init; }

    public int ScorePercent { get; init; }

    public bool Passed { get; init; }

    public int PassMarkPercent { get; init; }

    public bool CanRetake { get; init; }

    public IReadOnlyList<QuizResultQuestion> Questions { get; init; } = [];
}

public sealed record QuizResultQuestion(int Number, string Text, string? Explanation, bool IsCorrect, bool WasAnswered, IReadOnlyList<QuizResultOption> Options);

public sealed record QuizResultOption(string Text, bool IsCorrect, bool IsSelected);

public sealed record QuizAttemptSummary(int AttemptId, int QuizId, string QuizTitle, int CourseId, string CourseTitle, DateTime SubmittedAt, int ScorePercent, bool Passed);

public sealed class QuizHistoryViewModel
{
    public IReadOnlyList<QuizAttemptSummary> Attempts { get; init; } = [];

    public int PassedCount => Attempts.Count(a => a.Passed);

    public int AverageScore => Attempts.Count == 0 ? 0 : (int)Math.Round(Attempts.Average(a => a.ScorePercent));
}

public enum MyCoursesFilter
{
    All,
    InProgress,
    Completed
}

public sealed class EnrolledCourseViewModel
{
    public required CourseCardViewModel Course { get; init; }

    public DateTime EnrolledAt { get; init; }

    public DateTime? LastAccessedAt { get; init; }

    public CourseProgress Progress { get; set; } = CourseProgress.Empty;

    public int? NextResourceId { get; set; }

    public string? NextResourceTitle { get; set; }
}

public sealed class MyCoursesViewModel
{
    public MyCoursesFilter Filter { get; init; }

    public IReadOnlyList<EnrolledCourseViewModel> Courses { get; init; } = [];

    public int AllCount { get; init; }

    public int InProgressCount { get; init; }

    public int CompletedCount { get; init; }
}

public sealed class StudentDashboardViewModel
{
    public string FirstName { get; init; } = string.Empty;

    public int EnrolledCount { get; init; }

    public int CompletedCount { get; init; }

    public int InProgressCount { get; init; }

    public int AvailableCourseCount { get; init; }

    public int CompletedResourceCount { get; init; }

    public int QuizzesPassedCount { get; init; }

    public int? AverageQuizScore { get; init; }

    public int OverallProgressPercent { get; init; }

    public IReadOnlyList<EnrolledCourseViewModel> ContinueLearning { get; init; } = [];

    public IReadOnlyList<QuizAttemptSummary> RecentQuizResults { get; init; } = [];

    public IReadOnlyList<ActivityItem> RecentActivity { get; init; } = [];

    public IReadOnlyList<CourseCardViewModel> RecommendedCourses { get; init; } = [];
}
