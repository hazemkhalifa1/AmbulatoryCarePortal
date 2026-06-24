using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AmbulatoryCarePortal.Presentation.HealthChecks;

public class ApplicationHealthCheck : IHealthCheck
{
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var uptime = DateTime.UtcNow - _startTime;

        var data = new Dictionary<string, object>
        {
            { "uptime", uptime.ToString(@"dd\.hh\:mm\:ss") },
            { "startedAt", _startTime.ToString("O") }
        };

        return Task.FromResult(HealthCheckResult.Healthy("Application is running", data));
    }
}
