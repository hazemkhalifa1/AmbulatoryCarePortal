using AmbulatoryCarePortal.Application.DTOs.Document;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IDocumentGenerationService
{
    Task<GeneratedDocumentDto?> GenerateDocxAsync(int assignmentId, string userId);
    Task<GeneratedDocumentDto?> GeneratePdfAsync(int assignmentId, string userId);
    Task<byte[]?> DownloadGeneratedFileAsync(int generatedDocumentId);
    Task<List<GeneratedDocumentDto>> GetGeneratedDocumentsAsync(int assignmentId);
    Task<List<TemplateVariablePreviewDto>> ValidateVariablesAsync(int assignmentId);
}
