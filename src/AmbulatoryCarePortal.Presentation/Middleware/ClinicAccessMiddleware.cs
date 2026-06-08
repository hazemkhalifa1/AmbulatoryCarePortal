using System.Security.Claims;
using AmbulatoryCarePortal.Application.Interfaces;

namespace AmbulatoryCarePortal.Presentation.Middleware;

public class ClinicAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClinicAccessMiddleware> _logger;

    public ClinicAccessMiddleware(RequestDelegate next, ILogger<ClinicAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userClinicId = context.User.FindFirst("ClinicId")?.Value;
            var isSuperAdmin = context.User.IsInRole("SuperAdmin");

            if (!isSuperAdmin && !string.IsNullOrEmpty(userClinicId))
            {
                var path = context.Request.Path.Value ?? "";
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < segments.Length - 1; i++)
                {
                    if (int.TryParse(segments[i], out _) && i > 0)
                    {
                        var routeClinicId = segments[i];
                        if (routeClinicId != userClinicId)
                        {
                            _logger.LogWarning(
                                "Clinic access violation: User {UserId} tried to access clinic {TargetClinic} from clinic {UserClinic}",
                                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                                routeClinicId,
                                userClinicId
                            );

                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = "Access denied. You can only access your own clinic's data."
                            });
                            return;
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}
