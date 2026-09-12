using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Learning;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

/// <summary>Student quiz flow: take a quiz, submit answers, review results and history.</summary>
public interface IQuizService
{
    Task<QuizTakeResult> GetQuizToTakeAsync(int quizId, string userId, CancellationToken cancellationToken = default);

    Task<QuizSubmitResult> SubmitAsync(int quizId, string userId, IReadOnlyDictionary<int, int> answers, CancellationToken cancellationToken = default);

    /// <summary>Returns null unless the attempt belongs to <paramref name="userId"/> or the caller is an admin (prevents IDOR).</summary>
    Task<QuizResultViewModel?> GetResultAsync(int attemptId, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<QuizHistoryViewModel> GetHistoryAsync(string userId, int? take = null, CancellationToken cancellationToken = default);
}

public sealed class QuizService(
    ApplicationDbContext db,
    IEnrollmentService enrollments,
    TimeProvider clock,
    ILogger<QuizService> logger) : IQuizService
{
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
            Questions = questions
                .Select((q, index) => new TakeQuizQuestion(q.Id, index + 1, q.Text, q.Options))
                .ToList()
        };

        return new QuizTakeResult(ResourceAccess.Allowed, quiz.CourseId, model);
    }

    public async Task<QuizSubmitResult> SubmitAsync(
        int quizId, string userId, IReadOnlyDictionary<int, int> answers, CancellationToken cancellationToken = default)
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
                q.Options.Select(o => new GradingOption(o.Id, o.IsCorrect)).ToList()))
            .ToListAsync(cancellationToken);

        var graded = QuizGrader.Grade(questions, answers, quiz.PassMarkPercent);

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            SubmittedAt = clock.GetUtcNow().UtcDateTime,
            CorrectCount = graded.CorrectCount,
            QuestionCount = graded.QuestionCount,
            ScorePercent = graded.ScorePercent,
            Passed = graded.Passed,
            Answers = graded.Answers
                .Select(a => new QuizAnswer { QuestionId = a.QuestionId, SelectedOptionId = a.SelectedOptionId, IsCorrect = a.IsCorrect })
                .ToList()
        };

        db.QuizAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} submitted quiz {QuizId}: {Score}% ({Result}).",
            userId, quizId, attempt.ScorePercent, attempt.Passed ? "passed" : "not passed");

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
                a.SubmittedAt,
                a.CorrectCount,
                a.QuestionCount,
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
            SubmittedAt = attempt.SubmittedAt,
            CorrectCount = attempt.CorrectCount,
            QuestionCount = attempt.QuestionCount,
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
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new QuizAttemptSummary(
                a.Id, a.QuizId, a.Quiz.Title, a.Quiz.CourseId, a.Quiz.Course.Title, a.SubmittedAt, a.ScorePercent, a.Passed));

        if (take is int limit)
        {
            query = query.Take(limit);
        }

        return new QuizHistoryViewModel { Attempts = await query.ToListAsync(cancellationToken) };
    }

    private async Task<AvailableQuiz?> FindAvailableQuizAsync(int quizId, CancellationToken cancellationToken) =>
        await db.Quizzes.AsNoTracking()
            .Where(q => q.Id == quizId && q.IsPublished && q.Course.IsPublished && q.Questions.Any())
            .Select(q => new AvailableQuiz(q.Id, q.CourseId, q.Course.Title, q.Title, q.Description, q.PassMarkPercent))
            .FirstOrDefaultAsync(cancellationToken);

    private sealed record AvailableQuiz(int Id, int CourseId, string CourseTitle, string Title, string? Description, int PassMarkPercent);
}
