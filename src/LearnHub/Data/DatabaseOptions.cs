namespace LearnHub.Data;

/// <summary>Bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Environment variable that, when set, replaces <c>ConnectionStrings:DefaultConnection</c>. Hosting platforms
    /// such as Railway set it from the service's variables, e.g. <c>Data Source=/data/learnhub.db</c>.
    /// </summary>
    public const string ConnectionStringVariable = "DATABASE_CONNECTION_STRING";

    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>
    /// Applies pending EF Core migrations when the application starts. Suitable for the
    /// single-instance deployment used by this project.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; } = true;
}
