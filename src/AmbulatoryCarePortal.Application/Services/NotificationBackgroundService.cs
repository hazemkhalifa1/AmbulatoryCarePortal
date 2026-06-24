using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.Settings;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<NotificationSettings> _notificationSettings;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<NotificationSettings> notificationSettings,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _notificationSettings = notificationSettings;
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

            await Task.Delay(TimeSpan.FromMinutes(_notificationSettings.Value.CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Notification Background Service stopped");
    }
}
