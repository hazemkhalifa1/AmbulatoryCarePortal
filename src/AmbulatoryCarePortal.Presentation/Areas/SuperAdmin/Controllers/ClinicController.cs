using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Policy = "Permission.system.configure")]
public class ClinicController : Controller
{
    private readonly IClinicService _clinicService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClinicController> _logger;
    private readonly ITranslationService _localizer;
    private readonly IClinicTemplateAssignmentService _assignmentService;
    private readonly IDocumentGenerationService _generationService;

    public ClinicController(
        IClinicService clinicService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<ClinicController> logger,
        ITranslationService localizer,
        IClinicTemplateAssignmentService assignmentService,
        IDocumentGenerationService generationService)
    {
        _clinicService = clinicService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _localizer = localizer;
        _assignmentService = assignmentService;
        _generationService = generationService;
    }

    public async Task<IActionResult> ClinicDetail(int id)
    {
        var clinic = await _clinicService.GetClinicDetailsAsync(id);
        if (clinic == null)
            return NotFound();

        ViewBag.PageTitle = clinic.Name;

        return View(clinic);
    }

    [HttpGet]
    public IActionResult CreateClinic()
    {
        ViewBag.PageTitle = _localizer.T("Page.CreateClinic");
        ViewBag.StandardNames = GetStandardNames();
        ViewBag.StandardIcons = GetStandardIcons();
        return View(new CreateClinicViewModel());
    }

    private static Dictionary<string, string> GetStandardNames() => new()
    {
        {"LD", "Leadership"},
        {"PC", "Provision of Care"},
        {"FMS", "Facility Management & Safety"},
        {"LB", "Laboratory"},
        {"RD", "Radiology"},
        {"DN", "Dental/Nursing"},
        {"MM", "Medication Management"},
        {"MOI", "Management of Information"},
        {"IPC", "Infection Prevention & Control"},
        {"DPU", "DPU"},
        {"DA", "DA"},
        {"DL", "Dental Lab"}
    };

