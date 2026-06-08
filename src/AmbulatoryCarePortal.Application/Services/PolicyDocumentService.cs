using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Interfaces;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs.PolicyDocument;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Services;

public class PolicyDocumentService : IPolicyDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PolicyDocumentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<PolicyDocumentDto>> GetClinicPoliciesAsync(int clinicId, int pageNumber, int pageSize)
    {
        var pagedResult = await _unitOfWork.Repository<PolicyDocument>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: x => x.ClinicId == clinicId,
            orderBy: x => x.CreatedAt,
            ascending: false
        );

        var policies = _mapper.Map<List<PolicyDocumentDto>>(pagedResult.Data);

        return new PagedResult<PolicyDocumentDto>
        {
            Data = policies,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<PolicyDocumentDto>> GetDepartmentPoliciesAsync(int departmentId, int pageNumber, int pageSize)
    {
        var pagedResult = await _unitOfWork.Repository<PolicyDocument>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: x => x.DepartmentId == departmentId,
            orderBy: x => x.CreatedAt,
            ascending: false
        );

        var policies = _mapper.Map<List<PolicyDocumentDto>>(pagedResult.Data);

        return new PagedResult<PolicyDocumentDto>
        {
            Data = policies,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PolicyDocumentDetailDto?> GetPolicyDetailsAsync(int policyId)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(policyId);
        if (policy == null)
            return null;

        return _mapper.Map<PolicyDocumentDetailDto>(policy);
    }

    public async Task<int> CreatePolicyAsync(CreatePolicyDocumentDto dto)
    {
        var policy = _mapper.Map<PolicyDocument>(dto);
        policy.DocumentStatus = DocumentStatus.NeedsReview;
        policy.VersionNumber = 1;

        await _unitOfWork.Repository<PolicyDocument>().AddAsync(policy);
        await _unitOfWork.SaveChangesAsync();

        return policy.Id;
    }

    public async Task<bool> UpdatePolicyAsync(UpdatePolicyDocumentDto dto)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(dto.Id);
        if (policy == null)
            return false;

        policy.Title = dto.Title;
        policy.TitleAr = dto.TitleAr;
        policy.DocumentStatus = dto.DocumentStatus;
        policy.ExpiryDate = dto.ExpiryDate;
        policy.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<PolicyDocument>().Update(policy);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeletePolicyAsync(int policyId)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(policyId);
        if (policy == null)
            return false;

        _unitOfWork.Repository<PolicyDocument>().SoftDelete(policy);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetMissingPoliciesCountAsync(int clinicId)
    {
        return await _unitOfWork.Repository<PolicyDocument>().CountAsync(x =>
            x.ClinicId == clinicId && x.DocumentStatus == DocumentStatus.MissingAttachment
        );
    }

    public async Task<int> GetExpiredPoliciesCountAsync(int clinicId)
    {
        return await _unitOfWork.Repository<PolicyDocument>().CountAsync(x =>
            x.ClinicId == clinicId &&
            x.ExpiryDate.HasValue &&
            x.ExpiryDate < DateTime.Now
        );
    }
}
