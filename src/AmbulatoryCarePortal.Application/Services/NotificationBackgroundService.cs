using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();

                await backgroundJobService.ScheduleDocumentExpiryCheckAsync();
                await backgroundJobService.ScheduleComplianceAlertAsync();
                await backgroundJobService.ScheduleChecklistRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationBackgroundService cycle");
            }

            var intervalMinutes = _configuration.GetValue<int>("NotificationSettings:CheckIntervalMinutes", 60);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Notification Background Service stopped");
    }
}
