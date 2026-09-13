using System.Threading.RateLimiting;
using LearnHub.Data;
using LearnHub.Data.Seed;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.Services.Storage;
using LearnHub.ViewModels.Account;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Infrastructure;

/// <summary>Dependency-injection registration, grouped by concern so <c>Program.cs</c> stays short.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLearnHubIdentity(this IServiceCollection services, IHostEnvironment environment)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = PasswordRules.MinimumLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<LearnHubClaimsPrincipalFactory>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "LearnHub.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = SecurePolicy(environment);
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        // Role changes and deactivation update the security stamp; signed-in cookies are re-checked this often.
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(5));

        return services;
    }

    public static IServiceCollection AddLearnHubApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IFileStorageService, FileStorageService>();

        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICourseCatalogService, CourseCatalogService>();
        services.AddScoped<ICourseManagementService, CourseManagementService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<ILearningResourceService, LearningResourceService>();
        services.AddScoped<IResourceManagementService, ResourceManagementService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IQuizManagementService, QuizManagementService>();
        services.AddScoped<IQuestionManagementService, QuestionManagementService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IContactService, ContactService>();

        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    public static IServiceCollection AddLearnHubWeb(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddControllersWithViews(options =>
        {
            // Every POST/PUT/DELETE must carry a valid anti-forgery token (CSRF protection).
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

            // Validation rules are declared explicitly with [Required]; nullable annotations alone add no rules.
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });

        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "LearnHub.Antiforgery";
            options.Cookie.SecurePolicy = SecurePolicy(environment);
        });

        // Allow the largest permitted upload plus form overhead; the storage service enforces exact limits.
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = UploadRules.MaxDocumentBytes + (1024 * 1024));

        var rateLimiting = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Forms, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimiting.PermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimiting.WindowSeconds),
                    QueueLimit = 0
                }));
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            // Static text assets only. HTML pages are not compressed because they contain anti-forgery
            // tokens next to user-controlled text (BREACH-style attacks).
            options.MimeTypes = ["text/css", "text/javascript", "application/javascript", "image/svg+xml"];
        });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    private static CookieSecurePolicy SecurePolicy(IHostEnvironment environment) =>
        environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
}
