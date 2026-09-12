namespace LearnHub.Models;

/// <summary>
/// Entities implementing this interface get <see cref="CreatedAt"/> and <see cref="UpdatedAt"/>
/// maintained automatically by <c>ApplicationDbContext.SaveChangesAsync</c> (UTC).
/// </summary>
public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
