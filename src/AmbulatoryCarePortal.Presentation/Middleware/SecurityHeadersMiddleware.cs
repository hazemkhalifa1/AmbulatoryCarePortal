namespace AmbulatoryCarePortal.Presentation.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "0";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            var csp = "default-src 'self'; " +
                      "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://code.jquery.com https://cdn.jsdelivr.net; " +
                      "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
                      "font-src 'self' https://cdnjs.cloudflare.com https://fonts.gstatic.com; " +
                      "img-src 'self' data:; " +
                      "connect-src 'self'; " +
                      "frame-src 'none'; " +
                      "object-src 'none'; " +
                      "base-uri 'self'; " +
                      "form-action 'self'";
            context.Response.Headers["Content-Security-Policy"] = csp;

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
