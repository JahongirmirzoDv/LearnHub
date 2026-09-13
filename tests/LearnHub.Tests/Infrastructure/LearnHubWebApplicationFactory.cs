using LearnHub.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearnHub.Tests.Infrastructure;

/// <summary>
/// Runs the real LearnHub application in memory. Every factory instance gets its own database (migrations and
/// demo seeding really run) and its own temporary file storage, so test classes can run in parallel without
/// affecting each other.
/// </summary>
/// <remarks>
/// The database is in-memory SQLite by default. When <see cref="SqlServerVariable"/> holds a connection string,
/// each instance instead creates, and afterwards drops, its own database on that server, so CI can run the same
/// tests against SQL Server, the production provider, whose query translation differs from SQLite's.
/// </remarks>
public sealed class LearnHubWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Connection string of a disposable SQL Server instance; any database name in it is replaced.</summary>
    public const string SqlServerVariable = "LEARNHUB_TEST_SQLSERVER";

    // Test-only credentials for throw-away databases.
    public const string AdminEmail = "admin@learnhub.test";
    public const string AdminPassword = "AdminTest-Pass1";
    public const string DemoStudentEmail = "demo.student@learnhub.test";
    public const string DemoStudentPassword = "StudentTest-Pass1";
    public const string NewUserPassword = "NewUserTest-Pass1";

    private readonly DatabaseProvider _provider;
    private readonly string _connectionString;
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "learnhub-tests", Guid.NewGuid().ToString("N"));
    private readonly SqliteConnection? _keepAlive;

    public LearnHubWebApplicationFactory()
    {
        var sqlServer = Environment.GetEnvironmentVariable(SqlServerVariable);
        if (!string.IsNullOrWhiteSpace(sqlServer))
        {
            _provider = DatabaseProvider.SqlServer;
            _connectionString = new SqlConnectionStringBuilder(sqlServer)
            {
                InitialCatalog = $"LearnHubTests_{Guid.NewGuid():N}"
            }.ConnectionString;
            return;
        }

        _provider = DatabaseProvider.Sqlite;
        _connectionString = $"Data Source=learnhub-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        // A shared in-memory SQLite database lives only while at least one connection is open.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting values reach WebApplication.CreateBuilder as configuration, before Program.cs reads them.
        builder.UseSetting("Database:Provider", _provider.ToString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting($"ConnectionStrings:{_provider}", _connectionString);
        builder.UseSetting("Storage:RootPath", _storageRoot);
        builder.UseSetting("Seed:DemoData", "true");
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Seed:DemoStudentEmail", DemoStudentEmail);
        builder.UseSetting("Seed:DemoStudentPassword", DemoStudentPassword);
        builder.UseSetting("RateLimiting:PermitLimit", "100000");
    }

    /// <summary>A client that keeps cookies, does not follow redirects and uses HTTPS (cookies are Secure outside Development).</summary>
    public HttpClient CreateBrowserClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true
    });

    public async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(db);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (_keepAlive is not null)
        {
            await _keepAlive.DisposeAsync();
        }
        else
        {
            var options = new DbContextOptionsBuilder<SqlServerDbContext>().UseSqlServer(_connectionString).Options;
            await using var db = new SqlServerDbContext(options);
            await db.Database.EnsureDeletedAsync();
        }

        try
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }
}
