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

    public int SortOrder { get; set; }

    public ICollection<AnswerOption> Options { get; set; } = [];
}
