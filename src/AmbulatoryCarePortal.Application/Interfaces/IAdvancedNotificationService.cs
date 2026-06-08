using AmbulatoryCarePortal.Application.DTOs.Analytics;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IAdvancedNotificationService
{
    Task SendBulkNotificationAsync(List<string> userIds, string message, string type);
    Task SendScheduledNotificationAsync(string userId, string message, DateTime scheduledTime);
    Task SendNotificationBasedOnEventAsync(string eventType, object eventData);
    Task<List<NotificationSettingDto>> GetUserNotificationPreferencesAsync(string userId);
}
