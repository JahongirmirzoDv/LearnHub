using System.Globalization;
using System.Security.Cryptography;
using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Learning;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

/// <summary>Student quiz flow: take a quiz, submit answers, review results and history.</summary>
public interface IQuizService
{
    Task<QuizTakeResult> GetQuizToTakeAsync(int quizId, string userId, CancellationToken cancellationToken = default);

    /// <param name="startToken">The token from <see cref="TakeQuizViewModel.StartToken"/>; without a valid one the start time is unknown.</param>
    Task<QuizSubmitResult> SubmitAsync(
        int quizId, string userId, IReadOnlyDictionary<int, int> answers, string? startToken = null, CancellationToken cancellationToken = default);

    /// <summary>Returns null unless the attempt belongs to <paramref name="userId"/> or the caller is an admin (prevents IDOR).</summary>
    Task<QuizResultViewModel?> GetResultAsync(int attemptId, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<QuizHistoryViewModel> GetHistoryAsync(string userId, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>Every quiz the student can take in the published courses they are enrolled in, with their results.</summary>
    Task<StudentQuizzesViewModel> GetOverviewAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class QuizService(
    ApplicationDbContext db,
    IEnrollmentService enrollments,
    IProgressService progress,
    IDataProtectionProvider dataProtection,
    TimeProvider clock,
    ILogger<QuizService> logger) : IQuizService
{
    // Long enough for any real attempt. An expired or altered token only means the start time is unknown.
    private static readonly TimeSpan StartTokenLifetime = TimeSpan.FromHours(24);

    private readonly ITimeLimitedDataProtector _startTokens =
        dataProtection.CreateProtector("LearnHub.QuizAttempt.StartedAt").ToTimeLimitedDataProtector();

    public async Task<QuizTakeResult> GetQuizToTakeAsync(int quizId, string userId, CancellationToken cancellationToken = default)
    {
        var quiz = await FindAvailableQuizAsync(quizId, cancellationToken);
        if (quiz is null)
        {
            return new QuizTakeResult(ResourceAccess.NotFound, null, null);
        }

        if (!await enrollments.IsEnrolledAsync(userId, quiz.CourseId, cancellationToken))
        {
            return new QuizTakeResult(ResourceAccess.RequiresEnrollment, quiz.CourseId, null);
        }

        var questions = await db.Questions.AsNoTracking()
            .Where(q => q.QuizId == quizId)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Id)
            .Select(q => new
            {
                q.Id,
                q.Text,
                q.Points,
                Options = q.Options
                    .OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
                    .Select(o => new TakeQuizOption(o.Id, o.Text))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var previousScores = await db.QuizAttempts.AsNoTracking()
            .Where(a => a.QuizId == quizId && a.UserId == userId)
            .Select(a => a.ScorePercent)
            .ToListAsync(cancellationToken);

        await enrollments.RecordAccessAsync(userId, quiz.CourseId, cancellationToken);

        var model = new TakeQuizViewModel
        {
            QuizId = quiz.Id,
            CourseId = quiz.CourseId,
            CourseTitle = quiz.CourseTitle,
            Title = quiz.Title,
            Description = quiz.Description,
            PassMarkPercent = quiz.PassMarkPercent,
            PreviousAttemptCount = previousScores.Count,
            BestScorePercent = previousScores.Count == 0 ? null : previousScores.Max(),
            StartToken = _startTokens.Protect(StartPayload(quizId, userId, clock.GetUtcNow().UtcDateTime.Ticks), StartTokenLifetime),
            Questions = questions
                .Select((q, index) => new TakeQuizQuestion(q.Id, index + 1, q.Text, q.Points, q.Options))
                .ToList()
        };

        return new QuizTakeResult(ResourceAccess.Allowed, quiz.CourseId, model);
    }

    public async Task<QuizSubmitResult> SubmitAsync(
        int quizId, string userId, IReadOnlyDictionary<int, int> answers, string? startToken = null, CancellationToken cancellationToken = default)
    {
        var quiz = await FindAvailableQuizAsync(quizId, cancellationToken);
        if (quiz is null)
        {
            return new QuizSubmitResult(ResourceAccess.NotFound, null, null);
        }

        if (!await enrollments.IsEnrolledAsync(userId, quiz.CourseId, cancellationToken))
        {
            return new QuizSubmitResult(ResourceAccess.RequiresEnrollment, quiz.CourseId, null);
        }

        var questions = await db.Questions.AsNoTracking()
            .Where(q => q.QuizId == quizId)
            .Select(q => new GradingQuestion(
                q.Id,
                q.Points,
                q.Options.Select(o => new GradingOption(o.Id, o.IsCorrect)).ToList()))
            .ToListAsync(cancellationToken);

        var graded = QuizGrader.Grade(questions, answers, quiz.PassMarkPercent);
        var completedAt = clock.GetUtcNow().UtcDateTime;

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            StartedAt = ReadStartedAt(startToken, quizId, userId, completedAt),
            CompletedAt = completedAt,
            CorrectCount = graded.CorrectCount,
            QuestionCount = graded.QuestionCount,
            Score = graded.Score,
            MaxScore = graded.MaxScore,
            ScorePercent = graded.ScorePercent,
            Passed = graded.Passed,
            Answers = graded.Answers
                .Select(a => new QuizAnswer { QuestionId = a.QuestionId, SelectedOptionId = a.SelectedOptionId, IsCorrect = a.IsCorrect })
                .ToList()
        };

        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);
        await progress.SyncAsync(quiz.CourseId, userId, cancellationToken);

        logger.LogInformation(
            "User {UserId} submitted quiz {QuizId}: {Score}/{MaxScore} points, {Percent}% ({Result}).",
            userId, quizId, attempt.Score, attempt.MaxScore, attempt.ScorePercent, attempt.Passed ? "passed" : "not passed");

        return new QuizSubmitResult(ResourceAccess.Allowed, quiz.CourseId, attempt.Id);
    }

    public async Task<QuizResultViewModel?> GetResultAsync(int attemptId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var attempt = await db.QuizAttempts.AsNoTracking()
            .Where(a => a.Id == attemptId && (isAdmin || a.UserId == userId))
            .Select(a => new
            {
                a.Id,
                a.QuizId,
                QuizTitle = a.Quiz.Title,
                a.Quiz.CourseId,
                CourseTitle = a.Quiz.Course.Title,
                StudentName = a.User.FullName,
                a.UserId,
                a.StartedAt,
                a.CompletedAt,
                a.CorrectCount,
                a.QuestionCount,
                a.Score,
                a.MaxScore,
                a.ScorePercent,
                a.Passed,
                a.Quiz.PassMarkPercent,
                QuizAvailable = a.Quiz.IsPublished && a.Quiz.Course.IsPublished && a.Quiz.Questions.Any()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (attempt is null)
        {
            return null;
        }

        var answers = await db.QuizAnswers.AsNoTracking()
            .Where(answer => answer.QuizAttemptId == attemptId)
            .Select(answer => new { answer.QuestionId, answer.SelectedOptionId, answer.IsCorrect })
            .ToDictionaryAsync(answer => answer.QuestionId, cancellationToken);

        var questions = await db.Questions.AsNoTracking()
            .Where(q => q.QuizId == attempt.QuizId)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Id)
            .Select(q => new
            {
                q.Id,
                q.Text,
                q.Explanation,
                q.Points,
                Options = q.Options
                    .OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
                    .Select(o => new { o.Id, o.Text, o.IsCorrect })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        // Only questions that existed when the attempt was made have an answer row.
        var reviewed = questions
            .Where(q => answers.ContainsKey(q.Id))
            .Select((q, index) =>
            {
                var answer = answers[q.Id];
                return new QuizResultQuestion(
                    index + 1,
                    q.Text,
                    q.Explanation,
                    q.Points,
                    answer.IsCorrect,
                    answer.SelectedOptionId.HasValue,
                    q.Options.Select(o => new QuizResultOption(o.Text, o.IsCorrect, o.Id == answer.SelectedOptionId)).ToList());
            })
            .ToList();

        return new QuizResultViewModel
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.QuizTitle,
            CourseId = attempt.CourseId,
            CourseTitle = attempt.CourseTitle,
            StudentName = attempt.StudentName,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            CorrectCount = attempt.CorrectCount,
            QuestionCount = attempt.QuestionCount,
            Score = attempt.Score,
            MaxScore = attempt.MaxScore,
            ScorePercent = attempt.ScorePercent,
            Passed = attempt.Passed,
            PassMarkPercent = attempt.PassMarkPercent,
            CanRetake = attempt.QuizAvailable && attempt.UserId == userId,
            Questions = reviewed
        };
    }

    public async Task<QuizHistoryViewModel> GetHistoryAsync(string userId, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt)
            .Select(a => new QuizAttemptSummary(
                a.Id, a.QuizId, a.Quiz.Title, a.Quiz.CourseId, a.Quiz.Course.Title, a.CompletedAt, a.ScorePercent, a.Passed));

        if (take is int limit)
        {
            query = query.Take(limit);
        }

        return new QuizHistoryViewModel { Attempts = await query.ToListAsync(cancellationToken) };
    }

    public async Task<StudentQuizzesViewModel> GetOverviewAsync(string userId, CancellationToken cancellationToken = default)
    {
        var courseIds = await db.Enrollments.AsNoTracking()
            .Where(e => e.UserId == userId && e.Course.IsPublished)
            .Select(e => e.CourseId)
            .ToListAsync(cancellationToken);

        var quizzes = await db.Quizzes.AsNoTracking()
            .Where(q => courseIds.Contains(q.CourseId) && q.IsPublished && q.Questions.Any())
            .OrderBy(q => q.Course.Title).ThenBy(q => q.Id)
            .Select(q => new StudentQuizItem(
                q.Id,
                q.Title,
                q.Description,
                q.CourseId,
                q.Course.Title,
                q.Questions.Count,
                q.Questions.Sum(question => question.Points),
                q.PassMarkPercent,
                q.Attempts.Count(a => a.UserId == userId),
                q.Attempts.Where(a => a.UserId == userId).Max(a => (int?)a.ScorePercent),
                q.Attempts.Any(a => a.UserId == userId && a.Passed),
                q.Attempts.Where(a => a.UserId == userId).OrderByDescending(a => a.CompletedAt).Select(a => (int?)a.Id).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var history = await GetHistoryAsync(userId, take: 5, cancellationToken);
        return new StudentQuizzesViewModel
        {
            Quizzes = quizzes,
            RecentAttempts = history.Attempts,
            EnrolledCourseCount = courseIds.Count
        };
    }

    private async Task<AvailableQuiz?> FindAvailableQuizAsync(int quizId, CancellationToken cancellationToken) =>
        await db.Quizzes.AsNoTracking()
            .Where(q => q.Id == quizId && q.IsPublished && q.Course.IsPublished && q.Questions.Any())
            .Select(q => new AvailableQuiz(q.Id, q.CourseId, q.Course.Title, q.Title, q.Description, q.PassMarkPercent))
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// The start time comes from the signed token issued with the quiz page, so it cannot be edited in the browser.
    /// Without a valid token for this quiz and student the start is unknown and recorded as the completion time.
    /// </summary>
    private DateTime ReadStartedAt(string? token, int quizId, string userId, DateTime completedAt)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return completedAt;
        }

        try
        {
            var parts = _startTokens.Unprotect(token).Split('|');
            if (parts.Length == 3
                && parts[0] == quizId.ToString(CultureInfo.InvariantCulture)
                && parts[1] == userId
                && long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                && ticks >= DateTime.MinValue.Ticks && ticks <= completedAt.Ticks)
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            logger.LogInformation("Quiz {QuizId} was submitted with an expired or invalid start token.", quizId);
        }

        return completedAt;
    }

    private static string StartPayload(int quizId, string userId, long ticks) =>
        string.Create(CultureInfo.InvariantCulture, $"{quizId}|{userId}|{ticks}");

    private sealed record AvailableQuiz(int Id, int CourseId, string CourseTitle, string Title, string? Description, int PassMarkPercent);
}
