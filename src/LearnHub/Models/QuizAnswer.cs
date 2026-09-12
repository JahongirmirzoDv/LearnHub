namespace LearnHub.Models;

/// <summary>The option a student selected for one question in an attempt (null = unanswered).</summary>
public class QuizAnswer
{
    public int Id { get; set; }

    public int QuizAttemptId { get; set; }

    public QuizAttempt QuizAttempt { get; set; } = null!;

    public int QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public int? SelectedOptionId { get; set; }

    public AnswerOption? SelectedOption { get; set; }

    public bool IsCorrect { get; set; }
}
