using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IFormService
{
    Task<List<FormDto>> GetClinicFormsAsync(int clinicId);
    Task<FormDto?> GetFormByIdAsync(int formId);
    Task<int> CreateFormAsync(CreateFormDto dto);
    Task<bool> UpdateFormAsync(int id, CreateFormDto dto);
    Task<bool> DeleteFormAsync(int formId);
    Task<List<FormVersionDto>> GetFormVersionHistoryAsync(int formId);
    Task<int> UploadNewVersionAsync(int formId, string filePath, string userId, string? notes);
}
