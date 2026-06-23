using AutoMapper;
using Microsoft.Extensions.Logging;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Application.Services;

public class TemplateSignerService : ITemplateSignerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TemplateSignerService> _logger;

    public TemplateSignerService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TemplateSignerService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<TemplateSignerDto>> GetSignersByTemplateAsync(int templateId)
    {
        var signers = await _unitOfWork.Repository<TemplateSigner>()
            .FindAsync(s => s.DocumentTemplateId == templateId);
        return _mapper.Map<List<TemplateSignerDto>>(signers.ToList());
    }

    public async Task<List<TemplateSignerDto>> GetSignersByClinicAsync(int clinicId)
    {
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.ClinicId == clinicId);

        var templateIds = assignments.Select(a => a.DocumentTemplateId).Distinct().ToList();

        var signers = await _unitOfWork.Repository<TemplateSigner>()
            .FindAsync(s => templateIds.Contains(s.DocumentTemplateId));

        return signers
            .GroupBy(s => s.SignerCode)
            .Select(g => g.First())
            .Select(_mapper.Map<TemplateSignerDto>)
            .ToList();
    }

    public async Task<TemplateSignerDto?> GetSignerByIdAsync(int id)
    {
        var signer = await _unitOfWork.Repository<TemplateSigner>().GetByIdAsync(id);
        return signer == null ? null : _mapper.Map<TemplateSignerDto>(signer);
    }

    public async Task<int> CreateSignerAsync(CreateTemplateSignerDto dto, int templateId)
    {
        var signer = new TemplateSigner
        {
            DocumentTemplateId = templateId,
            SignerCode = dto.SignerCode,
            SignerDisplayName = dto.SignerDisplayName,
            SignerTitle = dto.SignerTitle,
            IsRequired = dto.IsRequired,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<TemplateSigner>().AddAsync(signer);
        await _unitOfWork.SaveChangesAsync();
        return signer.Id;
    }

    public async Task<bool> UpdateSignerAsync(int id, CreateTemplateSignerDto dto)
    {
        var signer = await _unitOfWork.Repository<TemplateSigner>().GetByIdAsync(id);
        if (signer == null) return false;

        signer.SignerDisplayName = dto.SignerDisplayName;
        signer.SignerTitle = dto.SignerTitle;
        signer.IsRequired = dto.IsRequired;
        signer.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<TemplateSigner>().Update(signer);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSignerAsync(int id)
    {
        var signer = await _unitOfWork.Repository<TemplateSigner>().GetByIdAsync(id);
        if (signer == null) return false;

        _unitOfWork.Repository<TemplateSigner>().SoftDelete(signer);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
