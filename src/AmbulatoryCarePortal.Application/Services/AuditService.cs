using AmbulatoryCarePortal.Application.BackgroundJobs;
using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Hangfire;

namespace AmbulatoryCarePortal.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task LogActionAsync(int clinicId, string actionType, string? description,
        string? targetObjectType, int? targetObjectId, string? userId, string? ipAddress)
    {
        BackgroundJob.Enqueue<AuditLogJob>(job =>
            job.LogActionAsync(clinicId, actionType, description, targetObjectType, targetObjectId, userId, ipAddress));
        return Task.CompletedTask;
    }

    public async Task<PagedResult<AuditTrailDto>> GetAuditTrailAsync(int clinicId, int pageNumber = 1, int pageSize = 20, string? searchTerm = null, string? actionTypeFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var repo = _unitOfWork.Repository<AuditTrail>();

        var auditLogs = await repo.FindWithIncludesAsync(
            x => x.ClinicId == clinicId,
            includeDeleted: false,
            includes: new System.Linq.Expressions.Expression<Func<AuditTrail, object>>[]
            {
                x => x.User,
                x => x.Clinic
            }
        );

        var query = auditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(x =>
                (x.Description != null && x.Description.ToLower().Contains(term)) ||
                (x.User != null && (x.User.FullNameEn != null && x.User.FullNameEn.ToLower().Contains(term))) ||
                (x.User != null && (x.User.FullNameAr != null && x.User.FullNameAr.ToLower().Contains(term))) ||
                x.TargetObjectType.ToLower().Contains(term) ||
                (x.IpAddress != null && x.IpAddress.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(actionTypeFilter) && Enum.TryParse<AuditActionType>(actionTypeFilter, out var actionType))
            query = query.Where(x => x.ActionType == actionType);

        if (dateFrom.HasValue)
            query = query.Where(x => x.ActionDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.ActionDate <= dateTo.Value);

        var totalCount = query.Count();

        var data = query
            .OrderByDescending(x => x.ActionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditTrailDto
            {
                Id = x.Id,
                ClinicId = x.ClinicId,
                ClinicName = x.Clinic != null ? x.Clinic.Name : null,
                ActionType = x.ActionType,
                Description = x.Description,
                TargetObjectType = x.TargetObjectType,
                TargetObjectId = x.TargetObjectId,
                UserName = x.User != null ? x.User.FullNameEn : null,
                IpAddress = x.IpAddress,
                ActionDate = x.ActionDate,
                OldValues = x.OldValues,
                NewValues = x.NewValues
            })
            .ToList();

        return new PagedResult<AuditTrailDto>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<AuditTrailDto?> GetAuditTrailByIdAsync(int id)
    {
        var repo = _unitOfWork.Repository<AuditTrail>();
        var trails = await repo.FindWithIncludesAsync(
            x => x.Id == id,
            includeDeleted: false,
            includes: new System.Linq.Expressions.Expression<Func<AuditTrail, object>>[]
            {
                x => x.User,
                x => x.Clinic
            }
        );

        var audit = trails.FirstOrDefault();
        if (audit == null) return null;

        return new AuditTrailDto
        {
            Id = audit.Id,
            ClinicId = audit.ClinicId,
            ClinicName = audit.Clinic?.Name,
            ActionType = audit.ActionType,
            Description = audit.Description,
            TargetObjectType = audit.TargetObjectType,
            TargetObjectId = audit.TargetObjectId,
            UserName = audit.User?.FullNameEn,
            IpAddress = audit.IpAddress,
            ActionDate = audit.ActionDate,
            OldValues = audit.OldValues,
            NewValues = audit.NewValues
        };
    }

    public async Task<int> GetAuditLogCountAsync(int clinicId)
    {
        return await _unitOfWork.Repository<AuditTrail>().CountAsync(x => x.ClinicId == clinicId);
    }

    public async Task<int> GetDistinctUserCountAsync(int clinicId)
    {
        var trails = await _unitOfWork.Repository<AuditTrail>().FindAsync(x => x.ClinicId == clinicId);
        return trails.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().Count();
    }
}
