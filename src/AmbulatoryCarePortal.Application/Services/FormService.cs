using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Application.Interfaces.Repositories;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Application.Services;

public class FormService : IFormService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FormService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<FormDto>> GetClinicFormsAsync(int clinicId)
    {
        var forms = await _unitOfWork.Repository<Form>().FindAsync(
            x => x.ClinicId == clinicId && x.IsActive
        );
        return _mapper.Map<List<FormDto>>(forms);
    }

    public async Task<FormDto?> GetFormByIdAsync(int formId)
    {
        var form = await _unitOfWork.Repository<Form>().GetByIdAsync(formId);
        return form == null ? null : _mapper.Map<FormDto>(form);
    }

    public async Task<int> CreateFormAsync(CreateFormDto dto)
    {
        var form = _mapper.Map<Form>(dto);
        form.VersionNumber = 1;
        form.IsActive = true;

        await _unitOfWork.Repository<Form>().AddAsync(form);
        await _unitOfWork.SaveChangesAsync();

        return form.Id;
    }

    public async Task<bool> UpdateFormAsync(int id, CreateFormDto dto)
    {
        var form = await _unitOfWork.Repository<Form>().GetByIdAsync(id);
        if (form == null) return false;

        form.Title = dto.Title;
        form.TitleAr = dto.TitleAr;
        form.Category = dto.Category;
        form.FilePath = dto.FilePath;
        form.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Form>().Update(form);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteFormAsync(int formId)
    {
        var form = await _unitOfWork.Repository<Form>().GetByIdAsync(formId);
        if (form == null) return false;

        _unitOfWork.Repository<Form>().SoftDelete(form);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<List<FormVersionDto>> GetFormVersionHistoryAsync(int formId)
    {
        var versions = await _unitOfWork.Repository<FormVersion>().FindAsync(
            x => x.FormId == formId
        );

        return _mapper.Map<List<FormVersionDto>>(
            versions.OrderByDescending(v => v.VersionNumber)
        );
    }

    public async Task<int> UploadNewVersionAsync(int formId, string filePath, string userId, string? notes)
    {
        var form = await _unitOfWork.Repository<Form>().GetByIdAsync(formId);
        if (form == null) return 0;

        form.VersionNumber++;
        form.FilePath = filePath;
        form.UpdatedAt = DateTime.UtcNow;

        var version = new FormVersion
        {
            FormId = formId,
            VersionNumber = form.VersionNumber,
            FilePath = filePath,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow,
            Notes = notes
        };

        _unitOfWork.Repository<Form>().Update(form);
        await _unitOfWork.Repository<FormVersion>().AddAsync(version);
        await _unitOfWork.SaveChangesAsync();

        return version.Id;
    }
}
