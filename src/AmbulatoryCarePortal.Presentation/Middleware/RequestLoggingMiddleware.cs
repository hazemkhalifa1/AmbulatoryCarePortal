using System.Diagnostics;
using System.Security.Claims;

namespace AmbulatoryCarePortal.Presentation.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var clinicId = context.User.FindFirst("ClinicId")?.Value ?? "none";
        var method = context.Request.Method;
        var path = context.Request.Path;
        var statusCode = context.Response.StatusCode;
        var elapsed = sw.ElapsedMilliseconds;

        if (statusCode >= 500)
        {
            _logger.LogError(LoggingEvents.ServerError,
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms [User={UserId}, Clinic={ClinicId}]",
                method, path, statusCode, elapsed, userId, clinicId);
        }
        else if (statusCode >= 400)
        {
            _logger.LogWarning(LoggingEvents.ClientError,
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms [User={UserId}, Clinic={ClinicId}]",
                method, path, statusCode, elapsed, userId, clinicId);
        }
        else
        {
            _logger.LogInformation(LoggingEvents.Request,
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms [User={UserId}, Clinic={ClinicId}]",
                method, path, statusCode, elapsed, userId, clinicId);
        }
    }
}

public static class LoggingEvents
{
    public const int Request = 1000;
    public const int ServerError = 1001;
    public const int ClientError = 1002;
    public const int Authentication = 2000;
    public const int AuthorizationFailure = 2001;
    public const int AuditAction = 3000;
    public const int EmailSent = 4000;
    public const int EmailFailed = 4001;
    public const int BackgroundJobStarted = 5000;
    public const int BackgroundJobCompleted = 5001;
    public const int BackgroundJobFailed = 5002;
    public const int CacheHit = 6000;
    public const int CacheMiss = 6001;
}
