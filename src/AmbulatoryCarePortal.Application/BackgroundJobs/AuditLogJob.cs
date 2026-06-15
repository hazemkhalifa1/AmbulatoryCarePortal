using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.BackgroundJobs;

public class AuditLogJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogJob> _logger;

    public AuditLogJob(IUnitOfWork unitOfWork, ILogger<AuditLogJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [DisableConcurrentExecution(15)]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [10, 30, 60], OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task LogActionAsync(int clinicId, string actionType, string? description,
        string? targetObjectType, int? targetObjectId, string? userId, string? ipAddress)
    {
        if (clinicId <= 0)
        {
            _logger.LogWarning("Skipping audit log: invalid ClinicId {ClinicId}", clinicId);
            return;
        }

        if (!Enum.TryParse<AuditActionType>(actionType, out var auditAction))
            auditAction = AuditActionType.Create;

        var auditTrail = new AuditTrail
        {
            ClinicId = clinicId,
            ActionType = auditAction,
            Description = description,
            TargetObjectType = targetObjectType ?? string.Empty,
            TargetObjectId = targetObjectId,
            UserId = userId,
            IpAddress = ipAddress,
            ActionDate = DateTime.UtcNow
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditTrail);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Audit log created: {Action} on {Target} by {User}", actionType, targetObjectType, userId);
    }
}
