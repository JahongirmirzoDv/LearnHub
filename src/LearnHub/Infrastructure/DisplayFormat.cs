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

    /// <summary>Time spent on a quiz attempt, e.g. "under a minute", "7 min" or "1 h 5 min".</summary>
    public static string TimeTaken(TimeSpan duration) =>
        duration.TotalMinutes < 1 ? "under a minute" : Duration((int)Math.Round(duration.TotalMinutes));

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
        ResourceType.Exercise => "bi-pencil-square",
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

    /// <summary>Category line colour class (<c>line-0</c> … <c>line-7</c>), derived from the category id.</summary>
    public static string LineClass(int categoryId) => $"line-{Math.Abs(categoryId % 8)}";

    /// <summary>
    /// Width class for progress bars (<c>pw-0</c> … <c>pw-100</c> in steps of 5). Inline style attributes would
    /// be blocked by the Content-Security-Policy; any started progress shows at least one step.
    /// </summary>
    public static string ProgressWidthClass(int percent)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var stepped = (int)(Math.Round(clamped / 5.0, MidpointRounding.AwayFromZero) * 5);
        return $"pw-{(clamped > 0 && stepped == 0 ? 5 : stepped)}";
    }

    public static string StudentFirstName(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;

    /// <summary>Icon and link target for a dashboard activity entry.</summary>
    public static (string Icon, string IconClass, string Controller, string Action) Activity(ViewModels.Shared.ActivityKind kind) => kind switch
    {
        ViewModels.Shared.ActivityKind.Enrolled => ("bi-journal-plus", string.Empty, "Courses", "Details"),
        ViewModels.Shared.ActivityKind.CompletedResource => ("bi-check2-circle", "is-go", "Resources", "Details"),
        ViewModels.Shared.ActivityKind.PassedQuiz => ("bi-trophy", "is-signal", "Quizzes", "Result"),
        _ => ("bi-arrow-repeat", string.Empty, "Quizzes", "Result")
    };
}
