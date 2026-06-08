namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true);
    Task<bool> SendExpiryReminderAsync(string to, string documentName, DateTime expiryDate);
    Task<bool> SendWelcomeEmailAsync(string to, string userName, string tempPassword);
    Task<bool> SendPasswordResetEmailAsync(string to, string resetToken);
    Task<bool> SendComplianceAlertAsync(string to, int clinicId, string alertType, string details);
    Task<bool> SendScheduledReportAsync(string to, string reportName, byte[] reportContent);
}
