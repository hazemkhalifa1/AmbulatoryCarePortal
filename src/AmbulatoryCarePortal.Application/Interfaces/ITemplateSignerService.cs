using AmbulatoryCarePortal.Application.DTOs.Document;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface ITemplateSignerService
{
    Task<List<TemplateSignerDto>> GetSignersByTemplateAsync(int templateId);
    Task<List<TemplateSignerDto>> GetSignersByClinicAsync(int clinicId);
    Task<TemplateSignerDto?> GetSignerByIdAsync(int id);
    Task<int> CreateSignerAsync(CreateTemplateSignerDto dto, int templateId);
    Task<bool> UpdateSignerAsync(int id, CreateTemplateSignerDto dto);
    Task<bool> DeleteSignerAsync(int id);
}
