using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using AmbulatoryCarePortal.Infrastructure.Data;

namespace AmbulatoryCarePortal.Presentation.DependencyInjection;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation());

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "database", tags: ["ready"]);

        return services;
    }
}
