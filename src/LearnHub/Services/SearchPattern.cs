namespace LearnHub.Services;

/// <summary>
/// Builds a parameter value for <c>EF.Functions.Like</c>. User input is still sent as a SQL parameter
/// (no injection), but LIKE wildcards typed by the user (% _ [) are escaped so they match literally.
/// </summary>
public static class SearchPattern
{
    public const string EscapeCharacter = "\\";

    public const int MaxTermLength = 100;

    public static string Contains(string term)
    {
        var trimmed = term.Trim();
        if (trimmed.Length > MaxTermLength)
        {
            trimmed = trimmed[..MaxTermLength];
        }

        var escaped = trimmed
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
            .Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", EscapeCharacter + "_", StringComparison.Ordinal)
            .Replace("[", EscapeCharacter + "[", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
