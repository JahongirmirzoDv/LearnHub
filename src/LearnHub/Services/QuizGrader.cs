namespace LearnHub.Services;

public sealed record GradingQuestion(int QuestionId, IReadOnlyList<GradingOption> Options);

public sealed record GradingOption(int OptionId, bool IsCorrect);

public sealed record GradedAnswer(int QuestionId, int? SelectedOptionId, bool IsCorrect);

public sealed record GradedAttempt(int CorrectCount, int QuestionCount, int ScorePercent, bool Passed, IReadOnlyList<GradedAnswer> Answers);

/// <summary>
/// Pure, side-effect-free grading used by <see cref="QuizService"/>. Grading always happens on the server:
/// the browser only sends option ids, and an id that does not belong to the question is treated as unanswered.
/// </summary>
public static class QuizGrader
{
    public static GradedAttempt Grade(IReadOnlyList<GradingQuestion> questions, IReadOnlyDictionary<int, int> submittedAnswers, int passMarkPercent)
    {
        var answers = new List<GradedAnswer>(questions.Count);
        foreach (var question in questions)
        {
            GradingOption? selected = null;
            if (submittedAnswers.TryGetValue(question.QuestionId, out var optionId))
            {
                selected = question.Options.FirstOrDefault(option => option.OptionId == optionId);
            }

            answers.Add(new GradedAnswer(question.QuestionId, selected?.OptionId, selected?.IsCorrect == true));
        }

        var correct = answers.Count(answer => answer.IsCorrect);
        // Floor, so a student who needs 70% really scored at least 70%.
        var percent = questions.Count == 0 ? 0 : (int)Math.Floor(correct * 100.0 / questions.Count);
        return new GradedAttempt(correct, questions.Count, percent, percent >= passMarkPercent, answers);
    }
}
