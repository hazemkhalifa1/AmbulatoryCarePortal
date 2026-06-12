using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Roles = "ClinicAdmin,ClinicViewer")]
public class DepartmentManagementController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DepartmentManagementController> _logger;
    private readonly ITranslationService _localizer;

    public DepartmentManagementController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<DepartmentManagementController> logger,
        ITranslationService localizer)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string searchTerm = "")
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var departments = await _unitOfWork.Repository<Department>().FindAsync(
            d => d.ClinicId == clinicId &&
                 (string.IsNullOrEmpty(searchTerm) || d.NameEn.Contains(searchTerm) || (d.NameAr != null && d.NameAr.Contains(searchTerm)))
        );

        var paged = departments
            .OrderBy(d => d.NameEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<DepartmentListViewModel>();
        foreach (var dept in paged)
        {
            var staffCount = await _unitOfWork.Repository<HrStaff>().CountAsync(s => s.DepartmentId == dept.Id && !s.IsDeleted);
            var policyCount = await _unitOfWork.Repository<PolicyDocument>().CountAsync(p => p.DepartmentId == dept.Id && !p.IsDeleted);
            var kpiCount = await _unitOfWork.Repository<KPI>().CountAsync(k => k.DepartmentId == dept.Id && !k.IsDeleted);

            dtos.Add(new DepartmentListViewModel
            {
                Id = dept.Id,
                NameEn = dept.NameEn,
                NameAr = dept.NameAr,
                Code = dept.Code,
                StaffCount = staffCount,
                PolicyCount = policyCount,
                KPICount = kpiCount
            });
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.TotalCount = departments.Count();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(departments.Count() / (double)pageSize);

        return View(dtos);
    }

    [HttpGet]
    [Authorize(Roles = "ClinicAdmin")]
    public IActionResult Create()
    {
        return View(new CreateDepartmentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ClinicAdmin")]
    public async Task<IActionResult> Create(CreateDepartmentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (clinicId == 0)
        {
            ModelState.AddModelError("", _localizer.T("Alert.Error.NoClinicAccess"));
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var department = new Department
        {
            NameEn = model.NameEn,
            NameAr = model.NameAr,
            Code = model.Code,
            ClinicId = clinicId,
            CreatedBy = userId
        };

        await _unitOfWork.Repository<Department>().AddAsync(department);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = department.Id,
            TargetObjectType = nameof(Department),
            Description = $"Created department: {department.NameEn}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Department {Name} created by {UserId}", department.NameEn, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.DepartmentCreated");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "ClinicAdmin")]
    public async Task<IActionResult> Edit(int id)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
        if (department == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (department.ClinicId != clinicId)
            return Forbid();

        var model = new EditDepartmentViewModel
        {
            Id = department.Id,
            NameEn = department.NameEn,
            NameAr = department.NameAr,
            Code = department.Code
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "ClinicAdmin")]
    public async Task<IActionResult> Edit(int id, EditDepartmentViewModel model)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
        if (department == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (department.ClinicId != clinicId)
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        department.NameEn = model.NameEn;
        department.NameAr = model.NameAr;
        department.Code = model.Code;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = userId;

        _unitOfWork.Repository<Department>().Update(department);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Update,
            TargetObjectId = department.Id,
            TargetObjectType = nameof(Department),
            Description = $"Updated department: {department.NameEn}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Department {Name} updated by {UserId}", department.NameEn, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.DepartmentUpdated");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
        if (department == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (department.ClinicId != clinicId)
            return Forbid();

        var staffCount = await _unitOfWork.Repository<HrStaff>().CountAsync(s => s.DepartmentId == id && !s.IsDeleted);
        var policyCount = await _unitOfWork.Repository<PolicyDocument>().CountAsync(p => p.DepartmentId == id && !p.IsDeleted);
        var kpiCount = await _unitOfWork.Repository<KPI>().CountAsync(k => k.DepartmentId == id && !k.IsDeleted);
        var checklistCount = await _unitOfWork.Repository<ChecklistTemplate>().CountAsync(c => c.DepartmentId == id && !c.IsDeleted);

        var model = new DepartmentDetailViewModel
        {
            Id = department.Id,
            NameEn = department.NameEn,
            NameAr = department.NameAr,
            Code = department.Code,
            StaffCount = staffCount,
            PolicyCount = policyCount,
            KPICount = kpiCount,
            ChecklistCount = checklistCount
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "ClinicAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id);
        if (department == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (department.ClinicId != clinicId)
            return Forbid();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var staffCount = await _unitOfWork.Repository<HrStaff>().CountAsync(s => s.DepartmentId == id && !s.IsDeleted);
        var policyCount = await _unitOfWork.Repository<PolicyDocument>().CountAsync(p => p.DepartmentId == id && !p.IsDeleted);
        var kpiCount = await _unitOfWork.Repository<KPI>().CountAsync(k => k.DepartmentId == id && !k.IsDeleted);
        var checklistCount = await _unitOfWork.Repository<ChecklistTemplate>().CountAsync(c => c.DepartmentId == id && !c.IsDeleted);

        if (staffCount > 0 || policyCount > 0 || kpiCount > 0 || checklistCount > 0)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DepartmentHasLinks", staffCount, policyCount, kpiCount, checklistCount);
            return RedirectToAction(nameof(Details), new { id });
        }

        _unitOfWork.Repository<Department>().SoftDelete(department);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Delete,
            TargetObjectId = department.Id,
            TargetObjectType = nameof(Department),
            Description = $"Deleted department: {department.NameEn}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Department {Name} deleted by {UserId}", department.NameEn, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.DepartmentDeleted");

        return RedirectToAction(nameof(Index));
    }
}

public class DepartmentListViewModel
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Code { get; set; } = string.Empty;
    public int StaffCount { get; set; }
    public int PolicyCount { get; set; }
    public int KPICount { get; set; }
}

public class CreateDepartmentViewModel
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class EditDepartmentViewModel
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class DepartmentDetailViewModel
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Code { get; set; } = string.Empty;
    public int StaffCount { get; set; }
    public int PolicyCount { get; set; }
    public int KPICount { get; set; }
    public int ChecklistCount { get; set; }
}
