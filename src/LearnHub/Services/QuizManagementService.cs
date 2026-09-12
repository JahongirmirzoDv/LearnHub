using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Admin;
using LearnHub.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

/// <summary>Admin management of quizzes and of the attempts (results) students have submitted.</summary>
public interface IQuizManagementService
{
    Task<AdminQuizListViewModel> GetListAsync(AdminQuizQuery query, CancellationToken cancellationToken = default);

    Task<AdminQuizDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<QuizFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> CreateAsync(QuizFormViewModel model, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(int id, QuizFormViewModel model, CancellationToken cancellationToken = default);

    Task<QuizDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminAttemptListViewModel> GetAttemptsAsync(AdminAttemptQuery query, CancellationToken cancellationToken = default);

    Task<AttemptDeleteViewModel?> GetAttemptForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAttemptAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class QuizManagementService(
    ApplicationDbContext db,
    ILookupService lookups,
    ILogger<QuizManagementService> logger) : IQuizManagementService
{
    public const int PageSize = 20;

    public async Task<AdminQuizListViewModel> GetListAsync(AdminQuizQuery query, CancellationToken cancellationToken = default)
    {
        var quizzes = db.Quizzes.AsNoTracking();

        if (query.CourseId is int courseId)
        {
            quizzes = quizzes.Where(q => q.CourseId == courseId);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = SearchPattern.Contains(query.Q);
            quizzes = quizzes.Where(q => EF.Functions.Like(q.Title, pattern, SearchPattern.EscapeCharacter));
        }

        var results = await quizzes
            .OrderBy(q => q.Course.Title).ThenBy(q => q.Title)
            .Select(q => new AdminQuizListItem(
                q.Id, q.Title, q.CourseId, q.Course.Title, q.Questions.Count, q.Attempts.Count, q.PassMarkPercent, q.IsPublished, q.UpdatedAt))
            .ToPagedResultAsync(query.Page, PageSize, cancellationToken);

        query.Page = results.Page;
        return new AdminQuizListViewModel
        {
            Query = query,
            Results = results,
            Courses = await lookups.GetCoursesAsync(cancellationToken)
        };
    }

    public async Task<AdminQuizDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Quizzes.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new AdminQuizDetailsViewModel
            {
                Id = q.Id,
                Title = q.Title,
                Description = q.Description,
                CourseId = q.CourseId,
                CourseTitle = q.Course.Title,
                PassMarkPercent = q.PassMarkPercent,
                IsPublished = q.IsPublished,
                AttemptCount = q.Attempts.Count,
                AverageScorePercent = (int?)q.Attempts.Average(a => (double?)a.ScorePercent),
                UpdatedAt = q.UpdatedAt,
                Questions = q.Questions
                    .OrderBy(question => question.SortOrder).ThenBy(question => question.Id)
                    .Select(question => new AdminQuestionItem(
                        question.Id,
                        question.SortOrder,
                        question.Text,
                        question.Explanation,
                        db.QuizAnswers.Count(answer => answer.QuestionId == question.Id),
                        question.Options
                            .OrderBy(option => option.SortOrder).ThenBy(option => option.Id)
                            .Select(option => new AdminOptionItem(option.Id, option.Text, option.IsCorrect))
                            .ToList()))
                    .ToList()
            })
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<QuizFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var model = await db.Quizzes.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new QuizFormViewModel
            {
                Id = q.Id,
                CourseId = q.CourseId,
                Title = q.Title,
                Description = q.Description,
                PassMarkPercent = q.PassMarkPercent,
                IsPublished = q.IsPublished
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (model is not null)
        {
            model.CourseOptions = await lookups.GetCoursesAsync(cancellationToken);
        }

        return model;
    }

    public async Task<OperationResult<int>> CreateAsync(QuizFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == model.CourseId, cancellationToken))
        {
            return OperationResult<int>.Failure("The selected course no longer exists.");
        }

