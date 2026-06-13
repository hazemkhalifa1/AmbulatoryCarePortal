using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace AmbulatoryCarePortal.Presentation.Middleware;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && (httpContext.User.IsInRole("SuperAdmin")
                || httpContext.User.HasClaim("Permission", "system.configure"));
    }
}
