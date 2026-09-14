using LearnHub.Data;
using LearnHub.Models;
using LearnHub.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Services;

/// <summary>Admin management of quiz questions and their answer options.</summary>
public interface IQuestionManagementService
{
    Task<QuestionFormViewModel?> GetNewAsync(int quizId, CancellationToken cancellationToken = default);

    Task<QuestionFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns the quiz title for a form redisplayed after a validation error (null if the quiz is gone).</summary>
    Task<string?> GetQuizTitleAsync(int quizId, CancellationToken cancellationToken = default);

    /// <summary>The value is the quiz id, for redirecting back to the quiz.</summary>
    Task<OperationResult<int>> CreateAsync(QuestionFormViewModel model, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> UpdateAsync(int id, QuestionFormViewModel model, CancellationToken cancellationToken = default);

    Task<QuestionDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class QuestionManagementService(
    ApplicationDbContext db,
    IProgressService progress,
    ILogger<QuestionManagementService> logger) : IQuestionManagementService
{
    public async Task<QuestionFormViewModel?> GetNewAsync(int quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await db.Quizzes.AsNoTracking()
            .Where(q => q.Id == quizId)
            .Select(q => new { q.Title, MaxOrder = q.Questions.Max(question => (int?)question.SortOrder) })
            .FirstOrDefaultAsync(cancellationToken);

        return quiz is null
            ? null
            : new QuestionFormViewModel { QuizId = quizId, QuizTitle = quiz.Title, SortOrder = (quiz.MaxOrder ?? 0) + 1 }.PadOptions();
    }

    public async Task<QuestionFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await db.Questions.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new
            {
                q.Id,
                q.QuizId,
                QuizTitle = q.Quiz.Title,
                q.Text,
                q.Explanation,
                q.SortOrder,
                q.Points,
                Options = q.Options.OrderBy(o => o.SortOrder).ThenBy(o => o.Id).Select(o => new { o.Id, o.Text, o.IsCorrect }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (question is null)
        {
            return null;
        }

        var model = new QuestionFormViewModel
        {
            Id = question.Id,
            QuizId = question.QuizId,
            QuizTitle = question.QuizTitle,
            Text = question.Text,
            Explanation = question.Explanation,
            SortOrder = question.SortOrder,
            Points = question.Points,
            Options = question.Options.Select(o => new AnswerOptionInput { Id = o.Id, Text = o.Text }).ToList()
        };

        var correctIndex = question.Options.FindIndex(o => o.IsCorrect);
        model.CorrectOptionIndex = correctIndex >= 0 ? correctIndex : null;
        return model.PadOptions();
    }

    public Task<string?> GetQuizTitleAsync(int quizId, CancellationToken cancellationToken = default) =>
        db.Quizzes.AsNoTracking().Where(q => q.Id == quizId).Select(q => q.Title).FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult<int>> CreateAsync(QuestionFormViewModel model, CancellationToken cancellationToken = default)
    {
        var courseId = await db.Quizzes.Where(q => q.Id == model.QuizId).Select(q => (int?)q.CourseId).FirstOrDefaultAsync(cancellationToken);
        if (courseId is null)
        {
            return OperationResult<int>.NotFound();
        }

        var question = new Question
        {
            QuizId = model.QuizId,
            Text = TextInput.Required(model.Text),
            Explanation = TextInput.Clean(model.Explanation),
            SortOrder = model.SortOrder,
            Points = model.Points
        };

        var position = 0;
        for (var index = 0; index < model.Options.Count; index++)
        {
            var text = TextInput.Clean(model.Options[index].Text);
            if (text is null)
            {
                continue;
            }

            question.Options.Add(new AnswerOption { Text = text, IsCorrect = index == model.CorrectOptionIndex, SortOrder = position++ });
        }

        db.Questions.Add(question);
        await db.SaveChangesAsync(cancellationToken);
        // The first question makes a published quiz available, which changes every enrolled student's progress.
        await progress.SyncAsync(courseId.Value, cancellationToken: cancellationToken);
        logger.LogInformation("Question {QuestionId} added to quiz {QuizId}.", question.Id, question.QuizId);
        return OperationResult<int>.Success(question.QuizId);
    }

    public async Task<OperationResult<int>> UpdateAsync(int id, QuestionFormViewModel model, CancellationToken cancellationToken = default)
    {
        var question = await db.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (question is null)
        {
            return OperationResult<int>.NotFound();
        }

        question.Text = TextInput.Required(model.Text);
        question.Explanation = TextInput.Clean(model.Explanation);
        question.SortOrder = model.SortOrder;
        // Past attempts keep their stored score; new points apply to attempts submitted from now on.
        question.Points = model.Points;

        var keptOptionIds = new HashSet<int>();
        var position = 0;
        for (var index = 0; index < model.Options.Count; index++)
        {
            var input = model.Options[index];
            var text = TextInput.Clean(input.Text);
            if (text is null)
            {
                continue;
            }

            // Only options that already belong to THIS question can be edited; posted ids for other
            // questions are ignored and treated as new answers.
            var option = input.Id is int optionId ? question.Options.FirstOrDefault(o => o.Id == optionId) : null;
            if (option is null)
            {
                option = new AnswerOption();
                question.Options.Add(option);
            }
            else
            {
                keptOptionIds.Add(option.Id);
            }

            option.Text = text;
            option.IsCorrect = index == model.CorrectOptionIndex;
            option.SortOrder = position++;
        }

        var removedOptions = question.Options.Where(o => o.Id != 0 && !keptOptionIds.Contains(o.Id)).ToList();
        var removedIds = removedOptions.Select(o => o.Id).ToList();

        await db.InTransactionAsync(async () =>
        {
            if (removedIds.Count > 0)
            {
                // Past attempts keep their score snapshot; the removed answer simply shows as "not answered".
                await db.QuizAnswers
                    .Where(a => a.SelectedOptionId != null && removedIds.Contains(a.SelectedOptionId.Value))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.SelectedOptionId, (int?)null), cancellationToken);
                db.AnswerOptions.RemoveRange(removedOptions);
            }

            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        logger.LogInformation("Question {QuestionId} updated ({RemovedCount} answer options removed).", id, removedIds.Count);
        return OperationResult<int>.Success(question.QuizId);
    }

    public async Task<QuestionDeleteViewModel?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Questions.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new QuestionDeleteViewModel(q.Id, q.QuizId, q.Quiz.Title, q.Text, db.QuizAnswers.Count(a => a.QuestionId == q.Id)))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var target = await db.Questions.Where(q => q.Id == id).Select(q => new { q.QuizId, q.Quiz.CourseId }).FirstOrDefaultAsync(cancellationToken);
        if (target is null)
        {
            return OperationResult<int>.NotFound();
        }

        await db.InTransactionAsync(async () =>
        {
            // Answers reference the question with NO ACTION; the options cascade with the question row.
            await db.QuizAnswers.Where(a => a.QuestionId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Questions.Where(q => q.Id == id).ExecuteDeleteAsync(cancellationToken);
        }, cancellationToken);

        // Removing the last question makes the quiz unavailable, so it no longer counts towards progress.
        await progress.SyncAsync(target.CourseId, cancellationToken: cancellationToken);
        logger.LogInformation("Question {QuestionId} deleted from quiz {QuizId}.", id, target.QuizId);
        return OperationResult<int>.Success(target.QuizId);
    }
}
