namespace LearnHub.Models;

/// <summary>
/// Allow-list of icons an administrator can assign to a category. Restricting the value avoids
/// arbitrary CSS class injection and guarantees the icon exists in the vendored icon font.
/// </summary>
public static class CategoryIcons
{
    public const string Default = "journal-code";

    public static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>
    {
        ["journal-code"] = "Notebook",
        ["code-slash"] = "Code",
        ["globe2"] = "Globe",
        ["database"] = "Database",
        ["shield-lock"] = "Shield",
        ["diagram-3"] = "Network diagram",
        ["cloud"] = "Cloud",
        ["cpu"] = "Processor",
        ["palette"] = "Design",
        ["graph-up"] = "Analytics",
        ["phone"] = "Mobile",
        ["robot"] = "Automation",
        ["lightbulb"] = "Ideas",
        ["briefcase"] = "Business"
    };

    public static bool IsAllowed(string? iconName) =>
        iconName is not null && Allowed.ContainsKey(iconName);
}
