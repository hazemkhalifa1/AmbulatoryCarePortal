using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Presentation.Helpers;

namespace AmbulatoryCarePortal.Presentation.Areas.ClinicAdmin.Controllers;

[Area("ClinicAdmin")]
[Authorize(Policy = "Permission.signatures.view")]
public class ClinicSignaturesController : Controller
{
    private readonly IClinicSignatureService _signatureService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentGenerationService _generationService;
    private readonly ITranslationService _localizer;
    private readonly ILogger<ClinicSignaturesController> _logger;

    public ClinicSignaturesController(
        IClinicSignatureService signatureService,
        IUnitOfWork unitOfWork,
        IDocumentGenerationService generationService,
        ITranslationService localizer,
        ILogger<ClinicSignaturesController> logger)
    {
        _signatureService = signatureService;
        _unitOfWork = unitOfWork;
        _generationService = generationService;
        _localizer = localizer;
        _logger = logger;
    }

    private int GetCurrentClinicId()
    {
        var claim = User.FindFirst("ClinicId");
        if (claim != null && int.TryParse(claim.Value, out var id))
            return id;
        return 0;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var clinicId = GetCurrentClinicId();
        if (clinicId == 0) return Forbid();

        ViewBag.PageTitle = _localizer.T("Page.Signatures");
        var signers = await _signatureService.GetRequiredSignersAsync(clinicId);
        return View(signers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.signatures.manage")]
    public async Task<IActionResult> SaveSignature(int signerId, string signerCode, string signerName, string signerTitle, string signatureData)
    {
        var clinicId = GetCurrentClinicId();
        if (clinicId == 0) return Json(new { success = false, message = _localizer.T("Alert.Error.Unauthorized") });

        if (string.IsNullOrWhiteSpace(signatureData))
            return Json(new { success = false, message = _localizer.T("Alert.Error.NoSignatureData") });

        try
        {
            var base64Data = signatureData.Contains(",")
                ? signatureData.Substring(signatureData.IndexOf(",") + 1)
                : signatureData;

            var imageBytes = Convert.FromBase64String(base64Data);

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "signatures", clinicId.ToString());
            Directory.CreateDirectory(dir);

            var fileName = $"{signerCode}.png";
            var relativePath = $"/uploads/signatures/{clinicId}/{fileName}";
            var fullPath = Path.Combine(dir, fileName);

            System.IO.File.WriteAllBytes(fullPath, imageBytes);

            var saved = await _signatureService.SaveSignatureAsync(clinicId, signerCode, signerName, signerTitle, relativePath, "Drawn");

            if (!saved)
                return Json(new { success = false, message = _localizer.T("Alert.Error.SignatureSaveFailed") });

            return Json(new { success = true, message = _localizer.T("Alert.Success.SignatureSaved"), imagePath = relativePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save drawn signature for clinic {ClinicId}", clinicId);
            return Json(new { success = false, message = _localizer.T("Alert.Error.SignatureSaveFailed") });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.signatures.manage")]
    public async Task<IActionResult> UploadSignature(string signerCode, string signerName, string signerTitle, IFormFile signatureFile)
    {
        var clinicId = GetCurrentClinicId();
        if (clinicId == 0) return Json(new { success = false, message = _localizer.T("Alert.Error.Unauthorized") });

        if (signatureFile == null || signatureFile.Length == 0)
            return Json(new { success = false, message = _localizer.T("Alert.Error.NoFileSelected") });

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg" };
        if (!allowedTypes.Contains(signatureFile.ContentType.ToLower()))
            return Json(new { success = false, message = _localizer.T("Alert.Error.InvalidFileType") });

        if (signatureFile.Length > 2 * 1024 * 1024)
            return Json(new { success = false, message = _localizer.T("Alert.Error.FileTooLarge") });

        try
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "signatures", clinicId.ToString());
            Directory.CreateDirectory(dir);

            var fileName = $"{signerCode}.png";
            var relativePath = $"/uploads/signatures/{clinicId}/{fileName}";
            var fullPath = Path.Combine(dir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await signatureFile.CopyToAsync(stream);
            }

            var saved = await _signatureService.SaveSignatureAsync(clinicId, signerCode, signerName, signerTitle, relativePath, "Uploaded");

            if (!saved)
                return Json(new { success = false, message = _localizer.T("Alert.Error.SignatureSaveFailed") });

            return Json(new { success = true, message = _localizer.T("Alert.Success.SignatureSaved"), imagePath = relativePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload signature for clinic {ClinicId}", clinicId);
            return Json(new { success = false, message = _localizer.T("Alert.Error.SignatureUploadFailed") });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission.signatures.manage")]
    public async Task<IActionResult> DeleteSignature(string signerCode)
    {
        var clinicId = GetCurrentClinicId();
        if (clinicId == 0) return Json(new { success = false, message = _localizer.T("Alert.Error.Unauthorized") });

        var deleted = await _signatureService.DeleteSignatureAsync(clinicId, signerCode);

        if (!deleted)
            return Json(new { success = false, message = _localizer.T("Alert.Error.SignatureDeleteFailed") });

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "signatures", clinicId.ToString(), $"{signerCode}.png");
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        return Json(new { success = true, message = _localizer.T("Alert.Success.SignatureDeleted") });
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int assignmentId)
    {
        var clinicId = GetCurrentClinicId();
        if (clinicId == 0) return Forbid();

        var assignment = await _unitOfWork.Repository<AmbulatoryCarePortal.Domain.Entities.ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null || assignment.ClinicId != clinicId) return NotFound();

        try
        {
            var pdfBytes = await _generationService.PreviewPdfAsync(assignmentId);
            if (pdfBytes == null)
                return NotFound();

            return File(pdfBytes, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview document for assignment {AssignmentId}", assignmentId);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadWord(int assignmentId)
    {
        var clinicId = GetCurrentClinicId();
        if (clinicId == 0) return Forbid();

        var assignment = await _unitOfWork.Repository<AmbulatoryCarePortal.Domain.Entities.ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null || assignment.ClinicId != clinicId) return NotFound();

        try
        {
            var docBytes = await _generationService.DownloadDocxAsync(assignmentId);
            if (docBytes == null)
                return NotFound();

            var template = await _unitOfWork.Repository<AmbulatoryCarePortal.Domain.Entities.DocumentTemplate>().GetByIdAsync(assignment.DocumentTemplateId);
            var fileName = $"{template?.StandardCode ?? "document"}_{DateTime.Now:yyyyMMddHHmmss}.docx";
            return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Word document for assignment {AssignmentId}", assignmentId);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int assignmentId)
    {
        var clinicId = GetCurrentClinicId();
        if (clinicId == 0) return Forbid();

        var assignment = await _unitOfWork.Repository<AmbulatoryCarePortal.Domain.Entities.ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null || assignment.ClinicId != clinicId) return NotFound();

        try
        {
            var pdfBytes = await _generationService.DownloadPdfAsync(assignmentId);
            if (pdfBytes == null)
                return NotFound();

            var template = await _unitOfWork.Repository<AmbulatoryCarePortal.Domain.Entities.DocumentTemplate>().GetByIdAsync(assignment.DocumentTemplateId);
            var fileName = $"{template?.StandardCode ?? "document"}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download PDF document for assignment {AssignmentId}", assignmentId);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.DocumentGenerationFailed");
            return RedirectToAction(nameof(Index));
        }
    }
}
