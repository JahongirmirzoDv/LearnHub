namespace LearnHub.Models;

public class Course : IHasTimestamps
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>One or two sentences shown on course cards.</summary>
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>Plain text; paragraphs are separated by blank lines.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Plain text, one learning outcome per line.</summary>
    public string? LearningOutcomes { get; set; }

    public string InstructorName { get; set; } = string.Empty;

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    public int DurationMinutes { get; set; }

    /// <summary>
    /// Browser path of the cover image: either a bundled asset (<c>/images/courses/…</c>) or an
    /// uploaded file (<c>/media/thumbnails/…</c>). Never entered as free text by users.
    /// </summary>
    public string? ThumbnailPath { get; set; }

    public bool IsPublished { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<LearningResource> Resources { get; set; } = [];

    public ICollection<Quiz> Quizzes { get; set; } = [];

    public ICollection<Enrollment> Enrollments { get; set; } = [];
}
