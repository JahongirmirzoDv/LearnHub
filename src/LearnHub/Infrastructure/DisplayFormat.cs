using System.Globalization;
using LearnHub.Models;

namespace LearnHub.Infrastructure;

/// <summary>Presentation helpers shared by views, so formatting rules live in one place.</summary>
public static class DisplayFormat
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Duration(int minutes)
    {
        if (minutes <= 0)
        {
            return "—";
        }

        if (minutes < 60)
        {
            return $"{minutes} min";
        }

        var hours = minutes / 60;
        var rest = minutes % 60;
        return rest == 0 ? $"{hours} h" : $"{hours} h {rest} min";
    }

    public static string FileSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => (bytes / (1024d * 1024d)).ToString("0.#", Culture) + " MB",
        >= 1024 => (bytes / 1024d).ToString("0", Culture) + " KB",
        _ => bytes.ToString(Culture) + " B"
    };

    public static string Date(DateTime utc) => utc.ToString("d MMM yyyy", Culture);

    public static string DateAndTime(DateTime utc) => utc.ToString("d MMM yyyy, HH:mm 'UTC'", Culture);

    /// <summary>ISO 8601 value for the <c>datetime</c> attribute; JavaScript converts it to the visitor's local time.</summary>
    public static string IsoDate(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("O", Culture);

    public static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "?";
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var initials = parts.Length == 1
            ? parts[0][..1]
            : string.Concat(parts[0][..1], parts[^1][..1]);
        return initials.ToUpperInvariant();
    }

    public static string DifficultyBadgeClass(DifficultyLevel difficulty) => difficulty switch
    {
        DifficultyLevel.Beginner => "badge-level badge-level--beginner",
        DifficultyLevel.Intermediate => "badge-level badge-level--intermediate",
        DifficultyLevel.Advanced => "badge-level badge-level--advanced",
        _ => "badge-level"
    };

    public static string ResourceIcon(ResourceType type) => type switch
    {
        ResourceType.Article => "bi-file-text",
        ResourceType.Video => "bi-play-circle",
        ResourceType.Pdf => "bi-file-earmark-pdf",
        ResourceType.Image => "bi-image",
        ResourceType.Link => "bi-box-arrow-up-right",
        _ => "bi-file-earmark"
    };

    public static string ResourceTypeName(ResourceType type) => type switch
    {
        ResourceType.Pdf => "PDF document",
        ResourceType.Link => "External link",
        _ => type.ToString()
    };

    /// <summary>Plain text split into paragraphs on blank lines (rendered with Razor encoding, never as HTML).</summary>
    public static IReadOnlyList<string> Paragraphs(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Plain text split into non-empty lines, e.g. learning outcomes.</summary>
    public static IReadOnlyList<string> Lines(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string Plural(int count, string singular, string? plural = null) =>
        $"{count.ToString(Culture)} {(count == 1 ? singular : plural ?? singular + "s")}";
}
