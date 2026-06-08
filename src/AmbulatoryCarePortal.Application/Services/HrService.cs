using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Interfaces;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Services;

public class HrService : IHrService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public HrService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<HrStaffDto>> GetClinicStaffAsync(int clinicId, int pageNumber, int pageSize)
    {
        var pagedResult = await _unitOfWork.Repository<HrStaff>().GetPagedAsync(
            pageNumber,
            pageSize,
            predicate: x => x.ClinicId == clinicId,
            orderBy: x => x.FullNameEn
        );

        var staff = _mapper.Map<List<HrStaffDto>>(pagedResult.Data);

        return new PagedResult<HrStaffDto>
        {
            Data = staff,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<HrStaffDetailDto?> GetStaffDetailsAsync(int staffId)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(staffId);
        if (staff == null)
            return null;

        return _mapper.Map<HrStaffDetailDto>(staff);
    }

    public async Task<int> CreateStaffAsync(CreateHrStaffDto dto)
    {
        var staff = _mapper.Map<HrStaff>(dto);
        staff.IsActive = true;

        await _unitOfWork.Repository<HrStaff>().AddAsync(staff);
        await _unitOfWork.SaveChangesAsync();

        return staff.Id;
    }

    public async Task<bool> UpdateStaffAsync(int id, CreateHrStaffDto dto)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(id);
        if (staff == null)
            return false;

        _mapper.Map(dto, staff);
        staff.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<HrStaff>().Update(staff);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteStaffAsync(int staffId)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().GetByIdAsync(staffId);
        if (staff == null)
            return false;

        _unitOfWork.Repository<HrStaff>().SoftDelete(staff);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddDocumentAsync(CreateHrDocumentDto dto)
    {
        var document = _mapper.Map<HrDocument>(dto);
        await _unitOfWork.Repository<HrDocument>().AddAsync(document);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<List<HrDocumentDto>> GetExpiringDocumentsAsync(int clinicId, int daysThreshold = 30)
    {
        var expiryDate = DateTime.Now.AddDays(daysThreshold);

        var staffIds = (await _unitOfWork.Repository<HrStaff>().FindAsync(s => s.ClinicId == clinicId))
            .Select(s => s.Id)
            .ToHashSet();

        var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(x =>
            staffIds.Contains(x.HrStaffId) &&
            x.ExpiryDate.HasValue &&
            x.ExpiryDate <= expiryDate
        );

        return _mapper.Map<List<HrDocumentDto>>(documents);
    }
}
