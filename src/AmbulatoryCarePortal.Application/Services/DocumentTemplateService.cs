using AmbulatoryCarePortal.Application.Common;
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

    public async Task<int> CreateTemplateAsync(CreateDocumentTemplateDto dto)
    {
        var template = _mapper.Map<DocumentTemplate>(dto);
        template.IsActive = true;

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

    public async Task<bool> UploadTemplateFileAsync(int id, string filePath)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(id);
        if (template == null)
            return false;

        template.TemplateFilePath = filePath;
        template.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<DocumentTemplate>().Update(template);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task AssignToAllClinicsAsync(int templateId)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(templateId);
        if (template == null)
            return;

        var activeClinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => c.IsActive);
        var existingAssignments = await _unitOfWork.Repository<ClinicDocument>()
            .FindAsync(cd => cd.DocumentTemplateId == templateId);

        var existingClinicIds = existingAssignments.Select(a => a.ClinicId).ToHashSet();

        var newAssignments = activeClinics
            .Where(c => !existingClinicIds.Contains(c.Id))
            .Select(c => new ClinicDocument
            {
                ClinicId = c.Id,
                DocumentTemplateId = templateId,
                DocumentStatus = ClinicDocumentStatus.NeedsReview,
                CreatedAt = DateTime.UtcNow
            }).ToList();

        if (newAssignments.Count == 0)
            return;

        await _unitOfWork.Repository<ClinicDocument>().AddRangeAsync(newAssignments);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Template {TemplateId} assigned to {Count} clinics", templateId, newAssignments.Count);
    }
}
