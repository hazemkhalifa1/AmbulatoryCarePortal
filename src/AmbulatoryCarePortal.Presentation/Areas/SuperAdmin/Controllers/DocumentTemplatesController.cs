using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Constants;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Policy = "Permission.system.configure")]
public class DocumentTemplatesController : Controller
{
    private readonly IDocumentTemplateService _templateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentTemplatesController> _logger;
    private readonly ITranslationService _localizer;

    public DocumentTemplatesController(
        IDocumentTemplateService templateService,
        IUnitOfWork unitOfWork,
        ILogger<DocumentTemplatesController> logger,
        ITranslationService localizer)
    {
        _templateService = templateService;
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
                .GroupBy(t => t.StandardCode)
                .ToDictionary(g => g.Key, g => g.Count());

            if (!string.IsNullOrEmpty(standard))
            {
                Expression<Func<DocumentTemplate, bool>> predicate = t =>
                    t.ClinicType == parsedType && t.StandardCode == standard && !t.IsDeleted;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    predicate = t => t.ClinicType == parsedType && t.StandardCode == standard && !t.IsDeleted
                        && (t.TitleEn.ToLower().Contains(term) || (t.TitleAr != null && t.TitleAr.Contains(term)));
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

            return View(new PagedResult<DocumentTemplateDto>());
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
    public async Task<IActionResult> ByTypeAndStandard(ClinicType clinicType, string standard)
    {
        var templates = await _templateService.GetTemplatesByTypeAndStandardAsync(clinicType, standard);
        return Json(templates);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.PageTitle = _localizer.T("Page.CreateDocumentTemplate");
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

            if (templateFile != null && templateFile.Length > 0)
            {
                var fileName = $"{dto.StandardCode}_{Path.GetRandomFileName()}.docx";
                var filePath = Path.Combine("wwwroot/uploads/templates", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await templateFile.CopyToAsync(stream);
                }

                await _templateService.UploadTemplateFileAsync(templateId, $"/uploads/templates/{fileName}");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ActionType = Domain.Enums.AuditActionType.Create,
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
    public async Task<IActionResult> Edit(int id, UpdateDocumentTemplateDto dto, IFormFile? templateFile)
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

                await _templateService.UploadTemplateFileAsync(id, $"/uploads/templates/{fileName}");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ActionType = Domain.Enums.AuditActionType.Update,
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
        var deleted = await _templateService.DeleteTemplateAsync(id);
        if (!deleted)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var auditLog = new AuditTrail
        {
            ActionType = Domain.Enums.AuditActionType.Delete,
            TargetObjectId = id,
            TargetObjectType = nameof(DocumentTemplate),
            Description = $"Deleted document template Id: {id}",
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
        await _templateService.AssignToAllClinicsAsync(id);

        TempData["SuccessMessage"] = _localizer.T("Alert.Success.TemplateAssigned");
        return RedirectToAction(nameof(Index));
    }
}
