using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IClinicDocumentService
{
    Task<List<ClinicDocumentDto>> GetClinicDocumentsAsync(int clinicId, string? searchTerm = null, string? statusFilter = null, string? standardFilter = null);
    Task<ClinicDocumentDetailDto?> GetClinicDocumentDetailsAsync(int id);
    Task<bool> UploadEvidenceAsync(int clinicDocumentId, string fileName, string filePath, string fileType, string uploadedByUserId, string? notes);
    Task<bool> DeleteAttachmentAsync(int attachmentId);
    Task<bool> UpdateStatusAsync(int clinicDocumentId, ClinicDocumentStatus status);
    Task<(byte[] FileContent, string FileName)?> DownloadDocumentAsync(int clinicDocumentId);
}
