namespace OvrTurn.Api.Security;

/// <summary>
/// Adds OWASP-recommended security response headers to every HTTP response.
/// Must be registered EARLY in the pipeline so headers are present even on error responses.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevents the browser from MIME-sniffing a response away from the declared Content-Type.
        headers["X-Content-Type-Options"] = "nosniff";

        // Blocks the site from being rendered in an <iframe>. Defeats clickjacking.
        headers["X-Frame-Options"] = "DENY";

        // Only send the origin (no path/query) as Referer when navigating cross-origin.
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Opt out of browser features we do not use. Reduces attack surface.
        headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

        // Content Security Policy — Blazor Server needs 'self' plus inline+eval for its boot script and SignalR.
        // Tightened later when we lock down scripts.
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Removes the "Server: Kestrel" header that leaks server identity to attackers.
        headers.Remove("Server");

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}