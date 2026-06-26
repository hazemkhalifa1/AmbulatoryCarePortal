using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Constants;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Policy = "Permission.system.configure")]
public class DocumentTemplatesController : Controller
{
    private readonly IDocumentTemplateService _templateService;
    private readonly ITemplateVariableService _variableService;
    private readonly IClinicTemplateAssignmentService _assignmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentTemplatesController> _logger;
    private readonly ITranslationService _localizer;

    public DocumentTemplatesController(
        IDocumentTemplateService templateService,
        ITemplateVariableService variableService,
        IClinicTemplateAssignmentService assignmentService,
        IUnitOfWork unitOfWork,
        ILogger<DocumentTemplatesController> logger,
        ITranslationService localizer)
    {
        _templateService = templateService;
        _variableService = variableService;
        _assignmentService = assignmentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? clinicType = null,
        string? standard = null)
    {
        ViewBag.SearchTerm = searchTerm;
        ViewBag.SelectedClinicType = clinicType;
        ViewBag.SelectedStandard = standard;
        ViewBag.PageTitle = _localizer.T("Page.DocumentTemplates");

        if (!string.IsNullOrEmpty(clinicType) && Enum.TryParse<ClinicType>(clinicType, out var parsedType))
        {
            var standards = ClinicTypeStandards.GetStandards(parsedType);
            ViewBag.Standards = standards;

            var allForType = await _unitOfWork.Repository<DocumentTemplate>()
                .FindAsync(t => t.ClinicType == parsedType && !t.IsDeleted);
            ViewBag.DocumentCounts = allForType
                .GroupBy(t => t.DepartmentCategory)
                .ToDictionary(g => g.Key, g => g.Count());

            Expression<Func<DocumentTemplate, bool>> predicate;

            if (!string.IsNullOrEmpty(standard))
            {
                predicate = t => t.ClinicType == parsedType && t.DepartmentCategory == standard && !t.IsDeleted;
            }
            else
            {
                predicate = t => t.ClinicType == parsedType && !t.IsDeleted;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.ToLower();
                predicate = t => t.ClinicType == parsedType && !t.IsDeleted
                    && (t.TitleEn.ToLower().Contains(term) || (t.TitleAr != null && t.TitleAr.Contains(term)))
                    && (string.IsNullOrEmpty(standard) || t.DepartmentCategory == standard);
            }

            var pagedResult = await _unitOfWork.Repository<DocumentTemplate>()
                .GetPagedAsync(page, pageSize, predicate, t => t.StandardCode);

            var dtoList = pagedResult.Data.Select(t => new DocumentTemplateDto
            {
                Id = t.Id,
                StandardCode = t.StandardCode,
                TitleEn = t.TitleEn,
                TitleAr = t.TitleAr,
                Description = t.Description,
                DepartmentCategory = t.DepartmentCategory,
                ClinicType = t.ClinicType,
                TemplateFilePath = t.TemplateFilePath,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            }).ToList();

            var result = new PagedResult<DocumentTemplateDto>
            {
                Data = dtoList,
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };

            return View(result);
        }

        var allResult = await _templateService.GetAllTemplatesAsync(page, pageSize, searchTerm);
        return View(allResult);
    }

