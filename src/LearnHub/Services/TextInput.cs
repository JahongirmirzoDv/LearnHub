namespace LearnHub.Services;

/// <summary>Normalises free-text form input before it is stored.</summary>
public static class TextInput
{
    /// <summary>Trims the value, normalises line endings and turns blank strings into <c>null</c>.</summary>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }

    public static string Required(string value) => Clean(value) ?? string.Empty;
}
