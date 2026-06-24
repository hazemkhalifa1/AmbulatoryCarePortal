using System.Security.Claims;
using Serilog.Context;

namespace AmbulatoryCarePortal.Presentation.Middleware;

public class LogContextEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public LogContextEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var _ = LogContext.PushProperty("CorrelationId", context.TraceIdentifier);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var clinicId = context.User.FindFirst("ClinicId")?.Value;

            if (!string.IsNullOrEmpty(userId))
                LogContext.PushProperty("UserId", userId);

            if (!string.IsNullOrEmpty(clinicId))
                LogContext.PushProperty("ClinicId", clinicId);
        }

        await _next(context);
    }
}
