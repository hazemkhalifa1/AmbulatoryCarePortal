using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs.PolicyDocument;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.policies.read")]
public class PolicyManagementController : Controller
{
    private readonly IPolicyDocumentService _policyService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PolicyManagementController> _logger;
    private readonly ITranslationService _localizer;

    public PolicyManagementController(
        IPolicyDocumentService policyService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PolicyManagementController> logger,
        ITranslationService localizer)
    {
        _policyService = policyService;
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
        string statusFilter = "",
        int? departmentFilter = null)
    {
        var clinicId = User.FindFirst("ClinicId") != null
            ? int.Parse(User.FindFirst("ClinicId")?.Value ?? "0")
            : 0;

        var policies = await _unitOfWork.Repository<PolicyDocument>().FindAsync(
            p => p.ClinicId == clinicId &&
                 (string.IsNullOrEmpty(searchTerm) || p.Title.Contains(searchTerm)) &&
                 (string.IsNullOrEmpty(statusFilter) || p.DocumentStatus.ToString() == statusFilter) &&
                 (!departmentFilter.HasValue || p.DepartmentId == departmentFilter.Value)
        );

        var pagedPolicies = policies
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var policyDtos = _mapper.Map<List<PolicyDocumentDto>>(pagedPolicies);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.DepartmentFilter = departmentFilter;
        ViewBag.TotalCount = policies.Count();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(policies.Count() / (double)pageSize);

        return View(policyDtos);
    }

