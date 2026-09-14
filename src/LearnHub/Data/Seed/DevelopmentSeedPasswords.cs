using System.Security.Cryptography;
using System.Text.Json;
using LearnHub.Infrastructure;
using Microsoft.Extensions.Options;

namespace LearnHub.Data.Seed;

/// <summary>
/// DEMO ONLY, Development environment only. When no demo passwords are configured, generates strong random ones and
/// keeps them in <c>App_Data/demo-credentials.json</c> (ignored by Git), so a fresh clone can sign in as the demo
/// administrator and student without a password ever being committed. Production must set
/// <c>Seed__AdminPassword</c> and <c>Seed__DemoStudentPassword</c> as environment variables instead.
/// </summary>
public sealed class DevelopmentSeedPasswords(IHostEnvironment environment) : IPostConfigureOptions<SeedOptions>
{
    public const string FileName = "demo-credentials.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string FilePath => Path.Combine(environment.ContentRootPath, "App_Data", FileName);

    public void PostConfigure(string? name, SeedOptions options)
    {
        if (!environment.IsDevelopment()
            || (!string.IsNullOrWhiteSpace(options.AdminPassword) && !string.IsNullOrWhiteSpace(options.DemoStudentPassword)))
        {
            return;
        }

        var saved = Read() ?? new StoredCredentials();
        saved.AdminEmail = options.AdminEmail;
        saved.StudentEmail = options.DemoStudentEmail;
        saved.AdminPassword ??= Generate();
        saved.StudentPassword ??= Generate();
        Write(saved);

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            options.AdminPassword = saved.AdminPassword;
        }

        if (string.IsNullOrWhiteSpace(options.DemoStudentPassword))
        {
            options.DemoStudentPassword = saved.StudentPassword;
        }
    }

    /// <summary>Satisfies the Identity password rules: 20 characters with upper-case, lower-case letters and digits.</summary>
    internal static string Generate() =>
        string.Concat(
            "Demo-",
            RandomNumberGenerator.GetString("ABCDEFGHJKLMNPQRSTUVWXYZ", 4),
            "-",
            RandomNumberGenerator.GetString("abcdefghijkmnopqrstuvwxyz", 6),
            "-",
            RandomNumberGenerator.GetString("23456789", 4));

    private StoredCredentials? Read()
    {
        try
        {
            return File.Exists(FilePath) ? JsonSerializer.Deserialize<StoredCredentials>(File.ReadAllText(FilePath)) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void Write(StoredCredentials credentials)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(credentials, JsonOptions));
    }

    private sealed class StoredCredentials
    {
        public string Notice { get; set; } = "DEMO ONLY - generated for local development. Delete the App_Data folder to reset the database and these passwords.";

        public string? AdminEmail { get; set; }

        public string? AdminPassword { get; set; }

        public string? StudentEmail { get; set; }

        public string? StudentPassword { get; set; }
    }
}
