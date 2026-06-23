using AmbulatoryCarePortal.Application.DTOs.Document;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IDocumentGenerationService
{
    Task<GeneratedDocumentDto?> GenerateDocxAsync(int assignmentId, string userId);
    Task<GeneratedDocumentDto?> GeneratePdfAsync(int assignmentId, string userId);
    Task<byte[]?> DownloadGeneratedFileAsync(int generatedDocumentId);
    Task<List<GeneratedDocumentDto>> GetGeneratedDocumentsAsync(int assignmentId);
    Task<List<TemplateVariablePreviewDto>> ValidateVariablesAsync(int assignmentId);


    Task<byte[]?> PreviewDocxAsync(int assignmentId);
    Task<byte[]?> PreviewPdfAsync(int assignmentId);
    Task<byte[]?> DownloadDocxAsync(int assignmentId);
    Task<byte[]?> DownloadPdfAsync(int assignmentId);
}
