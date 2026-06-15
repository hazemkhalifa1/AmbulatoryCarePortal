using System.Diagnostics;
using System.Security.Claims;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Enums;

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

        await _next(context);

        sw.Stop();

        if (context.User.Identity?.IsAuthenticated == true &&
            context.Request.Method != "GET")
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var clinicIdClaim = context.User.FindFirst("ClinicId")?.Value;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            if (!int.TryParse(clinicIdClaim, out var clinicId) || clinicId <= 0)
            {
                return;
            }

            var actionType = context.Request.Method switch
            {
                "POST" => AuditActionType.Create,
                "PUT" => AuditActionType.Update,
                "PATCH" => AuditActionType.Update,
                "DELETE" => AuditActionType.Delete,
                _ => AuditActionType.Create
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await auditService.LogActionAsync(
                        clinicId,
                        actionType.ToString(),
                        $"{context.Request.Method} {context.Request.Path} ({sw.ElapsedMilliseconds}ms)",
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
    }
}
