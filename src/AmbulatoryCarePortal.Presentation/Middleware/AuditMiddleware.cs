using System.Diagnostics;
using System.Security.Claims;
using AmbulatoryCarePortal.Application.Interfaces;

namespace AmbulatoryCarePortal.Presentation.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var sw = Stopwatch.StartNew();

        if (context.User.Identity?.IsAuthenticated == true &&
            context.Request.Method != "GET" &&
            !context.Request.Path.StartsWithSegments("/Account"))
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var clinicId = context.User.FindFirst("ClinicId")?.Value;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            _ = Task.Run(async () =>
            {
                try
                {
                    await auditService.LogActionAsync(
                        int.TryParse(clinicId, out var cid) ? cid : 0,
                        context.Request.Method,
                        $"{context.Request.Method} {context.Request.Path}",
                        "HTTP",
                        null,
                        userId,
                        ipAddress
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log audit entry");
                }
            });
        }

        await _next(context);
        sw.Stop();
    }
}
