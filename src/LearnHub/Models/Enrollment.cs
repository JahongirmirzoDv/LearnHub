namespace LearnHub.Models;

/// <summary>
/// Many-to-many link between a student and a course with its own data (dates).
/// Progress is calculated from <see cref="ResourceCompletion"/> and passed quiz attempts,
/// so it is never stored here and cannot become stale.
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
}
