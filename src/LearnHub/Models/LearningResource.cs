namespace LearnHub.Models;

/// <summary>
/// A lesson item inside a course. Which content fields are used depends on <see cref="Type"/>:
/// Article → <see cref="Body"/>; Video and Link → <see cref="ExternalUrl"/>;
/// Pdf and Image → the File* fields (stored privately, streamed after an access check).
/// </summary>
public class LearningResource : IHasTimestamps
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    /// <summary>Short description; for images it doubles as the alternative text.</summary>
    public string? Summary { get; set; }

    public ResourceType Type { get; set; }

    public string? Body { get; set; }

    public string? ExternalUrl { get; set; }

    /// <summary>Path relative to the private storage root, e.g. <c>resources/3f2a….pdf</c>.</summary>
    public string? FilePath { get; set; }

    /// <summary>Sanitised original file name, used for downloads.</summary>
    public string? FileName { get; set; }

    /// <summary>MIME type detected from the file signature, not from the browser.</summary>
    public string? FileContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public int? EstimatedMinutes { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Preview resources can be opened by guests without enrolling.</summary>
    public bool IsPreview { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ResourceCompletion> Completions { get; set; } = [];
}
