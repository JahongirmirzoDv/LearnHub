using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Data;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ApplicationDbContext"/> backed by the provider selected in
    /// <c>Database:Provider</c>. Controllers and services depend on the abstract context only.
    /// </summary>
    public static IServiceCollection AddLearnHubDatabase(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(DatabaseOptions.SectionName);
        services.Configure<DatabaseOptions>(section);
        var options = section.Get<DatabaseOptions>() ?? new DatabaseOptions();

        if (options.Provider == DatabaseProvider.SqlServer)
        {
            var connectionString = configuration.GetConnectionString("SqlServer");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:SqlServer must be configured when Database:Provider is SqlServer.");
            }

            // Azure SQL can briefly refuse connections (e.g. while resuming from auto-pause),
            // so transient failures are retried.
            services.AddDbContext<ApplicationDbContext, SqlServerDbContext>(db =>
                db.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        }
        else
        {
            var connectionString = SqliteConnectionStrings.Resolve(
                configuration.GetConnectionString("Sqlite"), environment.ContentRootPath);
            services.AddDbContext<ApplicationDbContext, SqliteDbContext>(db => db.UseSqlite(connectionString));
        }

        return services;
    }
}

internal static class SqliteConnectionStrings
{
    private const string DefaultConnectionString = "Data Source=App_Data/learnhub.db";

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
