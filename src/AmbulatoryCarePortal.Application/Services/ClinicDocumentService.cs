using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AutoMapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class ClinicDocumentService : IClinicDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ClinicDocumentService> _logger;

    public ClinicDocumentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ClinicDocumentService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ClinicDocumentDto>> GetClinicDocumentsAsync(int clinicId, string? searchTerm = null, string? statusFilter = null, string? standardFilter = null)
    {
        var clinicDocs = await _unitOfWork.Repository<ClinicDocument>().FindAsync(
            cd => cd.ClinicId == clinicId
        );

        var clinicDocList = clinicDocs.ToList();
        var result = new List<ClinicDocumentDto>();

        foreach (var cd in clinicDocList)
        {
            var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(cd.DocumentTemplateId);
            var attachmentCount = await _unitOfWork.Repository<ClinicDocumentAttachment>()
                .CountAsync(a => a.ClinicDocumentId == cd.Id);

            if (template == null)
                continue;

            if (!string.IsNullOrEmpty(searchTerm) &&
                !template.StandardCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) &&
                !template.TitleEn.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) &&
                !(template.DepartmentCategory?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                continue;

            if (!string.IsNullOrEmpty(statusFilter) &&
                !string.Equals(cd.DocumentStatus.ToString(), statusFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(standardFilter) &&
                !string.Equals(template.DepartmentCategory, standardFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(new ClinicDocumentDto
            {
                Id = cd.Id,
                ClinicId = cd.ClinicId,
                DocumentTemplateId = cd.DocumentTemplateId,
                StandardCode = template.StandardCode,
                TitleEn = template.TitleEn,
                TitleAr = template.TitleAr,
                DepartmentCategory = template.DepartmentCategory,
                DocumentStatus = cd.DocumentStatus,
                ExpiryDate = cd.ExpiryDate,
                OfficialPdfPath = cd.OfficialPdfPath,
                Notes = cd.Notes,
                AttachmentCount = attachmentCount
            });
        }

        return result.OrderBy(x => x.StandardCode).ToList();
    }

    public async Task<ClinicDocumentDetailDto?> GetClinicDocumentDetailsAsync(int id)
    {
        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(id);
        if (clinicDoc == null)
            return null;

        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(clinicDoc.DocumentTemplateId);
        if (template == null)
            return null;

        var attachments = await _unitOfWork.Repository<ClinicDocumentAttachment>()
            .FindAsync(a => a.ClinicDocumentId == id);

        var attachmentDtos = _mapper.Map<List<ClinicDocumentAttachmentDto>>(attachments.ToList());

        return new ClinicDocumentDetailDto
        {
            Id = clinicDoc.Id,
            ClinicId = clinicDoc.ClinicId,
            DocumentTemplateId = clinicDoc.DocumentTemplateId,
            StandardCode = template.StandardCode,
            TitleEn = template.TitleEn,
            TitleAr = template.TitleAr,
            DepartmentCategory = template.DepartmentCategory,
            DocumentStatus = clinicDoc.DocumentStatus,
            ExpiryDate = clinicDoc.ExpiryDate,
            OfficialPdfPath = clinicDoc.OfficialPdfPath,
            Notes = clinicDoc.Notes,
            Attachments = attachmentDtos
        };
    }

    public async Task<bool> UploadEvidenceAsync(int clinicDocumentId, string fileName, string filePath, string fileType, string uploadedByUserId, string? notes)
    {
        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(clinicDocumentId);
        if (clinicDoc == null)
            return false;

        var attachment = new ClinicDocumentAttachment
        {
            ClinicDocumentId = clinicDocumentId,
            FileName = fileName,
            FilePath = filePath,
            FileType = fileType,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
            Notes = notes,
            CreatedBy = uploadedByUserId
        };

        await _unitOfWork.Repository<ClinicDocumentAttachment>().AddAsync(attachment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId)
    {
        var attachment = await _unitOfWork.Repository<ClinicDocumentAttachment>().GetByIdAsync(attachmentId);
        if (attachment == null)
            return false;

        _unitOfWork.Repository<ClinicDocumentAttachment>().SoftDelete(attachment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateStatusAsync(int clinicDocumentId, ClinicDocumentStatus status)
    {
        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(clinicDocumentId);
        if (clinicDoc == null)
            return false;

        clinicDoc.DocumentStatus = status;
        clinicDoc.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<ClinicDocument>().Update(clinicDoc);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<(byte[] FileContent, string FileName)?> DownloadDocumentAsync(int clinicDocumentId)
    {
        var clinicDoc = await _unitOfWork.Repository<ClinicDocument>().GetByIdAsync(clinicDocumentId);
        if (clinicDoc == null)
            return null;

        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(clinicDoc.DocumentTemplateId);
        if (template == null || string.IsNullOrEmpty(template.TemplateFilePath))
            return null;

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicDoc.ClinicId);
        if (clinic == null)
            return null;

        var templatePath = template.TemplateFilePath;
        var fullPath = templatePath;
        if (!(templatePath.Length >= 2 && templatePath[1] == ':' && char.IsLetter(templatePath[0])))
        {
            fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", templatePath.TrimStart('/', '\\'));
        }

        if (!File.Exists(fullPath))
            return null;

        var variables = new Dictionary<string, string>
        {
            ["{{ClinicName}}"] = clinic.Name,
            ["{{ClinicNameAr}}"] = clinic.NameAr ?? "",
            ["{{LogoPath}}"] = clinic.LogoPath ?? "",
            ["{{LicenseNumber}}"] = clinic.LicenseNumber ?? "",
            ["{{LicenseExpiry}}"] = clinic.LicenseExpiry?.ToString("yyyy-MM-dd") ?? "",
            ["{{CityEn}}"] = clinic.CityEn ?? "",
            ["{{CityAr}}"] = clinic.CityAr ?? "",
            ["{{CurrentDate}}"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["{{CurrentYear}}"] = DateTime.Now.Year.ToString(),
        };

        byte[] fileBytes;
        using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
        using (var memStream = new MemoryStream())
        {
            stream.CopyTo(memStream);
            memStream.Position = 0;

            using (var wordDoc = WordprocessingDocument.Open(memStream, true))
            {
                ReplacePlaceholders(wordDoc, variables);
            }

            fileBytes = memStream.ToArray();
        }

        var fileName = $"{template.StandardCode}_{clinic.Name}.docx";
        var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

        return (fileBytes, safeFileName);
    }

    private static void ReplacePlaceholders(WordprocessingDocument doc, Dictionary<string, string> variables)
    {
        if (doc.MainDocumentPart?.Document?.Body == null)
            return;

        ReplaceInElement(doc.MainDocumentPart.Document.Body, variables);

        if (doc.MainDocumentPart.HeaderParts != null)
        {
            foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
            {
                if (headerPart.RootElement != null)
                    ReplaceInElement(headerPart.RootElement, variables);
            }
        }

        if (doc.MainDocumentPart.FooterParts != null)
        {
            foreach (var footerPart in doc.MainDocumentPart.FooterParts)
            {
                if (footerPart.RootElement != null)
                    ReplaceInElement(footerPart.RootElement, variables);
            }
        }
    }

    private static void ReplaceInElement(OpenXmlElement element, Dictionary<string, string> variables)
    {
        foreach (var text in element.Descendants<Text>())
        {
            if (text.Text == null)
                continue;

            var modified = text.Text;
            foreach (var kvp in variables)
            {
                modified = modified.Replace(kvp.Key, kvp.Value);
            }

            if (modified != text.Text)
                text.Text = modified;
        }
    }
}
