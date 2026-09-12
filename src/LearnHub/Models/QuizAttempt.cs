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

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int CorrectCount { get; set; }

    public int QuestionCount { get; set; }

    public int ScorePercent { get; set; }

    public bool Passed { get; set; }

    public ICollection<QuizAnswer> Answers { get; set; } = [];
}
