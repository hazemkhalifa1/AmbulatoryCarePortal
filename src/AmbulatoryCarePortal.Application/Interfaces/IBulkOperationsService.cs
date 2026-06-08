namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IBulkOperationsService
{
    Task<bool> BulkDeletePoliciesAsync(List<int> policyIds, int clinicId, string userId);
    Task<bool> BulkDeleteStaffAsync(List<int> staffIds, int clinicId, string userId);
    Task<bool> BulkApproveChecklistsAsync(List<int> roundIds, int clinicId, string userId);
    Task<bool> BulkVerifyDocumentsAsync(List<int> documentIds, int clinicId, string userId);
    Task<bool> BulkExportAsync<T>(List<int> ids, string format, string exportType) where T : class;
    Task<int> BulkUpdateStatusAsync(string entityType, List<int> ids, string newStatus);
}
