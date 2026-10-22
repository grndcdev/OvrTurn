namespace OvrTurn.App.Security;

/// <summary>
/// Adds OWASP-recommended security response headers to every HTTP response.
/// Must be registered EARLY in the pipeline so headers are present even on error responses.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

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

        // Read the API URL from config so the CSP can allow the Blazor App to call it.
        // Lives in User Secrets / Key Vault, never hard-coded.
        var apiBaseUrl = _configuration["OvrTurnApi:BaseUrl"] ?? string.Empty;

        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            $"connect-src 'self' ws: wss: {apiBaseUrl}; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

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