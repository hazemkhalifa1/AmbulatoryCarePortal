using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Roles = "ClinicAdmin,ClinicViewer")]
public class ClinicDocumentsController : Controller
{
    private readonly IClinicDocumentService _clinicDocumentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClinicDocumentsController> _logger;
    private readonly ITranslationService _localizer;

    public ClinicDocumentsController(
        IClinicDocumentService clinicDocumentService,
        IUnitOfWork unitOfWork,
        ILogger<ClinicDocumentsController> logger,
        ITranslationService localizer)
    {
        _clinicDocumentService = clinicDocumentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm = null, string? statusFilter = null)
    {
        var clinicId = User.FindFirst("ClinicId") != null
            ? int.Parse(User.FindFirst("ClinicId")?.Value ?? "0")
            : 0;

        var documents = await _clinicDocumentService.GetClinicDocumentsAsync(clinicId, searchTerm, statusFilter);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.StatusFilter = statusFilter;

        return View(documents);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var docDetail = await _clinicDocumentService.GetClinicDocumentDetailsAsync(id);
        if (docDetail == null)
            return NotFound();

        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(id);
        if (clinicDoc == null || clinicDoc.ClinicId != clinicId)
            return Forbid();

        return View(docDetail);
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");

        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(id);
        if (clinicDoc == null || clinicDoc.ClinicId != clinicId)
            return Forbid();

        var result = await _clinicDocumentService.DownloadDocumentAsync(id);
        if (result == null)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.TemplateFileNotFound");
            return RedirectToAction(nameof(Index));
        }

        return File(result.Value.FileContent,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            result.Value.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEvidence(int clinicDocumentId, IFormFile evidenceFile, string? notes)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(clinicDocumentId);
        if (clinicDoc == null || clinicDoc.ClinicId != clinicId)
            return Forbid();

        if (evidenceFile == null || evidenceFile.Length == 0)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.NoFileSelected");
            return RedirectToAction(nameof(Details), new { id = clinicDocumentId });
        }

        var fileName = Path.GetRandomFileName() + Path.GetExtension(evidenceFile.FileName);
        var filePath = Path.Combine("wwwroot/uploads/document-evidence", fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await evidenceFile.CopyToAsync(stream);
        }

        var uploaded = await _clinicDocumentService.UploadEvidenceAsync(
            clinicDocumentId,
            evidenceFile.FileName,
            $"/uploads/document-evidence/{fileName}",
            Path.GetExtension(evidenceFile.FileName),
            userId ?? "",
            notes
        );

        if (!uploaded)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.EvidenceAttachFailed");
            return RedirectToAction(nameof(Details), new { id = clinicDocumentId });
        }

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Upload,
            TargetObjectId = clinicDocumentId,
            TargetObjectType = nameof(ClinicDocumentAttachment),
            Description = $"Uploaded evidence for clinic document Id: {clinicDocumentId}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Evidence uploaded for clinic document {DocumentId} by {UserId}", clinicDocumentId, userId);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.EvidenceUploaded");

        return RedirectToAction(nameof(Details), new { id = clinicDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var attachment = await _unitOfWork.Repository<ClinicDocumentAttachment>().GetByIdAsync(attachmentId);
        if (attachment == null)
            return NotFound();

        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(attachment.ClinicDocumentId);
        if (clinicDoc == null || clinicDoc.ClinicId != clinicId)
            return Forbid();

        var deleted = await _clinicDocumentService.DeleteAttachmentAsync(attachmentId);
        if (!deleted)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.AttachmentDeleteFailed");
            return RedirectToAction(nameof(Details), new { id = attachment.ClinicDocumentId });
        }

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Delete,
            TargetObjectId = attachmentId,
            TargetObjectType = nameof(ClinicDocumentAttachment),
            Description = $"Deleted evidence attachment for clinic document Id: {attachment.ClinicDocumentId}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = _localizer.T("Alert.Success.AttachmentDeleted");
        return RedirectToAction(nameof(Details), new { id = attachment.ClinicDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ClinicDocumentStatus status)
    {
        var clinicId = int.Parse(User.FindFirst("ClinicId")?.Value ?? "0");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(id);
        if (clinicDoc == null || clinicDoc.ClinicId != clinicId)
            return Forbid();

        var updated = await _clinicDocumentService.UpdateStatusAsync(id, status);
        if (!updated)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.StatusUpdateFailed");
            return RedirectToAction(nameof(Details), new { id });
        }

        var auditLog = new AuditTrail
        {
            ActionType = AuditActionType.Update,
            TargetObjectId = id,
            TargetObjectType = nameof(ClinicDocument),
            Description = $"Updated clinic document status to {status}",
            ClinicId = clinicId,
            CreatedBy = userId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = _localizer.T("Alert.Info.StatusUpdated", status);
        return RedirectToAction(nameof(Details), new { id });
    }
}