        var quiz = new Quiz
        {
            CourseId = model.CourseId!.Value,
            Title = TextInput.Required(model.Title),
            Description = TextInput.Clean(model.Description),
            PassMarkPercent = model.PassMarkPercent,
            // A new quiz has no questions yet, so it always starts as a draft.
            IsPublished = false
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Quiz {QuizId} created in course {CourseId}.", quiz.Id, quiz.CourseId);
        return OperationResult<int>.Success(quiz.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, QuizFormViewModel model, CancellationToken cancellationToken = default)
    {
        var quiz = await db.Quizzes.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quiz is null)
        {
            return OperationResult.NotFound();
        }

        if (!await db.Courses.AnyAsync(c => c.Id == model.CourseId, cancellationToken))
        {
            return OperationResult.Failure("The selected course no longer exists.");
        }

        if (model.IsPublished && !await db.Questions.AnyAsync(q => q.QuizId == id, cancellationToken))
        {
            return OperationResult.Failure("Add at least one question before publishing this quiz.");
        }

        quiz.CourseId = model.CourseId!.Value;
        quiz.Title = TextInput.Required(model.Title);
        quiz.Description = TextInput.Clean(model.Description);
        quiz.PassMarkPercent = model.PassMarkPercent;
        quiz.IsPublished = model.IsPublished;

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Quiz {QuizId} updated.", id);
        return OperationResult.Success();
    }

    public async Task<QuizDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Quizzes.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new QuizDeleteViewModel(q.Id, q.Title, q.CourseId, q.Course.Title, q.Questions.Count, q.Attempts.Count))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var quiz = await db.Quizzes.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quiz is null)
        {
            return OperationResult<int>.NotFound();
        }

        await db.InTransactionAsync(async () =>
        {
            // Answers reference questions with NO ACTION; remove them before the quiz cascade runs.
            await db.QuizAnswers.Where(a => a.QuizAttempt.QuizId == id).ExecuteDeleteAsync(cancellationToken);
            db.Quizzes.Remove(quiz);
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        logger.LogInformation("Quiz {QuizId} deleted.", id);
        return OperationResult<int>.Success(quiz.CourseId);
    }

    public async Task<AdminAttemptListViewModel> GetAttemptsAsync(AdminAttemptQuery query, CancellationToken cancellationToken = default)
    {
        var attempts = db.QuizAttempts.AsNoTracking();

        if (query.QuizId is int quizId)
        {
            attempts = attempts.Where(a => a.QuizId == quizId);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = SearchPattern.Contains(query.Q);
            attempts = attempts.Where(a =>
                EF.Functions.Like(a.User.FullName, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(a.User.Email!, pattern, SearchPattern.EscapeCharacter));
        }

        var results = await attempts
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new AdminAttemptListItem(
                a.Id, a.User.FullName, a.User.Email ?? string.Empty, a.QuizId, a.Quiz.Title, a.Quiz.Course.Title, a.ScorePercent, a.Passed, a.SubmittedAt))
            .ToPagedResultAsync(query.Page, PageSize, cancellationToken);

        query.Page = results.Page;
        return new AdminAttemptListViewModel
        {
            Query = query,
            Results = results,
            Quizzes = await lookups.GetQuizzesAsync(cancellationToken)
        };
    }

    public async Task<AttemptDeleteViewModel?> GetAttemptForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await db.QuizAttempts.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AttemptDeleteViewModel(a.Id, a.User.FullName, a.Quiz.Title, a.ScorePercent, a.SubmittedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult> DeleteAttemptAsync(int id, CancellationToken cancellationToken = default)
    {
        var attempt = await db.QuizAttempts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (attempt is null)
        {
            return OperationResult.NotFound();
        }

        db.QuizAttempts.Remove(attempt);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Quiz attempt {AttemptId} deleted by an administrator.", id);
        return OperationResult.Success();
    }
}
