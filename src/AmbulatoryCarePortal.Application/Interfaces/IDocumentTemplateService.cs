using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IDocumentTemplateService
{
    Task<PagedResult<DocumentTemplateDto>> GetAllTemplatesAsync(int pageNumber, int pageSize, string? searchTerm = null);
    Task<DocumentTemplateDto?> GetTemplateByIdAsync(int id);
    Task<int> CreateTemplateAsync(CreateDocumentTemplateDto dto);
    Task<bool> UpdateTemplateAsync(UpdateDocumentTemplateDto dto);
    Task<bool> DeleteTemplateAsync(int id);
    Task<bool> UploadTemplateFileAsync(int id, string filePath);
    Task AssignToAllClinicsAsync(int templateId);
    Task<List<DocumentTemplateDto>> GetTemplatesByTypeAndStandardAsync(ClinicType clinicType, string standard);
}
