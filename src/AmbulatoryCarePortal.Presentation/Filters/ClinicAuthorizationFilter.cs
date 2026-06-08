using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmbulatoryCarePortal.Presentation.Filters;

public class ClinicAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<ClinicAuthorizationFilter> _logger;

    public ClinicAuthorizationFilter(ILogger<ClinicAuthorizationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
            return;

        var userClinicId = user.FindFirst("ClinicId")?.Value;
        var isSuperAdmin = user.IsInRole("SuperAdmin");

        if (isSuperAdmin || string.IsNullOrEmpty(userClinicId))
            return;

        var routeData = context.RouteData.Values;
        var area = routeData["area"]?.ToString();

        if (area == "ClinicAdmin")
        {
            var clinicIdParam = context.HttpContext.Request.Query["clinicId"].FirstOrDefault()
                              ?? context.HttpContext.Request.RouteValues["clinicId"]?.ToString();

            if (!string.IsNullOrEmpty(clinicIdParam) && clinicIdParam != userClinicId)
            {
                _logger.LogWarning(
                    "Clinic authorization failed: User {UserId} tried to access clinic {TargetClinic}",
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    clinicIdParam
                );

                context.Result = new ForbidResult();
            }
        }

        await Task.CompletedTask;
    }
}
