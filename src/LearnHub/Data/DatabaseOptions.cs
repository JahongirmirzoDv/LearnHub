namespace LearnHub.Data;

public enum DatabaseProvider
{
    Sqlite,
    SqlServer
}

/// <summary>Bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;

    /// <summary>
    /// Applies pending EF Core migrations when the application starts. Suitable for the
    /// single-instance deployment used by this project.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; } = true;
}
