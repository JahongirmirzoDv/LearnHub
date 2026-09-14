using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnHub.Data;

/// <summary>
/// Used only by the <c>dotnet ef</c> tools, so migrations can be created and applied without starting the web app:
/// <code>
/// dotnet ef migrations add Name --project src/LearnHub --output-dir Data/Migrations
/// dotnet ef database update --project src/LearnHub
/// </code>
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = SqliteConnectionStrings.Resolve(
            SqliteConnectionStrings.FromConfiguration(configuration), Directory.GetCurrentDirectory());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
