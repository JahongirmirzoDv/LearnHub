namespace LearnHub.Models;

/// <summary>A single-choice question: exactly one <see cref="AnswerOption"/> is correct.</summary>
public class Question
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public Quiz Quiz { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    /// <summary>Shown to the student on the result page to explain the correct answer.</summary>
    public string? Explanation { get; set; }

    /// <summary>Weight of the question in the score (1–100); a correct answer earns all points, a wrong one none.</summary>
    public int Points { get; set; } = 1;

    public int SortOrder { get; set; }

    public ICollection<AnswerOption> Options { get; set; } = [];
}
