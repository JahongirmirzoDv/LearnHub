using LearnHub.Services.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;

namespace LearnHub.Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();

    /// <summary>
    /// Railway (like any managed host) terminates TLS at its edge proxy and forwards plain HTTP, describing the
    /// original request in <c>X-Forwarded-Proto</c> and <c>X-Forwarded-For</c>. ASP.NET Core does not read those
    /// headers on its own, so without this the app would treat every request as insecure and
    /// <c>UseHttpsRedirection</c> would redirect to itself forever.
    /// </summary>
    /// <remarks>
    /// The platform proxy is the only route into the container, so its headers are trusted wholesale: the default
    /// loopback-only allow-list would reject them, because the proxy arrives from a private-network address. This
    /// must run before anything that inspects the scheme, the client address or the generated links.
    /// </remarks>
    public static IApplicationBuilder UsePlatformProxyHeaders(this IApplicationBuilder app) =>
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            KnownIPNetworks = { },
            KnownProxies = { }
        });

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
