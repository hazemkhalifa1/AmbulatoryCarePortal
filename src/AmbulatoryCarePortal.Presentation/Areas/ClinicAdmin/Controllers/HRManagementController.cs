using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;
using HRStaffDto = AmbulatoryCarePortal.Application.DTOs.HrStaffDto;
using HRDocumentDto = AmbulatoryCarePortal.Application.DTOs.HrDocumentDto;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.staff.view")]
public class HRManagementController : Controller
{
    private readonly IHrService _hrService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<HRManagementController> _logger;
    private readonly ITranslationService _localizer;

    public HRManagementController(
        IHrService hrService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<HRManagementController> logger,
        ITranslationService localizer)
    {
        _hrService = hrService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string searchTerm = "",
        string complianceFilter = "",
        int? departmentFilter = null)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var staff = await _unitOfWork.Repository<HrStaff>().FindWithIncludesAsync(
            s => s.ClinicId == clinicId &&
                 (string.IsNullOrEmpty(searchTerm) || s.FirstName.Contains(searchTerm) || s.LastName.Contains(searchTerm)) &&
                 (!departmentFilter.HasValue || s.DepartmentId == departmentFilter.Value),
            includeDeleted: false,
            x => x.Department
        );

        var pagedStaff = staff
            .OrderBy(s => s.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var staffDtos = new List<HRStaffDetailViewModel>();

        foreach (var person in pagedStaff)
        {
            var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
                d => d.HrStaffId == person.Id && !d.IsDeleted
            );

            var expiringDocuments = documents.Count(d =>
                d.ExpiryDate.HasValue &&
                d.ExpiryDate <= DateTime.UtcNow.AddMonths(1) &&
                d.ExpiryDate > DateTime.UtcNow);

            var expiredDocuments = documents.Count(d =>
                d.ExpiryDate.HasValue &&
                d.ExpiryDate <= DateTime.UtcNow);

            var allDocumentsValid = documents.All(d =>
                !d.ExpiryDate.HasValue || d.ExpiryDate > DateTime.UtcNow);

            staffDtos.Add(new HRStaffDetailViewModel
            {
                Staff = _mapper.Map<HRStaffDto>(person),
                TotalDocuments = documents.Count(),
                ValidDocuments = documents.Count(d => !d.ExpiryDate.HasValue || d.ExpiryDate > DateTime.UtcNow),
                ExpiringDocuments = expiringDocuments,
                ExpiredDocuments = expiredDocuments,
                ComplianceStatus = allDocumentsValid ? "Compliant" : "Non-Compliant",
                LastDocumentUpdate = documents.OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt).FirstOrDefault()?.CreatedAt ?? DateTime.MinValue
            });
        }

        if (!string.IsNullOrEmpty(complianceFilter))
        {
            staffDtos = staffDtos.Where(s => s.ComplianceStatus == complianceFilter).ToList();
        }

        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.ComplianceFilter = complianceFilter;
        ViewBag.DepartmentFilter = departmentFilter;
        ViewBag.Departments = departments;
        ViewBag.TotalCount = staffDtos.Count;
        ViewBag.CurrentPage = page;

        return View(staffDtos);
    }

    [HttpGet]
    [Authorize(Policy = "Permission.staff.manage")]
    public async Task<IActionResult> Create()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        var model = new CreateHRStaffViewModel
        {
            AvailableDepartments = departments.Select(d => new DepartmentViewModel { Id = d.Id, Name = d.NameEn, ClinicId = d.ClinicId }).ToList(),
            StaffTypes = Enum.GetValues(typeof(StaffType))
                .Cast<StaffType>()
                .Select(s => s.ToString())
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.staff.manage")]
    public async Task<IActionResult> Create(CreateHRStaffViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
            var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);
            model.AvailableDepartments = departments.Select(d => new DepartmentViewModel { Id = d.Id, Name = d.NameEn, ClinicId = d.ClinicId }).ToList();
            return View(model);
        }

        var clinicIdValue = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var staff = new HrStaff
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            FullNameEn = $"{model.FirstName} {model.LastName}".Trim(),
            NationalId = model.NationalId,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            PositionTitle = model.PositionTitle,
            StaffType = Enum.Parse<StaffType>(model.StaffType),
            DepartmentId = model.DepartmentId,
            ClinicId = clinicIdValue,
            IsActive = true,
            JoinDate = model.JoinDate,
            CreatedBy = userId
        };

        await _unitOfWork.Repository<HrStaff>().AddAsync(staff);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = staff.Id,
            TargetObjectType = nameof(HrStaff),
            Description = $"Created staff record: {staff.FirstName} {staff.LastName}",
            ClinicId = clinicIdValue,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"Staff record created for {staff.FirstName} {staff.LastName} by {userId}");
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.StaffCreated");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = "Permission.staff.manage")]
    public async Task<IActionResult> Edit(int id)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(id);
        if (staff == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (staff.ClinicId != clinicId)
            return Forbid();

        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        var model = new CreateHRStaffViewModel
        {
            FirstName = staff.FirstName ?? "",
            LastName = staff.LastName ?? "",
            NationalId = staff.NationalId ?? "",
            Email = staff.Email ?? "",
            PhoneNumber = staff.PhoneNumber ?? "",
            PositionTitle = staff.PositionTitle ?? "",
            StaffType = staff.StaffType.ToString(),
            DepartmentId = staff.DepartmentId ?? 0,
            JoinDate = staff.JoinDate ?? DateTime.Now,
            AvailableDepartments = departments.Select(d => new DepartmentViewModel { Id = d.Id, Name = d.NameEn, ClinicId = d.ClinicId }).ToList(),
            StaffTypes = Enum.GetValues(typeof(StaffType))
                .Cast<StaffType>()
                .Select(s => s.ToString())
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.staff.manage")]
    public async Task<IActionResult> Edit(int id, CreateHRStaffViewModel model)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(id);
        if (staff == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (staff.ClinicId != clinicId)
            return Forbid();

        if (!ModelState.IsValid)
        {
            var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);
            model.AvailableDepartments = departments.Select(d => new DepartmentViewModel { Id = d.Id, Name = d.NameEn, ClinicId = d.ClinicId }).ToList();
            model.StaffTypes = Enum.GetValues(typeof(StaffType)).Cast<StaffType>().Select(s => s.ToString()).ToList();
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        staff.FirstName = model.FirstName;
        staff.LastName = model.LastName;
        staff.FullNameEn = $"{model.FirstName} {model.LastName}";
        staff.NationalId = model.NationalId;
        staff.Email = model.Email;
        staff.PhoneNumber = model.PhoneNumber;
        staff.PositionTitle = model.PositionTitle;
        staff.StaffType = Enum.Parse<StaffType>(model.StaffType);
        staff.DepartmentId = model.DepartmentId;
        staff.JoinDate = model.JoinDate;
        staff.UpdatedAt = DateTime.UtcNow;
        staff.UpdatedBy = userId;

        _unitOfWork.Repository<HrStaff>().Update(staff);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Update,
            TargetObjectId = staff.Id,
            TargetObjectType = nameof(HrStaff),
            Description = $"Updated staff record: {staff.FirstName} {staff.LastName}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"Staff record updated for {staff.FirstName} {staff.LastName} by {userId}");
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.StaffUpdated");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var staff = (await _unitOfWork.Repository<HrStaff>().FindWithIncludesAsync(
            s => s.Id == id,
            includeDeleted: false,
            x => x.Department
        )).FirstOrDefault();
        if (staff == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (staff.ClinicId != clinicId)
            return Forbid();

        var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
            d => d.HrStaffId == id && !d.IsDeleted
        );

        var model = new HRStaffDetailsViewModel
        {
            Staff = _mapper.Map<HRStaffDto>(staff),
            Documents = _mapper.Map<List<HRDocumentDto>>(documents.OrderByDescending(d => d.CreatedAt).ToList()),
            DocumentCount = documents.Count(),
            ExpiringCount = documents.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate <= DateTime.UtcNow.AddMonths(1) && d.ExpiryDate > DateTime.UtcNow),
            ExpiredCount = documents.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate <= DateTime.UtcNow),
            ComplianceStatus = documents.All(d => !d.ExpiryDate.HasValue || d.ExpiryDate > DateTime.UtcNow) ? "Compliant" : "Non-Compliant"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.documents.upload")]
    public async Task<IActionResult> UploadDocument(int staffId, CreateHRDocumentViewModel model, IFormFile documentFile)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(staffId);
        if (staff == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (staff.ClinicId != clinicId)
            return Forbid();

        if (documentFile == null || documentFile.Length == 0)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.NoFileSelected");
            return RedirectToAction(nameof(Details), new { id = staffId });
        }

        var (isValid, errorMsg) = FileUploadValidator.ValidateDocument(documentFile);
        if (!isValid)
        {
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Details), new { id = staffId });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var fileName = Path.GetRandomFileName() + Path.GetExtension(documentFile.FileName);
        var filePath = Path.Combine("wwwroot/uploads/hr-documents", fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await documentFile.CopyToAsync(stream);
        }

        var document = new HrDocument
        {
            HrStaffId = staffId,
            DocumentName = model.DocumentName,
            DocumentNameAr = model.DocumentNameAr,
            DocumentType = Enum.Parse<HrDocumentType>(model.DocumentType),
            FilePath = $"/uploads/hr-documents/{fileName}",
            ExpiryDate = model.ExpiryDate,
            IssueDate = model.IssueDate,
            IsVerified = false,
            VersionNumber = 1,
            CreatedBy = userId
        };

        await _unitOfWork.Repository<HrDocument>().AddAsync(document);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = document.Id,
            TargetObjectType = nameof(HrDocument),
            Description = $"Uploaded document for {staff.FirstName} {staff.LastName}: {document.DocumentName}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"Document uploaded for staff {staffId} by {userId}");
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.DocumentUploaded");

        return RedirectToAction(nameof(Details), new { id = staffId });
    }

    [HttpPost]
    [Authorize(Policy = "Permission.documents.verify")]
    public async Task<IActionResult> VerifyDocument(int documentId)
    {
        var document = await _unitOfWork.Repository<HrDocument>().GetByIdAsync(documentId);
        if (document == null)
            return NotFound();

        var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(document.HrStaffId);
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (staff?.ClinicId != clinicId)
            return Forbid();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        document.IsVerified = true;
        document.VerifiedByUserId = userId;
        document.VerifiedDate = DateTime.UtcNow;
        document.UpdatedBy = userId;

        _unitOfWork.Repository<HrDocument>().Update(document);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Approve,
            TargetObjectId = document.Id,
            TargetObjectType = nameof(HrDocument),
            Description = $"Verified document: {document.DocumentName}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"Document {documentId} verified by {userId}");
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.DocumentVerified");

        return RedirectToAction(nameof(Details), new { id = document.HrStaffId });
    }

    [HttpGet]
    public async Task<IActionResult> ExpiringDocuments()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var staff = await _unitOfWork.Repository<HrStaff>().FindWithIncludesAsync(
            s => s.ClinicId == clinicId,
            includeDeleted: false,
            x => x.Department
        );
        var staffIds = staff.Select(s => s.Id).ToList();

        var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
            d => staffIds.Contains(d.HrStaffId) &&
                 d.ExpiryDate.HasValue &&
                 d.ExpiryDate <= DateTime.UtcNow.AddDays(90) &&
                 d.ExpiryDate > DateTime.UtcNow &&
                 !d.IsDeleted
        );

        var expiringList = new List<ExpiringDocumentViewModel>();

        foreach (var doc in documents.OrderBy(d => d.ExpiryDate))
        {
            var person = staff.FirstOrDefault(s => s.Id == doc.HrStaffId);
            var daysUntilExpiry = (doc.ExpiryDate!.Value - DateTime.UtcNow).Days;

            expiringList.Add(new ExpiringDocumentViewModel
            {
                Document = _mapper.Map<HRDocumentDto>(doc),
                StaffName = $"{person?.FirstName} {person?.LastName}",
                DepartmentName = person?.Department?.NameEn,
                DaysUntilExpiry = daysUntilExpiry,
                Category = daysUntilExpiry <= 7 ? "Critical" : daysUntilExpiry <= 30 ? "Urgent" : "Upcoming"
            });
        }

        return View(expiringList);
    }

    [HttpGet]
    public async Task<IActionResult> NonCompliantStaff()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var staff = await _unitOfWork.Repository<HrStaff>().FindWithIncludesAsync(
            s => s.ClinicId == clinicId,
            includeDeleted: false,
            x => x.Department
        );

        var nonCompliantList = new List<HRStaffNonCompliantViewModel>();

        foreach (var person in staff)
        {
            var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
                d => d.HrStaffId == person.Id && !d.IsDeleted
            );

            var expiredDocs = documents.Where(d => d.ExpiryDate.HasValue && d.ExpiryDate <= DateTime.UtcNow).ToList();

            if (expiredDocs.Any())
            {
                nonCompliantList.Add(new HRStaffNonCompliantViewModel
                {
                    Staff = _mapper.Map<HRStaffDto>(person),
                    ExpiredDocuments = _mapper.Map<List<HRDocumentDto>>(expiredDocs),
                    ExpiredDocumentCount = expiredDocs.Count,
                    DaysOverdue = expiredDocs.Max(d => (DateTime.UtcNow - d.ExpiryDate!.Value).Days)
                });
            }
        }

        return View(nonCompliantList.OrderByDescending(x => x.DaysOverdue).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Export(string format = "excel")
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(s => s.ClinicId == clinicId);

        if (format.ToLower() == "pdf")
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"HR Report: {staff.Count()} staff members");
            return File(bytes, "application/pdf", "hr-report.pdf");
        }
        else
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"HR Report: {staff.Count()} staff members");
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "hr-report.xlsx");
        }
    }

    [HttpGet]
    [Route("api/hr/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(s => s.ClinicId == clinicId);
        var staffIds = staff.Select(s => s.Id).ToList();

        var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
            d => staffIds.Contains(d.HrStaffId) && !d.IsDeleted
        );

        var summary = new
        {
            TotalStaff = staff.Count(),
            ActiveStaff = staff.Count(s => s.IsActive),
            InactiveStaff = staff.Count(s => !s.IsActive),
            TotalDocuments = documents.Count(),
            VerifiedDocuments = documents.Count(d => d.IsVerified),
            ExpiringDocuments = documents.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate <= DateTime.UtcNow.AddMonths(1) && d.ExpiryDate > DateTime.UtcNow),
            ExpiredDocuments = documents.Count(d => d.ExpiryDate.HasValue && d.ExpiryDate <= DateTime.UtcNow),
            CompliantStaff = staff.Count(s =>
                documents.Where(d => d.HrStaffId == s.Id).All(d => !d.ExpiryDate.HasValue || d.ExpiryDate > DateTime.UtcNow)
            )
        };

        return Json(summary);
    }
}

