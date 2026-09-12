namespace LearnHub.Models;

public class Quiz : IHasTimestamps
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Minimum score (0–100) required to pass.</summary>
    public int PassMarkPercent { get; set; } = 70;

    /// <summary>Only published quizzes with at least one valid question can be taken.</summary>
    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = [];

    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}
