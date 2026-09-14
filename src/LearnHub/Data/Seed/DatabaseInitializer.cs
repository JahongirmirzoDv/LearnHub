using LearnHub.Infrastructure;
using LearnHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnHub.Data.Seed;

/// <summary>
/// Runs once at start-up: applies migrations, creates the roles, creates the first administrator from
/// configuration (environment variables, or generated DEMO ONLY passwords in Development – never from source code)
/// and seeds demo content.
/// </summary>
public sealed class DatabaseInitializer(
    ApplicationDbContext db,
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    DemoDataSeeder demoDataSeeder,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<SeedOptions> seedOptions,
    IHostEnvironment environment,
    TimeProvider clock,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (databaseOptions.Value.ApplyMigrationsOnStartup)
        {
            logger.LogInformation("Applying database migrations using {Provider}.", db.Database.ProviderName);
            await db.Database.MigrateAsync(cancellationToken);
        }
        else if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            logger.LogWarning(
                "The database has pending migrations and Database:ApplyMigrationsOnStartup is false. " +
                "Run 'dotnet ef database update' before starting LearnHub. Seeding was skipped.");
            return;
        }

        await EnsureRolesAsync();
        await EnsureAdministratorAsync();

        if (environment.IsDevelopment())
        {
            logger.LogInformation(
                "DEMO ONLY sign-in details for {AdminEmail} and {StudentEmail} are in App_Data/{FileName} unless configured.",
                seedOptions.Value.AdminEmail, seedOptions.Value.DemoStudentEmail, DevelopmentSeedPasswords.FileName);
        }

        if (seedOptions.Value.DemoData)
        {
            await demoDataSeeder.SeedAsync(cancellationToken);
        }
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Role {Role} created.", role);
            }
        }
    }

    private async Task EnsureAdministratorAsync()
    {
        var options = seedOptions.Value;
        if (string.IsNullOrWhiteSpace(options.AdminEmail))
        {
            return;
        }

        var email = options.AdminEmail.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            // Never re-apply roles or passwords to an existing account: admins may have changed them deliberately.
            return;
        }

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            logger.LogWarning(
                "No administrator account exists for {Email}. Set the Seed__AdminPassword environment variable " +
                "(or Seed:AdminPassword with 'dotnet user-secrets'), then restart.",
                email);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = options.AdminFullName,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        var result = await userManager.CreateAsync(admin, options.AdminPassword);
        if (!result.Succeeded)
        {
            logger.LogError(
                "The administrator account could not be created: {Errors}",
                string.Join(" ", result.Errors.Select(error => error.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        logger.LogInformation("Administrator account {Email} created.", email);
    }
}

public static class DatabaseInitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
    }
}
