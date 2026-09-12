namespace LearnHub.Infrastructure;

/// <summary>Bound from the <c>Seed</c> section. Passwords must come from user-secrets or environment variables.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Creates demonstration categories, courses, resources, quizzes and learners on an empty database.</summary>
    public bool DemoData { get; set; } = true;

    public string? AdminEmail { get; set; }

    public string AdminFullName { get; set; } = "LearnHub Administrator";

    /// <summary>When empty, no administrator is created and a warning explains how to configure one.</summary>
    public string? AdminPassword { get; set; }

    public string DemoStudentEmail { get; set; } = "demo.student@example.com";

    /// <summary>When empty, the demo student exists (for realistic data) but cannot sign in.</summary>
    public string? DemoStudentPassword { get; set; }
}

/// <summary>Bound from the <c>Storage</c> section.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Private folder for uploaded files. Relative paths are resolved against the content root.</summary>
    public string RootPath { get; set; } = "App_Data/storage";
}

/// <summary>Bound from the <c>RateLimiting</c> section; applies to login, registration and contact forms.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}

/// <summary>Bound from the <c>Site</c> section; links shown in the footer and About page.</summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string? RepositoryUrl { get; set; }

    public string? PresentationUrl { get; set; }
}

public static class RateLimitPolicies
{
    public const string Forms = "forms";
}
