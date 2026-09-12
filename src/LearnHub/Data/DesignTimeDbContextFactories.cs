using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnHub.Data;

/// <summary>
/// Used only by the <c>dotnet ef</c> tools. Having a factory per provider means migrations for
/// both providers can be created regardless of which provider the running app is configured for:
/// <code>
/// dotnet ef migrations add Name --context SqliteDbContext    --output-dir Data/Migrations/Sqlite
/// dotnet ef migrations add Name --context SqlServerDbContext --output-dir Data/Migrations/SqlServer
/// </code>
/// </summary>
public sealed class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build();
        var connectionString = SqliteConnectionStrings.Resolve(
            configuration.GetConnectionString("Sqlite"), Directory.GetCurrentDirectory());

        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new SqliteDbContext(options);
    }
}

public sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerDbContext>
{
    // Generating a migration does not open a connection; applying one requires the real connection string.
    private const string PlaceholderConnectionString =
        "Server=localhost;Database=LearnHub;Integrated Security=true;TrustServerCertificate=true";

    public SqlServerDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build();
        var connectionString = configuration.GetConnectionString("SqlServer");

        var options = new DbContextOptionsBuilder<SqlServerDbContext>()
            .UseSqlServer(string.IsNullOrWhiteSpace(connectionString) ? PlaceholderConnectionString : connectionString)
            .Options;

        return new SqlServerDbContext(options);
    }
}

internal static class DesignTimeConfiguration
{
    public static IConfiguration Build()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<SqliteDesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
