using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task SendNotificationAsync(int clinicId, string title, string message,
        string? messageAr, string notificationType, int? targetObjectId, string targetObjectType)
    {
        if (!Enum.TryParse<NotificationType>(notificationType, out var notifType))
            notifType = NotificationType.SystemUpdate;

        var notification = new Notification
        {
            ClinicId = clinicId,
            Title = title,
            Message = message,
            MessageAr = messageAr,
            NotificationType = notifType,
            TargetObjectId = targetObjectId,
            TargetObjectType = targetObjectType,
            IsRead = false,
            ReadAt = null
        };

        await _unitOfWork.Repository<Notification>().AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId)
    {
        var notifications = await _unitOfWork.Repository<Notification>().FindAsync(
            x => x.UserId == userId,
            includeDeleted: false
        );

        return notifications
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                ClinicId = x.ClinicId,
                Title = x.Title,
                Message = x.Message,
                MessageAr = x.MessageAr,
                NotificationType = x.NotificationType,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt,
                UserId = x.UserId,
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _unitOfWork.Repository<Notification>().GetByIdAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _unitOfWork.Repository<Notification>().Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _unitOfWork.Repository<Notification>().FindAsync(
            x => x.UserId == userId && !x.IsRead
        );

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        _unitOfWork.Repository<Notification>().UpdateRange(notifications);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(int clinicId, string userId)
    {
        return await _unitOfWork.Repository<Notification>().CountAsync(
            x => x.ClinicId == clinicId && x.UserId == userId && !x.IsRead
        );
    }
}
