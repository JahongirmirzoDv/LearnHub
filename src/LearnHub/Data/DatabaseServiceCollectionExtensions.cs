using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Data;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ApplicationDbContext"/> on the SQLite database named by the configuration.</summary>
    public static IServiceCollection AddLearnHubDatabase(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        var connectionString = SqliteConnectionStrings.Resolve(
            SqliteConnectionStrings.FromConfiguration(configuration), environment.ContentRootPath);
        services.AddDbContext<ApplicationDbContext>(db => db.UseSqlite(connectionString));

        return services;
    }
}

internal static class SqliteConnectionStrings
{
    private const string DefaultConnectionString = "Data Source=App_Data/learnhub.db";

    /// <summary>
    /// <see cref="DatabaseOptions.ConnectionStringVariable"/> wins over <c>ConnectionStrings:DefaultConnection</c>, so a
    /// hosting platform can point the app at its persistent volume without editing appsettings.json.
    /// </summary>
    public static string? FromConfiguration(IConfiguration configuration)
    {
        var fromPlatform = configuration[DatabaseOptions.ConnectionStringVariable];
        return string.IsNullOrWhiteSpace(fromPlatform)
            ? configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
            : fromPlatform;
    }

    /// <summary>
    /// Makes a relative SQLite file path absolute (relative to the content root) and creates its
    /// folder, so the database location does not depend on the current working directory.
    /// </summary>
    public static string Resolve(string? connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(
            string.IsNullOrWhiteSpace(connectionString) ? DefaultConnectionString : connectionString);

        var isInMemory = builder.Mode == SqliteOpenMode.Memory
            || string.Equals(builder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase);

        if (!isInMemory)
        {
            if (!Path.IsPathRooted(builder.DataSource))
            {
                builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
            }

            var directory = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        return builder.ToString();
    }
}
