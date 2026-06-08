using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? MessageAr { get; set; }
    public NotificationType NotificationType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
