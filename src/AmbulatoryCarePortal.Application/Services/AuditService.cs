using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogActionAsync(int clinicId, string actionType, string? description,
        string? targetObjectType, int? targetObjectId, string? userId, string? ipAddress)
    {
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
    }

    public async Task<List<object>> GetAuditTrailAsync(int clinicId, int pageSize = 50)
    {
        var trails = await _unitOfWork.Repository<AuditTrail>().FindAsync(
            x => x.ClinicId == clinicId,
            includeDeleted: false
        );

        return trails
            .OrderByDescending(x => x.ActionDate)
            .Take(pageSize)
            .Select(x => (object)new
            {
                x.Id,
                x.ActionType,
                x.Description,
                x.TargetObjectType,
                x.TargetObjectId,
                UserName = x.User?.FullNameEn,
                x.IpAddress,
                x.ActionDate
            })
            .ToList();
    }
}
