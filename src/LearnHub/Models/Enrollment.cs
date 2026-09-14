namespace LearnHub.Models;

/// <summary>
/// Many-to-many link between a student and a course with its own data. <see cref="CompletionPercentage"/> and
/// <see cref="CompletedAt"/> are derived from <see cref="ResourceCompletion"/> rows and passed quiz attempts; they are
/// stored so lists and reports can sort and filter on them, and <c>ProgressService.SyncAsync</c> recalculates them
/// whenever a student's activity or the course content changes.
/// </summary>
public class Enrollment
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>Updated when the student opens a resource or quiz; drives "Continue learning".</summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>Completed items as a percentage (0–100) of the course's published lessons and available quizzes.</summary>
    public int CompletionPercentage { get; set; }

    /// <summary>When the course reached 100%; cleared if new content later brings it below 100%.</summary>
    public DateTime? CompletedAt { get; set; }
}
