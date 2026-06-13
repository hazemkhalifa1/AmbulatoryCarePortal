using AmbulatoryCarePortal.Application.BackgroundJobs;
using AmbulatoryCarePortal.Application.Interfaces;
using Hangfire;

namespace AmbulatoryCarePortal.Application.Services;

public class HangfireEmailService : IEmailService
{
    public Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        BackgroundJob.Enqueue<EmailJob>(job => job.SendEmailAsync(to, subject, body, isHtml));
        return Task.FromResult(true);
    }

    public Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true)
    {
        BackgroundJob.Enqueue<EmailJob>(job => job.SendBulkEmailAsync(recipients, subject, body, isHtml));
        return Task.FromResult(true);
    }

    public Task<bool> SendExpiryReminderAsync(string to, string documentName, DateTime expiryDate)
    {
        var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;
        var subject = $"Document Expiry Alert: {documentName}";
        var body = $@"
            <h2>Document Expiry Alert</h2>
            <p>The following document will expire soon:</p>
            <p><strong>Document Name:</strong> {documentName}</p>
            <p><strong>Expiry Date:</strong> {expiryDate:dd/MM/yyyy}</p>
            <p><strong>Days Remaining:</strong> {daysUntilExpiry}</p>
            <p>Please take action to renew this document before the expiry date.</p>
            <hr/>
            <p>This is an automated notification from CBAHI Portal</p>
        ";
        return SendEmailAsync(to, subject, body);
    }

    public Task<bool> SendWelcomeEmailAsync(string to, string userName, string tempPassword)
    {
        var subject = "Your CBAHI Portal Account Has Been Created";
        var body = $@"
            <h2>Welcome to CBAHI Portal</h2>
            <p>Hello {userName},</p>
            <p>Your account has been successfully created.</p>
            <p><strong>Email:</strong> {to}</p>
            <p><strong>Temporary Password:</strong> {tempPassword}</p>
            <p>Please login and change your password immediately.</p>
            <hr/>
            <p>For security, please do not share this email with others.</p>
        ";
        return SendEmailAsync(to, subject, body);
    }

    public Task<bool> SendPasswordResetEmailAsync(string to, string callbackUrl)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password.</p>
            <p>Click the link below to reset your password:</p>
            <p><a href='{callbackUrl}'>Reset Password</a></p>
            <p>This link will expire in 24 hours.</p>
            <p>If you did not request this, please ignore this email.</p>
            <hr/>
            <p>This is an automated notification from CBAHI Portal</p>
        ";
        return SendEmailAsync(to, subject, body);
    }

    public Task<bool> SendComplianceAlertAsync(string to, int clinicId, string alertType, string details)
    {
        var subject = $"Compliance Alert: {alertType}";
        var body = $@"
            <h2>Compliance Alert</h2>
            <p><strong>Clinic ID:</strong> {clinicId}</p>
            <p><strong>Alert Type:</strong> {alertType}</p>
            <p><strong>Details:</strong></p>
            <p>{details}</p>
            <p>Please review and take appropriate action.</p>
            <hr/>
            <p>This is an automated notification from CBAHI Portal</p>
        ";
        return SendEmailAsync(to, subject, body);
    }

    public Task<bool> SendScheduledReportAsync(string to, string reportName, byte[] reportContent)
    {
        BackgroundJob.Enqueue<EmailJob>(job => job.SendEmailAsync(to, $"Scheduled Report: {reportName}",
            $"<p>Your scheduled report <strong>{reportName}</strong> is attached.</p>", true));
        return Task.FromResult(true);
    }

    public async Task<(bool Success, string Message)> SendTestEmailAsync(string toAddress)
    {
        BackgroundJob.Enqueue<EmailJob>(job => job.SendEmailAsync(toAddress, "CBAHI Portal - Test Email",
            "<p>If you see this, SMTP is working.</p>", true));
        await Task.CompletedTask;
        return (true, "Test email queued for delivery via Hangfire");
    }
}
