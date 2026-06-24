using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.documents.manage")]
public class ClinicDocumentFillController : Controller
{
    private readonly IClinicTemplateAssignmentService _assignmentService;
    private readonly IDocumentGenerationService _generationService;
    private readonly ITemplateVariableService _variableService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClinicDocumentFillController> _logger;
    private readonly ITranslationService _localizer;

    public ClinicDocumentFillController(
        IClinicTemplateAssignmentService assignmentService,
        IDocumentGenerationService generationService,
        ITemplateVariableService variableService,
        IUnitOfWork unitOfWork,
        ILogger<ClinicDocumentFillController> logger,
        ITranslationService localizer)
    {
        _assignmentService = assignmentService;
        _generationService = generationService;
        _variableService = variableService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _localizer = localizer;
    }

    private int GetCurrentClinicId()
    {
        var claim = User.FindFirst("ClinicId");
        if (claim != null && int.TryParse(claim.Value, out var id))
            return id;
        return 0;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm = null, string? statusFilter = null)
    {
        ViewBag.SearchTerm = searchTerm;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.PageTitle = _localizer.T("Page.DocumentFill");

        var clinicId = GetCurrentClinicId();
        var assignments = await _assignmentService.GetAssignmentsByClinicAsync(clinicId, searchTerm, statusFilter);

        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> Fill(int id)
    {
        var assignment = await _assignmentService.GetAssignmentByIdAsync(id);
        if (assignment == null) return NotFound();

        var clinicId = GetCurrentClinicId();
        if (assignment.ClinicId != clinicId) return Forbid();

        var values = await _assignmentService.GetValuesForAssignmentAsync(id);
        ViewBag.Assignment = assignment;
        ViewBag.PageTitle = $"{assignment.StandardCode} - {assignment.TitleEn}";
        return View(values);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveValues(int assignmentId, int clinicId, List<UpsertClinicTemplateValueDto> values)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var currentClinicId = GetCurrentClinicId();

        if (clinicId != currentClinicId) return Forbid();

        var saved = await _assignmentService.UpsertValuesAsync(assignmentId, clinicId, values, userId);
        if (!saved)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.ValuesSaveFailed");
            return RedirectToAction(nameof(Fill), new { id = assignmentId });
        }

        TempData["SuccessMessage"] = _localizer.T("Alert.Success.ValuesSaved");
        return RedirectToAction(nameof(Fill), new { id = assignmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(int assignmentId, int variableId, IFormFile imageFile)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var clinicId = GetCurrentClinicId();

        if (imageFile == null || imageFile.Length == 0)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.NoFileSelected");
            return RedirectToAction(nameof(Fill), new { id = assignmentId });
        }

        var fileName = $"{variableId}_{Path.GetRandomFileName()}{Path.GetExtension(imageFile.FileName)}";
        var relativePath = $"/uploads/document-evidence/{fileName}";
        var fullPath = Path.Combine("wwwroot/uploads/document-evidence", fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? "");

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        var uploaded = await _assignmentService.UploadVariableImageAsync(assignmentId, variableId, clinicId, fileName, relativePath, userId);
        if (!uploaded)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.ImageUploadFailed");
            return RedirectToAction(nameof(Fill), new { id = assignmentId });
        }

        TempData["SuccessMessage"] = _localizer.T("Alert.Success.ImageUploaded");
        return RedirectToAction(nameof(Fill), new { id = assignmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateDocx(int assignmentId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        try
        {
            var generatedDoc = await _generationService.GenerateDocxAsync(assignmentId, userId);

            if (generatedDoc == null)
            {
                TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
                return RedirectToAction(nameof(Fill), new { id = assignmentId });
            }

            var auditLog = new AuditTrail
            {
                ActionType = AuditActionType.Create,
                TargetObjectId = generatedDoc.Id,
                TargetObjectType = nameof(GeneratedDocument),
                Description = $"Generated DOCX: {generatedDoc.FileName}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.DocumentGenerated");
            return RedirectToAction(nameof(Fill), new { id = assignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DOCX for assignment {AssignmentId}", assignmentId);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
            return RedirectToAction(nameof(Fill), new { id = assignmentId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePdf(int assignmentId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        try
        {
            var generatedDoc = await _generationService.GeneratePdfAsync(assignmentId, userId);

            if (generatedDoc == null)
            {
                TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
                return RedirectToAction(nameof(Fill), new { id = assignmentId });
            }

            var auditLog = new AuditTrail
            {
                ActionType = AuditActionType.Create,
                TargetObjectId = generatedDoc.Id,
                TargetObjectType = nameof(GeneratedDocument),
                Description = $"Generated PDF: {generatedDoc.FileName}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = _localizer.T("Alert.Success.DocumentGenerated");
            return RedirectToAction(nameof(Fill), new { id = assignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for assignment {AssignmentId}", assignmentId);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
            return RedirectToAction(nameof(Fill), new { id = assignmentId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var fileBytes = await _generationService.DownloadGeneratedFileAsync(id);

        if (fileBytes == null) return NotFound();

        var generatedDoc = await _unitOfWork.Repository<GeneratedDocument>().GetByIdAsync(id);
        if (generatedDoc == null) return NotFound();

        var clinicId = GetCurrentClinicId();
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(generatedDoc.ClinicTemplateAssignmentId);
        if (assignment?.ClinicId != clinicId) return Forbid();

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Download,
            TargetObjectId = id,
            TargetObjectType = nameof(GeneratedDocument),
            Description = $"Downloaded document: {generatedDoc.FileName}",
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        var contentType = generatedDoc.FileType.ToLower() == "pdf" ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        return File(fileBytes, contentType, generatedDoc.FileName);
    }
}
