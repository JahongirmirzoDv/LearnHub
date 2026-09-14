namespace LearnHub.Services;

public sealed record GradingQuestion(int QuestionId, int Points, IReadOnlyList<GradingOption> Options);

public sealed record GradingOption(int OptionId, bool IsCorrect);

public sealed record GradedAnswer(int QuestionId, int? SelectedOptionId, bool IsCorrect);

public sealed record GradedAttempt(
    int CorrectCount,
    int QuestionCount,
    int Score,
    int MaxScore,
    int ScorePercent,
    bool Passed,
    IReadOnlyList<GradedAnswer> Answers);

/// <summary>
/// Pure, side-effect-free grading used by <see cref="QuizService"/>. Grading always happens on the server:
/// the browser only sends option ids, and an id that does not belong to the question is treated as unanswered.
/// Each question is worth its <see cref="GradingQuestion.Points"/>; the pass mark applies to the percentage of points.
/// </summary>
public static class QuizGrader
{
    public static GradedAttempt Grade(IReadOnlyList<GradingQuestion> questions, IReadOnlyDictionary<int, int> submittedAnswers, int passMarkPercent)
    {
        var answers = new List<GradedAnswer>(questions.Count);
        var score = 0;
        foreach (var question in questions)
        {
            GradingOption? selected = null;
            if (submittedAnswers.TryGetValue(question.QuestionId, out var optionId))
            {
                selected = question.Options.FirstOrDefault(option => option.OptionId == optionId);
            }

            var isCorrect = selected?.IsCorrect == true;
            if (isCorrect)
            {
                score += question.Points;
            }

            answers.Add(new GradedAnswer(question.QuestionId, selected?.OptionId, isCorrect));
        }

        var maxScore = questions.Sum(question => question.Points);
        // Floor, so a student who needs 70% really scored at least 70%.
        var percent = maxScore == 0 ? 0 : (int)Math.Floor(score * 100.0 / maxScore);
        return new GradedAttempt(answers.Count(answer => answer.IsCorrect), questions.Count, score, maxScore, percent, percent >= passMarkPercent, answers);
    }
}
