using AutoMapper;
using Microsoft.Extensions.Logging;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Application.Services;

public class ClinicSignatureService : IClinicSignatureService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ClinicSignatureService> _logger;

    public ClinicSignatureService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ClinicSignatureService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<SignerViewModel>> GetRequiredSignersAsync(int clinicId)
    {
        var signerCodes = new Dictionary<string, TemplateSigner>();

        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindWithIncludesAsync(a => a.ClinicId == clinicId,
                includes: a => a.DocumentTemplate);

        foreach (var assignment in assignments)
        {
            var templateSigners = await _unitOfWork.Repository<TemplateSigner>()
                .FindAsync(s => s.DocumentTemplateId == assignment.DocumentTemplateId);

            foreach (var signer in templateSigners)
            {
                signerCodes.TryAdd(signer.SignerCode, signer);
            }
        }

        var existingSignatures = await _unitOfWork.Repository<ClinicSignature>()
            .FindAsync(s => s.ClinicId == clinicId);

        var sigByCode = existingSignatures.ToDictionary(s => s.SignerCode, s => s);

        var result = new List<SignerViewModel>();
        foreach (var kvp in signerCodes)
        {
            var signer = kvp.Value;
            var sig = sigByCode.GetValueOrDefault(signer.SignerCode);

            result.Add(new SignerViewModel
            {
                SignerCode = signer.SignerCode,
                SignerDisplayName = signer.SignerDisplayName,
                SignerTitle = signer.SignerTitle,
                IsRequired = signer.IsRequired,
                HasSignature = sig != null && !string.IsNullOrEmpty(sig.SignatureImagePath),
                SignatureImagePath = sig?.SignatureImagePath,
                CurrentSignerName = sig?.SignerName,
                CurrentSignerTitle = sig?.SignerTitle,
                SignatureId = sig?.Id
            });
        }

        return result;
    }

    public async Task<ClinicSignatureDto?> GetSignatureAsync(int clinicId, string signerCode)
    {
        var sig = await _unitOfWork.Repository<ClinicSignature>()
            .FirstOrDefaultAsync(s => s.ClinicId == clinicId && s.SignerCode == signerCode);

        return sig == null ? null : _mapper.Map<ClinicSignatureDto>(sig);
    }

    public async Task<bool> SaveSignatureAsync(int clinicId, string signerCode, string signerName, string signerTitle, string imagePath, string signatureType)
    {
        try
        {
            var existing = await _unitOfWork.Repository<ClinicSignature>()
                .FirstOrDefaultAsync(s => s.ClinicId == clinicId && s.SignerCode == signerCode);

            if (existing != null)
            {
                existing.SignerName = signerName;
                existing.SignerTitle = signerTitle;
                existing.SignatureImagePath = imagePath;
                existing.SignatureType = signatureType == "Uploaded" ? Domain.Enums.SignatureType.Uploaded : Domain.Enums.SignatureType.Drawn;
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<ClinicSignature>().Update(existing);
            }
            else
            {
                var sig = new ClinicSignature
                {
                    ClinicId = clinicId,
                    SignerCode = signerCode,
                    SignerName = signerName,
                    SignerTitle = signerTitle,
                    SignatureImagePath = imagePath,
                    SignatureType = signatureType == "Uploaded" ? Domain.Enums.SignatureType.Uploaded : Domain.Enums.SignatureType.Drawn,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<ClinicSignature>().AddAsync(sig);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save signature for clinic {ClinicId}, signer {SignerCode}", clinicId, signerCode);
            return false;
        }
    }

    public async Task<bool> DeleteSignatureAsync(int clinicId, string signerCode)
    {
        try
        {
            var sig = await _unitOfWork.Repository<ClinicSignature>()
                .FirstOrDefaultAsync(s => s.ClinicId == clinicId && s.SignerCode == signerCode);

            if (sig == null) return false;

            _unitOfWork.Repository<ClinicSignature>().SoftDelete(sig);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete signature for clinic {ClinicId}, signer {SignerCode}", clinicId, signerCode);
            return false;
        }
    }

    public async Task<string?> ResolveSignatureImagePathAsync(int clinicId, string signerCode)
    {
        var sig = await _unitOfWork.Repository<ClinicSignature>()
            .FirstOrDefaultAsync(s => s.ClinicId == clinicId && s.SignerCode == signerCode);

        return sig?.SignatureImagePath;
    }

    public async Task<string?> ResolveSignerNameAsync(int clinicId, string signerCode)
    {
        var sig = await _unitOfWork.Repository<ClinicSignature>()
            .FirstOrDefaultAsync(s => s.ClinicId == clinicId && s.SignerCode == signerCode);

        return sig?.SignerName;
    }

    public async Task<string?> ResolveSignerTitleAsync(int clinicId, string signerCode)
    {
        var sig = await _unitOfWork.Repository<ClinicSignature>()
            .FirstOrDefaultAsync(s => s.ClinicId == clinicId && s.SignerCode == signerCode);

        return sig?.SignerTitle;
    }
}
