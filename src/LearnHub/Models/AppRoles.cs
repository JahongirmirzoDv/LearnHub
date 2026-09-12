namespace LearnHub.Models;

/// <summary>Role names used by ASP.NET Core Identity and <c>[Authorize(Roles = ...)]</c>.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = [Admin, Student];
}
