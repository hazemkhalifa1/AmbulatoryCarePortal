using AmbulatoryCarePortal.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AmbulatoryCarePortal.Application.Services;

public class MailKitEmailSender
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(ISettingsService settingsService, ILogger<MailKitEmailSender> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    private async Task<SmtpClient> CreateSmtpClientAsync()
    {
        var smtpServer = await _settingsService.GetValueAsync("Smtp.Host") ?? "localhost";
        var smtpPort = await _settingsService.GetValueAsync("Smtp.Port", 587);
        var enableSsl = await _settingsService.GetValueAsync("Smtp.EnableSsl", true);
        var username = await _settingsService.GetValueAsync("Smtp.Username");
        var password = await _settingsService.GetValueAsync("Smtp.Password");

        var client = new SmtpClient();

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            await client.ConnectAsync(smtpServer, smtpPort, enableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None);
            await client.AuthenticateAsync(username, password);
        }
        else
        {
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTlsWhenAvailable);
        }

        return client;
    }

    private async Task<string> GetSenderEmailAsync()
    {
        return await _settingsService.GetValueAsync("Smtp.FromAddress") ?? "noreply@cbahi-portal.com";
    }

    private async Task<string> GetSenderNameAsync()
    {
        return await _settingsService.GetValueAsync("Smtp.FromName") ?? "CBAHI Portal";
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var client = await CreateSmtpClientAsync();
            var senderEmail = await GetSenderEmailAsync();
            var senderName = await GetSenderNameAsync();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
                bodyBuilder.HtmlBody = body;
            else
                bodyBuilder.TextBody = body;
            message.Body = bodyBuilder.ToMessageBody();

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var client = await CreateSmtpClientAsync();
            var senderEmail = await GetSenderEmailAsync();
            var senderName = await GetSenderNameAsync();

            foreach (var recipient in recipients)
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", recipient));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;
                message.Body = bodyBuilder.ToMessageBody();

                await client.SendAsync(message);
            }

            await client.DisconnectAsync(true);
            _logger.LogInformation("Bulk email sent to {Count} recipients", recipients.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email");
            return false;
        }
    }

    public async Task<(bool Success, string Message)> SendTestEmailAsync(string toAddress)
    {
        try
        {
            var subject = "CBAHI Portal - Test Email";
            var body = $@"
                <h2>Test Email</h2>
                <p>This is a test email from CBAHI Portal.</p>
                <p>If you received this email, your SMTP configuration is working correctly.</p>
                <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <hr/>
                <p>This is an automated notification from CBAHI Portal</p>
            ";

            var success = await SendEmailAsync(toAddress, subject, body);
            return success
                ? (true, "Test email sent successfully")
                : (false, "Failed to send test email. Check SMTP configuration.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email to {To}", toAddress);
            return (false, $"Failed to send test email: {ex.Message}");
        }
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

    public async Task<bool> SendScheduledReportAsync(string to, string reportName, byte[] reportContent)
    {
        try
        {
            using var client = await CreateSmtpClientAsync();
            var senderEmail = await GetSenderEmailAsync();
            var senderName = await GetSenderNameAsync();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = $"Scheduled Report: {reportName}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <h2>Scheduled Report</h2>
                    <p>Your scheduled report is attached:</p>
                    <p><strong>Report Name:</strong> {reportName}</p>
                    <p><strong>Generated Date:</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm}</p>
                    <hr/>
                    <p>This is an automated notification from CBAHI Portal</p>
                "
            };

            bodyBuilder.Attachments.Add($"{reportName}.pdf", reportContent, new ContentType("application", "pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Scheduled report sent to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send scheduled report to {To}", to);
            return false;
        }
    }
}