    private static Dictionary<string, string> GetStandardIcons() => new()
    {
        {"LD", "fa-users-cog"},
        {"PC", "fa-stethoscope"},
        {"FMS", "fa-building"},
        {"LB", "fa-flask"},
        {"RD", "fa-x-ray"},
        {"DN", "fa-tooth"},
        {"MM", "fa-pills"},
        {"MOI", "fa-database"},
        {"IPC", "fa-shield-virus"},
        {"DPU", "fa-file-contract"},
        {"DA", "fa-chart-bar"},
        {"DL", "fa-teeth"}
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClinic(CreateClinicViewModel model, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = _localizer.T("Page.CreateClinic");
            ViewBag.StandardNames = GetStandardNames();
            ViewBag.StandardIcons = GetStandardIcons();
            return View(model);
        }

        try
        {
            var dto = new AmbulatoryCarePortal.Application.DTOs.Clinic.CreateClinicDto
            {
                Name = model.Name,
                NameAr = model.NameAr,
                CityEn = model.CityEn,
                CityAr = model.CityAr,
                ClinicType = model.ClinicType,
                LicenseNumber = model.LicenseNumber,
                LicenseExpiry = model.LicenseExpiry,
                SelectedStandards = model.SelectedStandards
            };

            var clinicId = await _clinicService.CreateClinicAsync(dto);

            if (logoFile != null)
            {
                var (isValid, errorMsg) = FileUploadValidator.ValidateImage(logoFile);
                if (!isValid)
                {
                    ModelState.AddModelError(string.Empty, errorMsg);
                    ViewBag.PageTitle = _localizer.T("Page.CreateClinic");
                    ViewBag.StandardNames = GetStandardNames();
                    ViewBag.StandardIcons = GetStandardIcons();
                    return View(model);
                }
            }

            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsDir = Path.Combine("wwwroot", "uploads", "clinic-logos");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(logoFile.FileName);
                var fileName = $"clinic_{clinicId}_{Path.GetRandomFileName()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId);
                if (clinic != null)
                {
                    clinic.LogoPath = $"/uploads/clinic-logos/{fileName}";
                }
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = clinicId,
                ActionType = AuditActionType.Create,
                TargetObjectId = clinicId,
                TargetObjectType = nameof(Clinic),
                Description = $"Created clinic: {model.Name}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);

            // Auto-assign templates for selected standards
            if (model.SelectedStandards?.Count > 0)
            {
                var templates = await _unitOfWork.Repository<DocumentTemplate>()
                    .FindAsync(t => model.SelectedStandards.Contains(t.DepartmentCategory)
                                 && t.ClinicType == model.ClinicType
                                 && t.IsActive && !t.IsDeleted);

                foreach (var template in templates)
                {
                    await _assignmentService.AssignTemplateToClinicAsync(template.Id, clinicId);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.ClinicCreated");

            return RedirectToAction("ClinicDetail", new { id = clinicId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating clinic");
            ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.ClinicCreateFailed"));
            ViewBag.PageTitle = _localizer.T("Page.CreateClinic");
            ViewBag.StandardNames = GetStandardNames();
            ViewBag.StandardIcons = GetStandardIcons();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var clinic = await _clinicService.GetClinicDetailsAsync(id);
        if (clinic == null)
            return NotFound();

        var model = new CreateClinicViewModel
        {
            Id = clinic.Id,
            Name = clinic.Name,
            NameAr = clinic.NameAr,
            CityEn = clinic.CityEn,
            CityAr = clinic.CityAr,
            ClinicType = clinic.ClinicType,
            LicenseNumber = clinic.LicenseNumber,
            LicenseExpiry = clinic.LicenseExpiry,
            IsActive = clinic.IsActive,
            ExistingLogoPath = clinic.LogoPath,
            SelectedStandards = clinic.SelectedStandards
        };

        ViewBag.PageTitle = _localizer.T("Page.EditClinic");
        ViewBag.StandardNames = GetStandardNames();
        ViewBag.StandardIcons = GetStandardIcons();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateClinicViewModel model, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = _localizer.T("Page.EditClinic");
            ViewBag.StandardNames = GetStandardNames();
            ViewBag.StandardIcons = GetStandardIcons();
            return View(model);
        }

        try
        {
            var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(id);
            if (clinic == null)
                return NotFound();

            clinic.Name = model.Name;
            clinic.NameAr = model.NameAr;
            clinic.CityEn = model.CityEn;
            clinic.CityAr = model.CityAr;
            clinic.ClinicType = model.ClinicType;
            clinic.LicenseNumber = model.LicenseNumber;
            clinic.LicenseExpiry = model.LicenseExpiry;
            clinic.IsActive = model.IsActive;

            if (logoFile != null)
            {
                var (isValid, errorMsg) = FileUploadValidator.ValidateImage(logoFile);
                if (!isValid)
                {
                    ModelState.AddModelError(string.Empty, errorMsg);
                    ViewBag.PageTitle = _localizer.T("Page.EditClinic");
                    return View(model);
                }
            }

            if (logoFile != null && logoFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(clinic.LogoPath))
                {
                    var oldPath = Path.Combine("wwwroot", clinic.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uploadsDir = Path.Combine("wwwroot", "uploads", "clinic-logos");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(logoFile.FileName);
                var fileName = $"clinic_{id}_{Path.GetRandomFileName()}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                clinic.LogoPath = $"/uploads/clinic-logos/{fileName}";
            }

            // Sync template assignments for selected standards
            var existingAssignments = (await _unitOfWork.Repository<ClinicTemplateAssignment>()
                .FindWithIncludesAsync(a => a.ClinicId == id, false, a => a.DocumentTemplate))
                .ToList();

            var selectedTemplates = new List<DocumentTemplate>();
            if (model.SelectedStandards?.Count > 0)
            {
                selectedTemplates = (await _unitOfWork.Repository<DocumentTemplate>()
                    .FindAsync(t => model.SelectedStandards.Contains(t.DepartmentCategory)
                                 && t.ClinicType == model.ClinicType
                                 && t.IsActive && !t.IsDeleted))
                    .ToList();
            }

            var selectedTemplateIds = selectedTemplates.Select(t => t.Id).ToHashSet();

            foreach (var template in selectedTemplates)
            {
                var exists = existingAssignments.Any(a => a.DocumentTemplateId == template.Id);
                if (!exists)
                {
                    await _assignmentService.AssignTemplateToClinicAsync(template.Id, id);
                }
            }

            foreach (var assignment in existingAssignments)
            {
                if (!selectedTemplateIds.Contains(assignment.DocumentTemplateId)
                    && assignment.DocumentTemplate != null
                    && !model.SelectedStandards.Contains(assignment.DocumentTemplate.DepartmentCategory ?? ""))
                {
                    _unitOfWork.Repository<ClinicTemplateAssignment>().SoftDelete(assignment);
                }
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = id,
                ActionType = AuditActionType.Update,
                TargetObjectId = id,
                TargetObjectType = nameof(Clinic),
                Description = $"Edited clinic: {model.Name}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.ClinicUpdated");

            return RedirectToAction("ClinicDetail", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing clinic {ClinicId}", id);
            ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.ClinicEditFailed"));
            ViewBag.PageTitle = _localizer.T("Page.EditClinic");
            ViewBag.StandardNames = GetStandardNames();
            ViewBag.StandardIcons = GetStandardIcons();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(id);
        if (clinic == null)
            return NotFound();

        try
        {
            if (!string.IsNullOrEmpty(clinic.LogoPath))
            {
                var logoPath = Path.Combine("wwwroot", clinic.LogoPath.TrimStart('/'));
                if (System.IO.File.Exists(logoPath))
                    System.IO.File.Delete(logoPath);
            }

            var clinicName = clinic.Name;

            await _clinicService.DeleteClinicAsync(id);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = id,
                ActionType = AuditActionType.Delete,
                TargetObjectId = id,
                TargetObjectType = nameof(Clinic),
                Description = $"Deleted clinic: {clinicName}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted clinic {ClinicId}: {Name}", id, clinicName);
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.ClinicDeleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting clinic {ClinicId}", id);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.ClinicDeleteFailed");
        }

        return RedirectToAction("Clinics", "Dashboard", new { area = "SuperAdmin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplateAssignment(int id, int clinicId)
    {
        var deleted = await _assignmentService.DeleteAssignmentAsync(id);
        if (deleted)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = clinicId,
                ActionType = AuditActionType.Delete,
                TargetObjectId = id,
                TargetObjectType = nameof(ClinicTemplateAssignment),
                Description = $"Deleted template assignment {id} from clinic {clinicId}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.AssignmentDeleted");
            _logger.LogInformation("Template assignment {AssignmentId} deleted from clinic {ClinicId}", id, clinicId);
        }
        else
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.AssignmentDeleteFailed");
        }

        return RedirectToAction("ClinicDetail", new { id = clinicId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDocumentValues(int clinicId, int assignmentId, List<UpsertClinicTemplateValueDto> values)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var saved = await _assignmentService.UpsertSuperAdminValuesAsync(assignmentId, values, userId);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            if (saved)
                return Ok();
            return BadRequest();
        }

        if (!saved)
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.ValuesSaveFailed");
        else
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.ValuesSaved");

        return RedirectToAction("ClinicDetail", new { id = clinicId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGlobalValues(int clinicId, List<UpsertGlobalTemplateValueDto> values)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        await _assignmentService.SaveGlobalTemplateValuesAsync(clinicId, values, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.ValuesSaved");
        return RedirectToAction("ClinicDetail", new { id = clinicId });
    }

    [HttpGet]
    public async Task<IActionResult> PreviewDocument(int assignmentId)
    {
        try
        {
            var docxBytes = await _generationService.PreviewPdfAsync(assignmentId);
            if (docxBytes == null)
                return NotFound();

            var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
            var clinic = assignment != null ? await _unitOfWork.Repository<Clinic>().GetByIdAsync(assignment.ClinicId) : null;
            var template = assignment != null ? await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(assignment.DocumentTemplateId) : null;
            var fileName = $"{clinic?.Name ?? "Clinic"}_{template?.StandardCode ?? "document"}.docx";
            var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", safeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview document for assignment {AssignmentId}", assignmentId);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
            return RedirectToAction("Clinics", "Dashboard", new { area = "SuperAdmin" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocumentWord(int assignmentId)
    {
        try
        {
            var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
                return RedirectToAction("Clinics", "Dashboard", new { area = "SuperAdmin" });
            }

            var docBytes = await _generationService.PreviewDocxAsync(assignmentId);
            if (docBytes == null)
            {
                TempData["ErrorMessage"] = _localizer.T("Alert.Error.TemplateFileNotFound");
                return RedirectToAction("ClinicDetail", new { id = assignment.ClinicId });
            }

            var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(assignment.ClinicId);
            var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(assignment.DocumentTemplateId);
            var fileName = $"{clinic?.Name ?? "Clinic"}_{template?.StandardCode ?? "document"}.docx";
            var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", safeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download document for assignment {AssignmentId}", assignmentId);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
            return RedirectToAction("Clinics", "Dashboard", new { area = "SuperAdmin" });
        }
    }
}
