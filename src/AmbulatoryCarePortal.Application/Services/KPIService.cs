using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Interfaces;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Services;

public class KPIService : IKPIService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public KPIService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<KPIDto>> GetClinicKPIsAsync(int clinicId)
    {
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(x => x.ClinicId == clinicId);
        return _mapper.Map<List<KPIDto>>(kpis);
    }

    public async Task<List<KPIDto>> GetDepartmentKPIsAsync(int departmentId)
    {
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(x => x.DepartmentId == departmentId);
        return _mapper.Map<List<KPIDto>>(kpis);
    }

    public async Task<int> CreateKPIAsync(CreateKPIDto dto)
    {
        var kpi = _mapper.Map<KPI>(dto);
        await _unitOfWork.Repository<KPI>().AddAsync(kpi);
        await _unitOfWork.SaveChangesAsync();

        return kpi.Id;
    }

    public async Task<bool> UpdateKPIAsync(int id, CreateKPIDto dto)
    {
        var kpi = await _unitOfWork.Repository<KPI>().GetByIdAsync(id);
        if (kpi == null)
            return false;

        _mapper.Map(dto, kpi);
        kpi.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<KPI>().Update(kpi);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteKPIAsync(int kpiId)
    {
        var kpi = await _unitOfWork.Repository<KPI>().GetByIdAsync(kpiId);
        if (kpi == null)
            return false;

        _unitOfWork.Repository<KPI>().SoftDelete(kpi);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddKPIEntryAsync(int kpiId, int month, int year, decimal actualValue)
    {
        var existingEntry = await _unitOfWork.Repository<KPIEntry>().FirstOrDefaultAsync(x =>
            x.KPIId == kpiId && x.PeriodMonth == month && x.PeriodYear == year
        );

        if (existingEntry != null)
        {
            existingEntry.ActualValue = actualValue;
            _unitOfWork.Repository<KPIEntry>().Update(existingEntry);
        }
        else
        {
            var entry = new KPIEntry
            {
                KPIId = kpiId,
                PeriodMonth = month,
                PeriodYear = year,
                ActualValue = actualValue
            };
            await _unitOfWork.Repository<KPIEntry>().AddAsync(entry);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
