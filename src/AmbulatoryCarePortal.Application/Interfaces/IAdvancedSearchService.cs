using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IAdvancedSearchService
{
    Task<List<T>> SearchAsync<T>(
        IQueryable<T> query,
        string searchTerm,
        List<string> searchFields,
        Dictionary<string, object> filters) where T : class;
    Task<List<PolicyDocument>> SearchPoliciesAsync(int clinicId, string searchTerm, Dictionary<string, object> filters);
    Task<List<KPI>> SearchKPIsAsync(int clinicId, string searchTerm, Dictionary<string, object> filters);
    Task<List<HrStaff>> SearchStaffAsync(int clinicId, string searchTerm, Dictionary<string, object> filters);
    Task<List<AuditTrail>> SearchAuditLogsAsync(int clinicId, string searchTerm, Dictionary<string, object> filters);
}
