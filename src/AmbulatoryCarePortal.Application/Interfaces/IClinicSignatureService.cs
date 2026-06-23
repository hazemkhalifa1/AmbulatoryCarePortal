using AmbulatoryCarePortal.Application.DTOs.Document;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IClinicSignatureService
{
    Task<List<SignerViewModel>> GetRequiredSignersAsync(int clinicId);
    Task<ClinicSignatureDto?> GetSignatureAsync(int clinicId, string signerCode);
    Task<bool> SaveSignatureAsync(int clinicId, string signerCode, string signerName, string signerTitle, string imagePath, string signatureType);
    Task<bool> DeleteSignatureAsync(int clinicId, string signerCode);
    Task<string?> ResolveSignatureImagePathAsync(int clinicId, string signerCode);
    Task<string?> ResolveSignerNameAsync(int clinicId, string signerCode);
    Task<string?> ResolveSignerTitleAsync(int clinicId, string signerCode);
}