    [HttpGet]
    [Authorize(Policy = "Permission.policies.create")]
    public async Task<IActionResult> Create()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        var model = new CreatePolicyDocumentViewModel
        {
            AvailableDepartments = _mapper.Map<List<DepartmentViewModel>>(departments),
            CreatedDate = DateTime.Now
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.policies.create")]
    public async Task<IActionResult> Create(CreatePolicyDocumentViewModel model, IFormFile policyFile)
    {
        if (!ModelState.IsValid)
        {
            var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
            var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);
            model.AvailableDepartments = _mapper.Map<List<DepartmentViewModel>>(departments);
            return View(model);
        }

        var clinicIdValue = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var policy = new PolicyDocument
        {
            Title = model.Title,
            TitleAr = model.TitleAr,
            StandardCode = model.StandardCode,
            DepartmentId = model.DepartmentId,
            ClinicId = clinicIdValue,
            CreatedBy = userId,
            ExpiryDate = model.ExpiryDate,
            VersionNumber = 1,
            DocumentStatus = DocumentStatus.Draft
        };

        if (policyFile != null && policyFile.Length > 0)
        {
            var fileName = Path.GetRandomFileName() + Path.GetExtension(policyFile.FileName);
            var filePath = Path.Combine("wwwroot/uploads/policies", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await policyFile.CopyToAsync(stream);
            }

            policy.OfficialPdfPath = $"/uploads/policies/{fileName}";
        }

        await _unitOfWork.Repository<PolicyDocument>().AddAsync(policy);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = policy.Id,
            TargetObjectType = nameof(PolicyDocument),
            Description = $"Created policy: {policy.Title}",
            ClinicId = clinicIdValue,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Policy {Title} created by {UserId}", policy.Title, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.PolicyCreated");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = "Permission.policies.update")]
    public async Task<IActionResult> Edit(int id)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(id);
        if (policy == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (policy.ClinicId != clinicId)
            return Forbid();

        var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);

        var model = new UpdatePolicyDocumentViewModel
        {
            Id = policy.Id,
            Title = policy.Title,
            TitleAr = policy.TitleAr,
            StandardCode = policy.StandardCode,
            DepartmentId = policy.DepartmentId,
            ExpiryDate = policy.ExpiryDate,
            DocumentStatus = policy.DocumentStatus.ToString(),
            VersionNumber = policy.VersionNumber,
            AvailableDepartments = _mapper.Map<List<DepartmentViewModel>>(departments)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.policies.update")]
    public async Task<IActionResult> Edit(int id, UpdatePolicyDocumentViewModel model)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(id);
        if (policy == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (policy.ClinicId != clinicId)
            return Forbid();

        if (!ModelState.IsValid)
        {
            var departments = await _unitOfWork.Repository<Department>().FindAsync(d => d.ClinicId == clinicId);
            model.AvailableDepartments = _mapper.Map<List<DepartmentViewModel>>(departments);
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        policy.Title = model.Title;
        policy.TitleAr = model.TitleAr;
        policy.StandardCode = model.StandardCode;
        policy.DepartmentId = model.DepartmentId;
        policy.ExpiryDate = model.ExpiryDate;
        policy.UpdatedAt = DateTime.UtcNow;
        policy.UpdatedBy = userId;

        if (Enum.TryParse<DocumentStatus>(model.DocumentStatus, out var status))
            policy.DocumentStatus = status;

        _unitOfWork.Repository<PolicyDocument>().Update(policy);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Update,
            TargetObjectId = policy.Id,
            TargetObjectType = nameof(PolicyDocument),
            Description = $"Updated policy: {policy.Title}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Policy {Title} updated by {UserId}", policy.Title, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.PolicyUpdated");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(id);
        if (policy == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (policy.ClinicId != clinicId)
            return Forbid();

        var evidence = await _unitOfWork.Repository<EvidenceAttachment>().FindAsync(
            e => e.PolicyDocumentId == id
        );

        var policyDto = _mapper.Map<PolicyDocumentDto>(policy);
        var evidenceDtos = _mapper.Map<List<EvidenceAttachmentDto>>(evidence);

        var model = new PolicyDocumentDetailViewModel
        {
            Policy = policyDto,
            Evidence = evidenceDtos,
            EvidenceCount = evidenceDtos.Count,
            Status = policy.DocumentStatus.ToString()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.policies.evidence.upload")]
    public async Task<IActionResult> UploadEvidence(int policyId, IFormFile evidenceFile, string? notes)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(policyId);
        if (policy == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (policy.ClinicId != clinicId)
            return Forbid();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (evidenceFile == null || evidenceFile.Length == 0)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.NoFileSelected");
            return RedirectToAction(nameof(Details), new { id = policyId });
        }

        var fileName = Path.GetRandomFileName() + Path.GetExtension(evidenceFile.FileName);
        var filePath = Path.Combine("wwwroot/uploads/evidence", fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await evidenceFile.CopyToAsync(stream);
        }

        var evidence = new EvidenceAttachment
        {
            PolicyDocumentId = policyId,
            DocumentName = evidenceFile.FileName,
            FilePath = $"/uploads/evidence/{fileName}",
            FileType = Path.GetExtension(evidenceFile.FileName),
            UploadDate = DateTime.UtcNow,
            Notes = notes,
            UploadedByUserId = userId,
            CreatedBy = userId
        };

        await _unitOfWork.Repository<EvidenceAttachment>().AddAsync(evidence);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Upload,
            TargetObjectId = evidence.Id,
            TargetObjectType = nameof(EvidenceAttachment),
            Description = $"Uploaded evidence for policy: {policy.Title}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Evidence uploaded for policy {PolicyId} by {UserId}", policyId, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.EvidenceUploaded");

        return RedirectToAction(nameof(Details), new { id = policyId });
    }

    [HttpPost]
    [Authorize(Policy = "Permission.policies.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(id);
        if (policy == null)
            return NotFound();

        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        if (policy.ClinicId != clinicId)
            return Forbid();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        _unitOfWork.Repository<PolicyDocument>().SoftDelete(policy);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Delete,
            TargetObjectId = policy.Id,
            TargetObjectType = nameof(PolicyDocument),
            Description = $"Deleted policy: {policy.Title}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Policy {Title} deleted by {UserId}", policy.Title, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.PolicyDeleted");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "Permission.policies.approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var policy = await _unitOfWork.Repository<PolicyDocument>().GetByIdAsync(id);
        if (policy == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var clinicId = policy.ClinicId;

        policy.DocumentStatus = DocumentStatus.Approved;
        policy.UpdatedBy = userId;
        policy.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<PolicyDocument>().Update(policy);
        await _unitOfWork.SaveChangesAsync();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Approve,
            TargetObjectId = policy.Id,
            TargetObjectType = nameof(PolicyDocument),
            Description = $"Approved policy: {policy.Title}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Policy {Title} approved by {UserId}", policy.Title, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.PolicyApproved");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string format = "excel")
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var policies = await _unitOfWork.Repository<PolicyDocument>().FindAsync(p => p.ClinicId == clinicId);

        if (format.ToLower() == "pdf")
        {
            var pdfContent = "PDF Export: " + string.Join(", ", policies.Select(p => p.Title));
            var bytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);
            return File(bytes, "application/pdf", "policies-export.pdf");
        }
        else
        {
            var excelContent = "Excel Export: " + string.Join(", ", policies.Select(p => p.Title));
            var bytes = System.Text.Encoding.UTF8.GetBytes(excelContent);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "policies-export.xlsx");
        }
    }

    [HttpGet]
    [Route("api/policies/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var policies = await _unitOfWork.Repository<PolicyDocument>().FindAsync(p => p.ClinicId == clinicId);

        var summary = new
        {
            TotalPolicies = policies.Count(),
            ApprovedPolicies = policies.Count(p => p.DocumentStatus == DocumentStatus.Approved),
            PendingPolicies = policies.Count(p => p.DocumentStatus == DocumentStatus.Pending),
            DraftPolicies = policies.Count(p => p.DocumentStatus == DocumentStatus.Draft),
            ExpiringPolicies = policies.Count(p => p.ExpiryDate.HasValue && p.ExpiryDate <= DateTime.UtcNow.AddMonths(1)),
            CompletionRate = policies.Any()
                ? Math.Round((policies.Count(p => p.DocumentStatus == DocumentStatus.Approved) * 100m / policies.Count()), 2)
                : 0
        };

        return Json(summary);
    }
}

public class CreatePolicyDocumentViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? StandardCode { get; set; }
    public int DepartmentId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<DepartmentViewModel> AvailableDepartments { get; set; } = new();
}

public class UpdatePolicyDocumentViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? StandardCode { get; set; }
    public int DepartmentId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string DocumentStatus { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public List<DepartmentViewModel> AvailableDepartments { get; set; } = new();
}

public class PolicyDocumentDetailViewModel
{
    public PolicyDocumentDto Policy { get; set; } = null!;
    public List<EvidenceAttachmentDto> Evidence { get; set; } = new();
    public int EvidenceCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
