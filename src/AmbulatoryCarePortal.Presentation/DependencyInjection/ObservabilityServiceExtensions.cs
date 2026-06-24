using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Presentation.HealthChecks;

namespace AmbulatoryCarePortal.Presentation.DependencyInjection;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation());

        var redisConn = configuration.GetValue<string>("Redis:ConnectionString")
            ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        var healthChecks = services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "database", tags: ["ready", "database"])
            .AddCheck<ApplicationHealthCheck>("self", tags: ["live"]);

        if (!string.IsNullOrEmpty(redisConn))
        {
            healthChecks.AddRedis(
                redisConn,
                name: "redis",
                tags: ["ready", "cache"]);
        }

        return services;
    }
}
