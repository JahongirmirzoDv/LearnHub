using System.Threading.RateLimiting;
using LearnHub.Data;
using LearnHub.Data.Seed;
using LearnHub.Models;
using LearnHub.Services;
using LearnHub.Services.Storage;
using LearnHub.ViewModels.Account;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IPostConfigureOptions<SeedOptions>, DevelopmentSeedPasswords>();
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

        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // One fixed window per client IP address for login, registration and contact form posts.
            options.AddPolicy(RateLimitPolicies.Forms, context =>
            {
                var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueLimit = 0
                    });
            });
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            // Static text assets only. HTML pages are not compressed because they contain anti-forgery
            // tokens next to user-controlled text (BREACH-style attacks).
            options.MimeTypes = ["text/css", "text/javascript", "application/javascript", "image/svg+xml"];
        });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        // Data Protection keys encrypt the sign-in and anti-forgery cookies. In a container they must live on
        // persistent storage (DataProtection__KeysPath=/data/keys), or every redeployment signs everybody out.
        var dataProtection = services.AddDataProtection().SetApplicationName("LearnHub");
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keysPath, environment.ContentRootPath)));
        }

        return services;
    }

    /// <summary>
    /// Platforms such as Railway choose the port at run time and pass it in the <c>PORT</c> variable; the app must
    /// listen on it on every network interface. Without <c>PORT</c> the usual ASP.NET Core URL settings apply.
    /// </summary>
    public static WebApplicationBuilder UsePlatformPort(this WebApplicationBuilder builder)
    {
        if (int.TryParse(builder.Configuration["PORT"], out var port) && port is > 0 and <= 65535)
        {
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        }

        return builder;
    }

    private static CookieSecurePolicy SecurePolicy(IHostEnvironment environment) =>
        environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
}
