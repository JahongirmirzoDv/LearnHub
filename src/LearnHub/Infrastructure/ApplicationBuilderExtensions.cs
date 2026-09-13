using LearnHub.Services.Storage;
using Microsoft.Extensions.FileProviders;

namespace LearnHub.Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();

    /// <summary>
    /// Serves <c>wwwroot</c> plus uploaded course thumbnails. Uploaded learning-resource files are deliberately
    /// NOT served as static files: they are streamed by <c>ResourcesController</c> after an access check.
    /// </summary>
    public static IApplicationBuilder UseLearnHubStaticFiles(this IApplicationBuilder app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                // asp-append-version adds a content hash (?v=…), so versioned files can be cached for a year.
                if (context.Context.Request.Query.ContainsKey("v"))
                {
                    context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
                }
            }
        });

        var storage = app.ApplicationServices.GetRequiredService<IFileStorageService>();
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = FileStorageService.ThumbnailRequestPath,
            FileProvider = new PhysicalFileProvider(storage.ThumbnailsPath),
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public,max-age=604800"
        });

        return app;
    }

    public static IEndpointRouteBuilder MapLearnHubRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");

        endpoints.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

        // Friendly top-level URLs for the public information pages.
        endpoints.MapControllerRoute("about", "About", new { controller = "Home", action = "About" });
        endpoints.MapControllerRoute("contact", "Contact", new { controller = "Home", action = "Contact" });
        endpoints.MapControllerRoute("privacy", "Privacy", new { controller = "Home", action = "Privacy" });

        endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        return endpoints;
    }
}