    [HttpGet]
    public JsonResult GetStandards(string clinicType)
    {
        if (!Enum.TryParse<ClinicType>(clinicType, out var type))
            return Json(new { standards = Array.Empty<string>() });

        var standards = ClinicTypeStandards.GetStandards(type);
        return Json(new { standards });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var details = await _templateService.GetTemplateDetailsAsync(id);
        if (details == null) return NotFound();

        ViewBag.PageTitle = $"{details.StandardCode} - {details.TitleEn}";
        return View(details);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtractVariables(int id)
    {
        var variables = await _variableService.ExtractVariablesFromFileAsync(id);
        TempData["SuccessMessage"] = $"{variables.Count} variables extracted from template.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Create(string? clinicType = null, string? standard = null)
    {
        ViewBag.PageTitle = _localizer.T("Page.CreateDocumentTemplate");
        ViewBag.SelectedClinicType = clinicType;
        ViewBag.SelectedStandard = standard;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDocumentTemplateDto dto, IFormFile? templateFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = _localizer.T("Page.CreateDocumentTemplate");
            return View(dto);
        }

        try
        {
            var templateId = await _templateService.CreateTemplateAsync(dto);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (templateFile != null)
            {
                var (isValid, errorMsg) = FileUploadValidator.ValidateTemplate(templateFile);
                if (!isValid)
                {
                    ModelState.AddModelError(string.Empty, errorMsg);
                    ViewBag.PageTitle = _localizer.T("Page.CreateDocumentTemplate");
                    return View(dto);
                }
            }

            if (templateFile != null && templateFile.Length > 0)
            {
                var fileName = $"{dto.StandardCode}_{Path.GetRandomFileName()}.docx";
                var filePath = Path.Combine("wwwroot/uploads/templates", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await templateFile.CopyToAsync(stream);
                }

                await _templateService.UploadTemplateFileAsync(templateId, $"/uploads/templates/{fileName}", "Initial upload", userId ?? "");

                await _variableService.ExtractVariablesFromFileAsync(templateId);
            }

            var auditLog = new AuditTrail
            {
                ActionType = AuditActionType.Create,
                TargetObjectId = templateId,
                TargetObjectType = nameof(DocumentTemplate),
                Description = $"Created document template: {dto.StandardCode}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.TemplateCreated");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document template");
            ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.TemplateCreateFailed"));
            ViewBag.PageTitle = _localizer.T("Page.CreateDocumentTemplate");
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
            return NotFound();

        var dto = new UpdateDocumentTemplateDto
        {
            Id = template.Id,
            StandardCode = template.StandardCode,
            TitleEn = template.TitleEn,
            TitleAr = template.TitleAr,
            Description = template.Description,
            DepartmentCategory = template.DepartmentCategory,
            ClinicType = template.ClinicType,
            IsActive = template.IsActive
        };

        ViewBag.TemplateFilePath = template.TemplateFilePath;
        ViewBag.PageTitle = _localizer.T("Page.EditDocumentTemplate");

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateDocumentTemplateDto dto, IFormFile? templateFile, string? changeLog)
    {
        if (id != dto.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.PageTitle = _localizer.T("Page.EditDocumentTemplate");
            return View(dto);
        }

        try
        {
            var updated = await _templateService.UpdateTemplateAsync(dto);
            if (!updated)
                return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (templateFile != null)
            {
                var (isValid, errorMsg) = FileUploadValidator.ValidateTemplate(templateFile);
                if (!isValid)
                {
                    ModelState.AddModelError(string.Empty, errorMsg);
                    ViewBag.PageTitle = _localizer.T("Page.EditDocumentTemplate");
                    return View(dto);
                }
            }

            if (templateFile != null && templateFile.Length > 0)
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                if (template != null && !string.IsNullOrEmpty(template.TemplateFilePath))
                {
                    var oldFullPath = Path.Combine("wwwroot", template.TemplateFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFullPath))
                        System.IO.File.Delete(oldFullPath);
                }

                var fileName = $"{dto.StandardCode}_{Path.GetRandomFileName()}.docx";
                var filePath = Path.Combine("wwwroot/uploads/templates", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await templateFile.CopyToAsync(stream);
                }

                await _templateService.UploadTemplateFileAsync(id, $"/uploads/templates/{fileName}", changeLog ?? "Updated file", userId ?? "");

                await _variableService.ExtractVariablesFromFileAsync(id);
            }

            var auditLog = new AuditTrail
            {
                ActionType = AuditActionType.Update,
                TargetObjectId = id,
                TargetObjectType = nameof(DocumentTemplate),
                Description = $"Updated document template: {dto.StandardCode}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.TemplateUpdated");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document template");
            ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.TemplateUpdateFailed"));
            ViewBag.PageTitle = _localizer.T("Page.EditDocumentTemplate");
            return View(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
            return NotFound();

        if (!string.IsNullOrEmpty(template.TemplateFilePath))
        {
            var filePath = Path.Combine("wwwroot", template.TemplateFilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        var versions = await _templateService.GetVersionsAsync(id);
        foreach (var version in versions)
        {
            if (!string.IsNullOrEmpty(version.FilePath))
            {
                var versionPath = Path.Combine("wwwroot", version.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(versionPath))
                    System.IO.File.Delete(versionPath);
            }
        }

        await _templateService.DeleteTemplateAsync(id);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Delete,
            TargetObjectId = id,
            TargetObjectType = nameof(DocumentTemplate),
            Description = $"Deleted document template: {template.StandardCode}",
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = _localizer.T("Alert.Success.TemplateDeleted");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToAllClinics(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        await _assignmentService.AssignTemplateToAllClinicsAsync(id);

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Create,
            TargetObjectId = id,
            TargetObjectType = nameof(ClinicTemplateAssignment),
            Description = $"Assigned template to all clinics",
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = _localizer.T("Alert.Success.TemplateAssigned");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVariable(int id, UpdateTemplateVariableDto dto)
    {
        dto.Id = id;
        var updated = await _variableService.UpdateVariableAsync(dto);
        if (!updated) return NotFound();
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariable(int id)
    {
        var deleted = await _variableService.DeleteVariableAsync(id);
        if (!deleted) return NotFound();
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreVersion(int templateId, int versionId)
    {
        var restored = await _templateService.RestoreVersionAsync(templateId, versionId);
        if (!restored) return NotFound();

        TempData["SuccessMessage"] = "Template version restored successfully.";
        return RedirectToAction(nameof(Details), new { id = templateId });
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var details = await _templateService.GetTemplateDetailsAsync(id);
        if (details == null) return NotFound();

        ViewBag.PageTitle = _localizer.T("Page.PreviewTemplate");
        return View(details);
    }
}
