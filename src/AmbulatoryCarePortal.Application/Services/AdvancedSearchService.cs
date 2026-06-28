using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class AdvancedSearchService : IAdvancedSearchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdvancedSearchService> _logger;

    public AdvancedSearchService(IUnitOfWork unitOfWork, ILogger<AdvancedSearchService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<T>> SearchAsync<T>(
        IQueryable<T> query,
        string searchTerm,
        List<string> searchFields,
        Dictionary<string, object> filters) where T : class
    {
        try
        {
            foreach (var filter in filters) { }

            if (!string.IsNullOrEmpty(searchTerm)) { }

            var result = query.ToList();
            _logger.LogInformation($"Search completed for {typeof(T).Name}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in search: {ex.Message}");
            throw;
        }
    }

    public async Task<List<KPI>> SearchKPIsAsync(int clinicId, string searchTerm, Dictionary<string, object> filters)
    {
        try
        {
            var kpis = await _unitOfWork.Repository<KPI>().FindAsync(
                k => k.ClinicId == clinicId &&
                     (string.IsNullOrEmpty(searchTerm) || k.Name.Contains(searchTerm))
            );

            _logger.LogInformation("KPIs search completed");
            return kpis.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching KPIs: {ex.Message}");
            throw;
        }
    }

    public async Task<List<HrStaff>> SearchStaffAsync(int clinicId, string searchTerm, Dictionary<string, object> filters)
    {
        try
        {
            var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(
                s => s.ClinicId == clinicId &&
                     (string.IsNullOrEmpty(searchTerm) || s.FullNameEn.Contains(searchTerm))
            );

            _logger.LogInformation("Staff search completed");
            return staff.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching staff: {ex.Message}");
            throw;
        }
    }

    public async Task<List<AuditTrail>> SearchAuditLogsAsync(int clinicId, string searchTerm, Dictionary<string, object> filters)
    {
        try
        {
            var logs = await _unitOfWork.Repository<AuditTrail>().FindAsync(
                a => a.ClinicId == clinicId &&
                     (string.IsNullOrEmpty(searchTerm) || (a.Description != null && a.Description.Contains(searchTerm)) || (a.CreatedBy != null && a.CreatedBy.Contains(searchTerm)))
            );

            _logger.LogInformation("Audit logs search completed");
            return logs.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching audit logs: {ex.Message}");
            throw;
        }
    }
}
