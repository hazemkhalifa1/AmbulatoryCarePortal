using AmbulatoryCarePortal.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace AmbulatoryCarePortal.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpClient _smtpClient;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];

        _senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@cbahi-portal.com";
        _senderName = _configuration["EmailSettings:SenderName"] ?? "CBAHI Portal";

        _smtpClient = new SmtpClient(smtpServer, smtpPort)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var mailMessage = new MailMessage(_senderEmail, to)
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            await _smtpClient.SendMailAsync(mailMessage);
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
            foreach (var recipient in recipients)
                await SendEmailAsync(recipient, subject, body, isHtml);

            _logger.LogInformation("Bulk email sent to {Count} recipients", recipients.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email");
            return false;
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

    public Task<bool> SendPasswordResetEmailAsync(string to, string resetToken)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password. Use the token below:</p>
            <p><strong>Reset Token:</strong> {resetToken}</p>
            <p>This token will expire in 24 hours.</p>
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
            using var mailMessage = new MailMessage(_senderEmail, to)
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = $"Scheduled Report: {reportName}",
                Body = $@"
                    <h2>Scheduled Report</h2>
                    <p>Your scheduled report is attached:</p>
                    <p><strong>Report Name:</strong> {reportName}</p>
                    <p><strong>Generated Date:</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm}</p>
                    <hr/>
                    <p>This is an automated notification from CBAHI Portal</p>
                ",
                IsBodyHtml = true
            };

            var stream = new MemoryStream(reportContent);
            mailMessage.Attachments.Add(new Attachment(stream, $"{reportName}.pdf", "application/pdf"));

            await _smtpClient.SendMailAsync(mailMessage);
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
