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

    /// <summary>Article or exercise text, already encoded and formatted by <c>LessonContentRenderer</c>.</summary>
    public IHtmlContent Body { get; init; } = HtmlString.Empty;

    /// <summary>Exercise solution, formatted like <see cref="Body"/>; empty for other types.</summary>
    public IHtmlContent Solution { get; init; } = HtmlString.Empty;

    public bool HasSolution { get; init; }

    /// <summary>Only administrators can open draft resources; the page shows them a notice.</summary>
    public bool IsPublished { get; init; } = true;

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

    /// <summary>Signed, time-limited token recording when the quiz was opened; posted back with the answers.</summary>
    public string StartToken { get; init; } = string.Empty;

    public int TotalPoints => Questions.Sum(q => q.Points);

    public IReadOnlyList<TakeQuizQuestion> Questions { get; init; } = [];
}

public sealed record TakeQuizQuestion(int Id, int Number, string Text, int Points, IReadOnlyList<TakeQuizOption> Options);

/// <summary>Deliberately has no "is correct" flag: correct answers never reach the browser before submission.</summary>
public sealed record TakeQuizOption(int Id, string Text);

/// <summary>Posted by the quiz form: <c>Answers[questionId] = selectedOptionId</c>. Unanswered questions are absent.</summary>
public sealed class QuizSubmissionModel
{
    public Dictionary<int, int> Answers { get; set; } = [];

    public string? StartToken { get; set; }
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

    public DateTime StartedAt { get; init; }

    public DateTime CompletedAt { get; init; }

    /// <summary>Null when the start time is unknown (for example the quiz page was open for more than a day).</summary>
    public TimeSpan? TimeTaken => CompletedAt > StartedAt ? CompletedAt - StartedAt : null;

    public int CorrectCount { get; init; }

    public int QuestionCount { get; init; }

    public int Score { get; init; }

    public int MaxScore { get; init; }

    public int ScorePercent { get; init; }

    public bool Passed { get; init; }

    public int PassMarkPercent { get; init; }

    public bool CanRetake { get; init; }

    public IReadOnlyList<QuizResultQuestion> Questions { get; init; } = [];
}

public sealed record QuizResultQuestion(int Number, string Text, string? Explanation, int Points, bool IsCorrect, bool WasAnswered, IReadOnlyList<QuizResultOption> Options);

public sealed record QuizResultOption(string Text, bool IsCorrect, bool IsSelected);

public sealed record QuizAttemptSummary(int AttemptId, int QuizId, string QuizTitle, int CourseId, string CourseTitle, DateTime CompletedAt, int ScorePercent, bool Passed);

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

/// <summary>A quiz the student can take, with their own results so far.</summary>
public sealed record StudentQuizItem(
    int QuizId,
    string Title,
    string? Description,
    int CourseId,
    string CourseTitle,
    int QuestionCount,
    int TotalPoints,
    int PassMarkPercent,
    int AttemptCount,
    int? BestScorePercent,
    bool Passed,
    int? LatestAttemptId);

public sealed class StudentQuizzesViewModel
{
    public IReadOnlyList<StudentQuizItem> Quizzes { get; init; } = [];

    public IReadOnlyList<QuizAttemptSummary> RecentAttempts { get; init; } = [];

    public int EnrolledCourseCount { get; init; }

    public int PassedCount => Quizzes.Count(q => q.Passed);

    public int NotAttemptedCount => Quizzes.Count(q => q.AttemptCount == 0);
}

public sealed class CourseProgressReport
{
    public int CourseId { get; init; }

    public string CourseTitle { get; init; } = string.Empty;

    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public DateTime EnrolledAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public int LessonsCompleted { get; init; }

    public int LessonsTotal { get; init; }

    public IReadOnlyList<StudentQuizItem> Quizzes { get; init; } = [];

    public int QuizzesPassed => Quizzes.Count(q => q.Passed);

    public CourseProgress Progress => new(LessonsCompleted + QuizzesPassed, LessonsTotal + Quizzes.Count);
}

public sealed class StudentProgressViewModel
{
    public IReadOnlyList<CourseProgressReport> Courses { get; init; } = [];

    public int CompletedCourseCount => Courses.Count(c => c.Progress.IsCompleted);

    public int LessonsCompleted => Courses.Sum(c => c.LessonsCompleted);

    public int LessonsTotal => Courses.Sum(c => c.LessonsTotal);

    public int QuizzesPassed => Courses.Sum(c => c.QuizzesPassed);

    public int QuizzesTotal => Courses.Sum(c => c.Quizzes.Count);

    public CourseProgress Overall => new(LessonsCompleted + QuizzesPassed, LessonsTotal + QuizzesTotal);
}
