using AmbulatoryCarePortal.Application.DTOs.Document;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IClinicTemplateAssignmentService
{
    Task<List<ClinicTemplateAssignmentDto>> GetAssignmentsByClinicAsync(int clinicId, string? searchTerm = null, string? statusFilter = null);
    Task<List<ClinicTemplateAssignmentDto>> GetAssignmentsByTemplateAsync(int templateId);
    Task<ClinicTemplateAssignmentDto?> GetAssignmentByIdAsync(int id);
    Task AssignTemplateToClinicAsync(int templateId, int clinicId);
    Task AssignTemplateToAllClinicsAsync(int templateId);
    Task<bool> UpdateAssignmentStatusAsync(int id, string status);
    Task<bool> UpdateAssignmentNotesAsync(int id, string? notes);
    Task<bool> DeleteAssignmentAsync(int id);
    Task<List<ClinicTemplateValueDto>> GetValuesForAssignmentAsync(int assignmentId);
    Task<bool> UpsertValuesAsync(int assignmentId, int clinicId, List<UpsertClinicTemplateValueDto> values, string userId);
    Task<bool> UploadVariableImageAsync(int assignmentId, int variableId, int clinicId, string fileName, string filePath, string userId);
}
