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
    private readonly IClinicTemplateAssignmentService _assignmentService;

    public ClinicService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<AppUser> userManager, IClinicTemplateAssignmentService assignmentService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
        _assignmentService = assignmentService;
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
        var policyCount = await _unitOfWork.Repository<ClinicTemplateAssignment>().CountAsync(x => x.ClinicId == clinicId);
        var openGapCount = await _unitOfWork.Repository<ClinicTemplateAssignment>().CountAsync(x =>
            x.ClinicId == clinicId &&
            (x.AssignmentStatus == ClinicDocumentStatus.MissingAttachment ||
             x.AssignmentStatus == ClinicDocumentStatus.Expired ||
             x.AssignmentStatus == ClinicDocumentStatus.NeedsReview)
        );

        var dto = _mapper.Map<ClinicDetailDto>(clinic);
        dto.UserCount = userCount;
        dto.DepartmentCount = departmentCount;
        dto.PolicyDocumentCount = policyCount;
        dto.OpenGapCount = openGapCount;

        var assignments = await _assignmentService.GetClinicAssignmentsWithDetailsAsync(clinicId);
        var globalValues = await _assignmentService.GetGlobalTemplateValuesForClinicAsync(clinicId);

        foreach (var gv in globalValues)
        {
            if (gv.VariableName.StartsWith("ClinicName", StringComparison.OrdinalIgnoreCase) ||
                gv.VariableName.Equals("Clinic Name", StringComparison.OrdinalIgnoreCase) ||
                gv.VariableName.Equals("Clinic_Name", StringComparison.OrdinalIgnoreCase))
            {
                gv.Value = clinic.Name;
            }

            if (gv.VariableName.Contains("logo", StringComparison.OrdinalIgnoreCase) ||
                gv.VariableName.Contains("Logo", StringComparison.OrdinalIgnoreCase))
            {
                gv.ImagePath = clinic.LogoPath;
            }
        }

        var globalNames = globalValues.Select(g => g.VariableName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var assignment in assignments)
        {
            assignment.VariableValues = assignment.VariableValues
                .Where(v => !globalNames.Contains(v.VariableName))
                .ToList();
        }

        dto.DocumentAssignments = assignments;
        dto.GlobalTemplateValues = globalValues;

        return dto;
    }

    public async Task<int> CreateClinicAsync(CreateClinicDto dto)
    {
        var clinic = _mapper.Map<Clinic>(dto);
        clinic.IsActive = true;
        clinic.ComplianceScore = 0;

        await _unitOfWork.Repository<Clinic>().AddAsync(clinic);
        await _unitOfWork.SaveChangesAsync();

        if (clinic.ClinicType == ClinicType.AMB)
        {
            await CreateDefaultDepartmentsAsync(clinic.Id, dto.SelectedStandards);
        }
        else if (clinic.ClinicType == ClinicType.Dental)
        {
            await CreateDentalDepartmentsAsync(clinic.Id);
        }

        return clinic.Id;
    }

    public async Task<bool> UpdateClinicAsync(UpdateClinicDto dto)
    {
        var clinic = (await _unitOfWork.Repository<Clinic>().FindWithIncludesAsync(c => c.Id == dto.Id, includes: c => c.Departments)).FirstOrDefault();
        if (clinic == null)
            return false;

        clinic.Name = dto.Name;
        clinic.NameAr = dto.NameAr;
        clinic.CityEn = dto.CityEn;
        clinic.CityAr = dto.CityAr;
        clinic.ClinicType = dto.ClinicType;
        clinic.LicenseNumber = dto.LicenseNumber;
        clinic.LicenseExpiry = dto.LicenseExpiry;
        clinic.IsActive = dto.IsActive;

        List<string> removedStandards = new List<string>();

        if (clinic.SelectedStandards.Count == 0 && dto.SelectedStandards.Count > 0)
        {
            removedStandards = dto.SelectedStandards;
        }
        else
        {
            removedStandards = clinic.SelectedStandards.Except(dto.SelectedStandards).ToList();
        }

        if (removedStandards.Count > 0)
        {
            var departmentsToRemove = clinic.Departments.Where(d => removedStandards.Contains(d.Code, StringComparer.OrdinalIgnoreCase)).ToList();
            foreach (var department in departmentsToRemove)
            {
                _unitOfWork.Repository<Department>().SoftDelete(department);
            }
        }

        var addedStandards = dto.SelectedStandards.Except(clinic.SelectedStandards).ToList();

        if (addedStandards.Count > 0)
        {
            await CreateDefaultDepartmentsAsync(clinic.Id, addedStandards);
        }

        if (clinic == null)
            return false;

        _mapper.Map(dto, clinic);
        clinic.UpdatedAt = DateTime.UtcNow;
        clinic.SelectedStandards = dto.SelectedStandards;

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

    private async Task CreateDefaultDepartmentsAsync(int clinicId, List<string> SelectadStandards)
    {
        var departments = new[]
        {
            ("LD", "Leadership of the Organization", "قيادة المنظمة"),
            ("PC", "Provision of Care", "تقديم الرعاية"),
            ("LB", "Laboratory", "المختبر"),
            ("RD", "Radiology Department", "قسم الأشعات"),
            ("DN", "Dental", "الأسنان"),
            ("MM", "Medication Management", "إدارة الأدوية"),
            ("MOI", "Management of Information", "إدارة المعلومات"),
            ("IPC", "Infection Prevention and Control", "الوقاية من العدوى والتحكم بها"),
            ("FMS", "Facility Management and Safety", "إدارة المرافق والسلامة"),
            ("DPU", "Dialysis Patient Unit", "وحدة مرضى غسيل الكلى"),
            ("DA", "Dental Anesthesia", "تخدير الأسنان")
        };

        var departmentEntities = departments.Where(d => SelectadStandards.Contains(d.Item1, StringComparer.OrdinalIgnoreCase)).Select(d => new Department
        {
            NameEn = d.Item2,
            NameAr = d.Item3,
            Code = d.Item1,
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
            ("LD", "Leadership of the Organization", "قيادة المنظمة"),
            ("PC", "Provision of Care", "تقديم الرعاية"),
            ("DL", "Dental Laboratory", "معمل الأسنان"),
            ("MOI", "Management of Information", "إدارة المعلومات"),
            ("IPC", "Infection Prevention and Control", "الوقاية من العدوى والتحكم بها"),
            ("FMS", "Facility Management and Safety", "إدارة المرافق والسلامة")
        };

        var departmentEntities = departments.Select(d => new Department
        {
            NameEn = d.Item2,
            NameAr = d.Item3,
            Code = d.Item1,
            ClinicId = clinicId,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _unitOfWork.Repository<Department>().AddRangeAsync(departmentEntities);
        await _unitOfWork.SaveChangesAsync();
    }
}
