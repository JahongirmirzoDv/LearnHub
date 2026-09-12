namespace LearnHub.Models;

/// <summary>Records that a user marked a learning resource as completed.</summary>
public class ResourceCompletion
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int LearningResourceId { get; set; }

    public LearningResource LearningResource { get; set; } = null!;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
