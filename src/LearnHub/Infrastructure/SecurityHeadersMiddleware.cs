namespace LearnHub.Infrastructure;

/// <summary>
/// Adds defensive HTTP headers to every response. The Content-Security-Policy only allows scripts,
/// styles and fonts from this site (no inline script) and restricts embedded frames to the
/// privacy-enhanced YouTube and Vimeo players, which blocks most XSS payloads even if one slipped through.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "media-src 'self'; " +
        "frame-src 'self' https://www.youtube-nocookie.com https://player.vimeo.com; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // CSP is meaningful for documents; applying it to streamed PDFs stops browsers rendering them inline.
            var contentType = context.Response.ContentType;
            if (contentType is null
                || contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase)
                || contentType.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase))
            {
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
