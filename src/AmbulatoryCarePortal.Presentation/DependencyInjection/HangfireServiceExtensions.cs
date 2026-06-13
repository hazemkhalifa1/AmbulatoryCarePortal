using Hangfire;
using Microsoft.Extensions.Configuration;
using AmbulatoryCarePortal.Application.BackgroundJobs;

namespace AmbulatoryCarePortal.Presentation.DependencyInjection;

public static class HangfireServiceExtensions
{
    public static IServiceCollection AddHangfireJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        services.AddHangfire(config =>
        {
            config.UseSqlServerStorage(connectionString);
            config.UseSerilogLogProvider();
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
            options.Queues = ["default", "critical", "notifications", "reports"];
        });

        return services;
    }
}
