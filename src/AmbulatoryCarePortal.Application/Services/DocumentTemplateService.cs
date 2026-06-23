using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Constants;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class DocumentTemplateService : IDocumentTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentTemplateService> _logger;

    public DocumentTemplateService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<DocumentTemplateService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<DocumentTemplateDto>> GetAllTemplatesAsync(int pageNumber, int pageSize, string? searchTerm = null)
    {
        var pagedResult = await _unitOfWork.Repository<DocumentTemplate>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: string.IsNullOrEmpty(searchTerm) ? null :
                (System.Linq.Expressions.Expression<Func<DocumentTemplate, bool>>)(t =>
                    t.StandardCode.Contains(searchTerm) ||
                    t.TitleEn.Contains(searchTerm) ||
                    (t.TitleAr != null && t.TitleAr.Contains(searchTerm)) ||
                    (t.DepartmentCategory != null && t.DepartmentCategory.Contains(searchTerm))),
            orderBy: x => x.StandardCode,
            ascending: true
        );

        var dtos = _mapper.Map<List<DocumentTemplateDto>>(pagedResult.Data);

        return new PagedResult<DocumentTemplateDto>
        {
            Data = dtos,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<DocumentTemplateDto?> GetTemplateByIdAsync(int id)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(id);
        return template == null ? null : _mapper.Map<DocumentTemplateDto>(template);
    }

    public async Task<TemplateDetailsDto?> GetTemplateDetailsAsync(int id)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(id);
        if (template == null) return null;

        var variables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == id);
        var versions = await _unitOfWork.Repository<DocumentTemplateVersion>()
            .FindAsync(v => v.DocumentTemplateId == id);
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.DocumentTemplateId == id);
        var generated = await _unitOfWork.Repository<GeneratedDocument>()
            .FindAsync(g => g.DocumentTemplateId == id);

        var clinicIds = assignments.Select(a => a.ClinicId).ToHashSet();
        var clinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => clinicIds.Contains(c.Id));
        var clinicMap = clinics.ToDictionary(c => c.Id, c => c.Name);

        return new TemplateDetailsDto
        {
            Id = template.Id,
            StandardCode = template.StandardCode,
            TitleEn = template.TitleEn,
            TitleAr = template.TitleAr,
            Description = template.Description,
            DepartmentCategory = template.DepartmentCategory,
            ClinicType = template.ClinicType.ToString(),
            TemplateFilePath = template.TemplateFilePath,
            IsActive = template.IsActive,
            CurrentVersion = template.CurrentVersion,
            CreatedAt = template.CreatedAt,
            Variables = _mapper.Map<List<TemplateVariableDto>>(variables.ToList()),
            Versions = _mapper.Map<List<DocumentTemplateVersionDto>>(versions.OrderByDescending(v => v.VersionNumber).ToList()),
            Assignments = assignments.Select(a => new ClinicTemplateAssignmentDto
            {
                Id = a.Id,
                ClinicId = a.ClinicId,
                ClinicName = clinicMap.GetValueOrDefault(a.ClinicId, "Unknown"),
                DocumentTemplateId = a.DocumentTemplateId,
                StandardCode = template.StandardCode,
                TitleEn = template.TitleEn,
                TitleAr = template.TitleAr,
                AssignmentStatus = a.AssignmentStatus.ToString(),
                ExpiryDate = a.ExpiryDate,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            }).ToList(),
            GeneratedDocuments = _mapper.Map<List<GeneratedDocumentDto>>(generated.OrderByDescending(g => g.CreatedAt).ToList())
        };
    }

    public async Task<int> CreateTemplateAsync(CreateDocumentTemplateDto dto)
    {
        var existing = await _unitOfWork.Repository<DocumentTemplate>()
            .FindAsync(t => t.StandardCode == dto.StandardCode && !t.IsDeleted);
        if (existing.Any())
            throw new InvalidOperationException($"A template with StandardCode '{dto.StandardCode}' already exists.");

        var template = _mapper.Map<DocumentTemplate>(dto);
        template.IsActive = true;
        template.CurrentVersion = 1;

        await _unitOfWork.Repository<DocumentTemplate>().AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Document template {StandardCode} created with Id {Id}", template.StandardCode, template.Id);
        return template.Id;
    }

    public async Task<bool> UpdateTemplateAsync(UpdateDocumentTemplateDto dto)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(dto.Id);
        if (template == null)
            return false;

        if (template.StandardCode != dto.StandardCode)
        {
            var existing = await _unitOfWork.Repository<DocumentTemplate>()
                .FindAsync(t => t.StandardCode == dto.StandardCode && !t.IsDeleted);
            if (existing.Any())
                throw new InvalidOperationException($"A template with StandardCode '{dto.StandardCode}' already exists.");
        }

        _mapper.Map(dto, template);
        template.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<DocumentTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Document template {StandardCode} updated", template.StandardCode);
        return true;
    }

    public async Task<bool> DeleteTemplateAsync(int id)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(id);
        if (template == null)
            return false;

        _unitOfWork.Repository<DocumentTemplate>().SoftDelete(template);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Document template {StandardCode} deleted", template.StandardCode);
        return true;
    }

    public async Task<bool> UploadTemplateFileAsync(int id, string filePath, string changeLog, string userId)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(id);
        if (template == null)
            return false;

        var oldPath = template.TemplateFilePath;
        template.TemplateFilePath = filePath;
        template.CurrentVersion++;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = userId;

        _unitOfWork.Repository<DocumentTemplate>().Update(template);

        var version = new DocumentTemplateVersion
        {
            DocumentTemplateId = id,
            VersionNumber = template.CurrentVersion,
            FilePath = filePath,
            ChangeLog = string.IsNullOrEmpty(changeLog) ? $"Version {template.CurrentVersion}" : changeLog,
            UploadedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<DocumentTemplateVersion>().AddAsync(version);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Template {TemplateId} file uploaded, now version {Version}", id, template.CurrentVersion);
        return true;
    }

    public async Task AssignToAllClinicsAsync(int templateId)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return;

        var activeClinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => c.IsActive);

        var existingAssignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(cd => cd.DocumentTemplateId == templateId);
        var existingClinicIds = existingAssignments.Select(a => a.ClinicId).ToHashSet();

        var newAssignments = activeClinics
            .Where(c => !existingClinicIds.Contains(c.Id))
            .Select(c => new ClinicTemplateAssignment
            {
                ClinicId = c.Id,
                DocumentTemplateId = templateId,
                AssignmentStatus = ClinicDocumentStatus.NeedsReview,
                CreatedAt = DateTime.UtcNow
            }).ToList();

        if (newAssignments.Count == 0)
            return;

        await _unitOfWork.Repository<ClinicTemplateAssignment>().AddRangeAsync(newAssignments);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Template {TemplateId} assigned to {Count} clinics via new system", templateId, newAssignments.Count);
    }

    public async Task<List<DocumentTemplateDto>> GetTemplatesByTypeAndStandardAsync(ClinicType clinicType, string standard)
    {
        var templates = await _unitOfWork.Repository<DocumentTemplate>().FindAsync(
            t => t.ClinicType == clinicType && t.DepartmentCategory == standard && !t.IsDeleted
        );

        return _mapper.Map<List<DocumentTemplateDto>>(templates.ToList());
    }

    public async Task<List<DocumentTemplateVersionDto>> GetVersionsAsync(int templateId)
    {
        var versions = await _unitOfWork.Repository<DocumentTemplateVersion>()
            .FindAsync(v => v.DocumentTemplateId == templateId);
        return _mapper.Map<List<DocumentTemplateVersionDto>>(versions.OrderByDescending(v => v.VersionNumber).ToList());
    }

    public async Task<DocumentTemplateVersionDto?> GetVersionByIdAsync(int versionId)
    {
        var version = await _unitOfWork.Repository<DocumentTemplateVersion>().GetByIdAsync(versionId);
        return version == null ? null : _mapper.Map<DocumentTemplateVersionDto>(version);
    }

    public async Task<bool> RestoreVersionAsync(int templateId, int versionId)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(templateId);
        if (template == null) return false;

        var version = await _unitOfWork.Repository<DocumentTemplateVersion>().GetByIdAsync(versionId);
        if (version == null || version.DocumentTemplateId != templateId) return false;

        template.TemplateFilePath = version.FilePath;
        template.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<DocumentTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Template {TemplateId} restored to version {Version}", templateId, version.VersionNumber);
        return true;
    }
}
