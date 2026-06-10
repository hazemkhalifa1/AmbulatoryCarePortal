using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.DTOs.Clinic;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AmbulatoryCarePortal.Application.Services;

public class ClinicService : IClinicService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<AppUser> _userManager;

    public ClinicService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<AppUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<PagedResult<ClinicDto>> GetAllClinicsAsync(int pageNumber, int pageSize)
    {
        var pagedResult = await _unitOfWork.Repository<Clinic>().GetPagedAsync(
            pageNumber,
            pageSize,
            orderBy: x => x.Name,
            ascending: true
        );

        var clinics = _mapper.Map<List<ClinicDto>>(pagedResult.Data);

        return new PagedResult<ClinicDto>
        {
            Data = clinics,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ClinicDetailDto?> GetClinicDetailsAsync(int clinicId)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        if (clinic == null)
            return null;

        var userCount = await _userManager.Users.CountAsync(x => x.ClinicId == clinicId);
        var departmentCount = await _unitOfWork.Repository<Department>().CountAsync(x => x.ClinicId == clinicId);
        var policyCount = await _unitOfWork.Repository<PolicyDocument>().CountAsync(x => x.ClinicId == clinicId);
        var openGapCount = await _unitOfWork.Repository<PolicyDocument>().CountAsync(x =>
            x.ClinicId == clinicId &&
            (x.DocumentStatus == DocumentStatus.MissingAttachment ||
             x.DocumentStatus == DocumentStatus.Expired ||
             x.DocumentStatus == DocumentStatus.NeedsReview)
        );

        var dto = _mapper.Map<ClinicDetailDto>(clinic);
        dto.UserCount = userCount;
        dto.DepartmentCount = departmentCount;
        dto.PolicyDocumentCount = policyCount;
        dto.OpenGapCount = openGapCount;

        return dto;
    }

    public async Task<int> CreateClinicAsync(CreateClinicDto dto)
    {
        var clinic = _mapper.Map<Clinic>(dto);
        clinic.IsActive = true;
        clinic.ComplianceScore = 0;

        await _unitOfWork.Repository<Clinic>().AddAsync(clinic);
        await _unitOfWork.SaveChangesAsync();

        // Create default departments for ambulatory care
        if (clinic.ClinicType == ClinicType.Ambulatory || clinic.ClinicType == ClinicType.Specialty)
        {
            await CreateDefaultDepartmentsAsync(clinic.Id);
        }
        // Create dental-specific departments for dental centers
        else if (clinic.ClinicType == ClinicType.DentalCenter)
        {
            await CreateDentalDepartmentsAsync(clinic.Id);
        }

        // Auto-assign active document templates to the new clinic
        var activeTemplates = await _unitOfWork.Repository<DocumentTemplate>().FindAsync(t => t.IsActive);
        if (activeTemplates.Any())
        {
            var clinicDocuments = activeTemplates.Select(t => new ClinicDocument
            {
                ClinicId = clinic.Id,
                DocumentTemplateId = t.Id,
                DocumentStatus = Domain.Enums.ClinicDocumentStatus.NeedsReview,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _unitOfWork.Repository<ClinicDocument>().AddRangeAsync(clinicDocuments);
            await _unitOfWork.SaveChangesAsync();
        }

        return clinic.Id;
    }

    public async Task<bool> UpdateClinicAsync(UpdateClinicDto dto)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(dto.Id);
        if (clinic == null)
            return false;

        _mapper.Map(dto, clinic);
        clinic.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Clinic>().Update(clinic);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteClinicAsync(int clinicId)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        if (clinic == null)
            return false;

        _unitOfWork.Repository<Clinic>().SoftDelete(clinic);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<decimal> CalculateComplianceScoreAsync(int clinicId)
    {
        var totalPolicies = await _unitOfWork.Repository<PolicyDocument>().CountAsync(x => x.ClinicId == clinicId);

        if (totalPolicies == 0)
            return 0;

        var completePolicies = await _unitOfWork.Repository<PolicyDocument>().CountAsync(x =>
            x.ClinicId == clinicId && x.DocumentStatus == DocumentStatus.Complete
        );

        var score = (completePolicies * 100m) / totalPolicies;

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
        if (clinic != null)
        {
            clinic.ComplianceScore = Math.Round(score, 2);
            _unitOfWork.Repository<Clinic>().Update(clinic);
            await _unitOfWork.SaveChangesAsync();
        }

        return Math.Round(score, 2);
    }

    private async Task CreateDefaultDepartmentsAsync(int clinicId)
    {
        var departments = new[]
        {
            (DepartmentCodeEnum.LD, "Leadership of the Organization", "قيادة المنظمة"),
            (DepartmentCodeEnum.PC, "Provision of Care", "تقديم الرعاية"),
            (DepartmentCodeEnum.LB, "Laboratory", "المختبر"),
            (DepartmentCodeEnum.RD, "Radiology Department", "قسم الأشعات"),
            (DepartmentCodeEnum.DN, "Dental", "الأسنان"),
            (DepartmentCodeEnum.MM, "Medication Management", "إدارة الأدوية"),
            (DepartmentCodeEnum.MOI, "Management of Information", "إدارة المعلومات"),
            (DepartmentCodeEnum.IPC, "Infection Prevention and Control", "الوقاية من العدوى والتحكم بها"),
            (DepartmentCodeEnum.FMS, "Facility Management and Safety", "إدارة المرافق والسلامة"),
            (DepartmentCodeEnum.DPU, "Dialysis Patient Unit", "وحدة مرضى غسيل الكلى"),
            (DepartmentCodeEnum.DA, "Dental Anesthesia", "تخدير الأسنان")
        };

        var departmentEntities = departments.Select(d => new Department
        {
            NameEn = d.Item2,
            NameAr = d.Item3,
            DepartmentCode = d.Item1,
            ClinicId = clinicId,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _unitOfWork.Repository<Department>().AddRangeAsync(departmentEntities);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task CreateDentalDepartmentsAsync(int clinicId)
    {
        var departments = new[]
        {
            (DepartmentCodeEnum.LD, "Leadership of the Organization", "قيادة المنظمة"),
            (DepartmentCodeEnum.PC, "Provision of Care", "تقديم الرعاية"),
            (DepartmentCodeEnum.DL, "Dental Laboratory", "معمل الأسنان"),
            (DepartmentCodeEnum.MOI, "Management of Information", "إدارة المعلومات"),
            (DepartmentCodeEnum.IPC, "Infection Prevention and Control", "الوقاية من العدوى والتحكم بها"),
            (DepartmentCodeEnum.FMS, "Facility Management and Safety", "إدارة المرافق والسلامة")
        };

        var departmentEntities = departments.Select(d => new Department
        {
            NameEn = d.Item2,
            NameAr = d.Item3,
            DepartmentCode = d.Item1,
            ClinicId = clinicId,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _unitOfWork.Repository<Department>().AddRangeAsync(departmentEntities);
        await _unitOfWork.SaveChangesAsync();
    }
}