public class CreateHRStaffViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public string StaffType { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public DateTime JoinDate { get; set; }
    public List<DepartmentViewModel> AvailableDepartments { get; set; } = new();
    public List<string> StaffTypes { get; set; } = new();
}

public class CreateHRDocumentViewModel
{
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentNameAr { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class HRStaffDetailViewModel
{
    public HRStaffDto Staff { get; set; } = null!;
    public int TotalDocuments { get; set; }
    public int ValidDocuments { get; set; }
    public int ExpiringDocuments { get; set; }
    public int ExpiredDocuments { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
    public DateTime LastDocumentUpdate { get; set; }
}

public class HRStaffDetailsViewModel
{
    public HRStaffDto Staff { get; set; } = null!;
    public List<HRDocumentDto> Documents { get; set; } = new();
    public int DocumentCount { get; set; }
    public int ExpiringCount { get; set; }
    public int ExpiredCount { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
}

public class ExpiringDocumentViewModel
{
    public HRDocumentDto Document { get; set; } = null!;
    public string StaffName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class HRStaffNonCompliantViewModel
{
    public HRStaffDto Staff { get; set; } = null!;
    public List<HRDocumentDto> ExpiredDocuments { get; set; } = new();
    public int ExpiredDocumentCount { get; set; }
    public int DaysOverdue { get; set; }
}
