using AmbulatoryCarePortal.Application.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public class EmailJob
{
    private readonly MailKitEmailSender _mailKitSender;
    private readonly ILogger<EmailJob> _logger;

    public EmailJob(MailKitEmailSender mailKitSender, ILogger<EmailJob> logger)
    {
        _mailKitSender = mailKitSender;
        _logger = logger;
    }

    [DisableConcurrentExecution(30)]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 600], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml)
    {
        _logger.LogInformation("EmailJob sending to {To}: {Subject}", to, subject);
        await _mailKitSender.SendEmailAsync(to, subject, body, isHtml);
    }

    [DisableConcurrentExecution(60)]
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [120, 600], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendBulkEmailAsync(List<string> recipients, string subject, string body, bool isHtml)
    {
        _logger.LogInformation("EmailJob sending bulk to {Count} recipients: {Subject}", recipients.Count, subject);
        await _mailKitSender.SendBulkEmailAsync(recipients, subject, body, isHtml);
    }
}
