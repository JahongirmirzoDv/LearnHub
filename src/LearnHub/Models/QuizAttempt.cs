namespace LearnHub.Models;

/// <summary>
/// A submitted quiz. The score fields are a snapshot taken at submission time, so the student's
/// history stays accurate even if an administrator later edits the quiz.
/// </summary>
public class QuizAttempt
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public Quiz Quiz { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    /// <summary>When the quiz page was opened (read from a signed token posted with the answers).</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the answers were submitted and graded.</summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public int CorrectCount { get; set; }

    public int QuestionCount { get; set; }

    /// <summary>Points earned: the sum of <see cref="Question.Points"/> for correctly answered questions.</summary>
    public int Score { get; set; }

    /// <summary>Points available when the attempt was made.</summary>
    public int MaxScore { get; set; }

    /// <summary><see cref="Score"/> as a percentage of <see cref="MaxScore"/>, rounded down.</summary>
    public int ScorePercent { get; set; }

    public bool Passed { get; set; }

    public ICollection<QuizAnswer> Answers { get; set; } = [];
}
