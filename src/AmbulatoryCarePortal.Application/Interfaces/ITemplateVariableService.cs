using AmbulatoryCarePortal.Application.DTOs.Document;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface ITemplateVariableService
{
    Task<List<TemplateVariableDto>> GetVariablesByTemplateIdAsync(int templateId);
    Task<List<TemplateVariableDto>> ExtractVariablesFromFileAsync(int templateId);
    Task<TemplateVariableDto?> GetVariableByIdAsync(int id);
    Task<TemplateVariableDto> CreateVariableAsync(int templateId, CreateTemplateVariableDto dto);
    Task<bool> UpdateVariableAsync(UpdateTemplateVariableDto dto);
    Task<bool> DeleteVariableAsync(int id);
    Task<List<TemplateVariablePreviewDto>> PreviewVariableValuesAsync(int assignmentId);
}
