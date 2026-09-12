using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LearnHub.Infrastructure;

public enum StatusKind
{
    Success,
    Info,
    Warning,
    Danger
}

/// <summary>One-time "flash" messages carried across a redirect (Post/Redirect/Get) via TempData.</summary>
public static class StatusMessages
{
    public const string MessageKey = "StatusMessage";
    public const string KindKey = "StatusKind";

    public static void SetStatus(this ITempDataDictionary tempData, string message, StatusKind kind = StatusKind.Success)
    {
        tempData[MessageKey] = message;
        tempData[KindKey] = kind.ToString();
    }
}
