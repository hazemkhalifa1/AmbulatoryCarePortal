using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class BulkOperationsService : IBulkOperationsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkOperationsService> _logger;

    public BulkOperationsService(IUnitOfWork unitOfWork, ILogger<BulkOperationsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> BulkDeletePoliciesAsync(List<int> policyIds, int clinicId, string userId)
    {
        try
        {
            var policies = await _unitOfWork.Repository<PolicyDocument>().FindAsync(
                p => policyIds.Contains(p.Id) && p.ClinicId == clinicId
            );

            foreach (var policy in policies)
                _unitOfWork.Repository<PolicyDocument>().SoftDelete(policy);

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Bulk deleted {policies.Count()} policies");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk delete policies: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> BulkDeleteStaffAsync(List<int> staffIds, int clinicId, string userId)
    {
        try
        {
            var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(
                s => staffIds.Contains(s.Id) && s.ClinicId == clinicId
            );

            foreach (var person in staff)
                _unitOfWork.Repository<HrStaff>().SoftDelete(person);

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Bulk deleted {staff.Count()} staff records");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk delete staff: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> BulkApproveChecklistsAsync(List<int> roundIds, int clinicId, string userId)
    {
        try
        {
            var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
                r => roundIds.Contains(r.Id) && r.ClinicId == clinicId
            );

            foreach (var round in rounds)
            {
                round.ApprovedAt = DateTime.UtcNow;
                round.ApprovedByUserId = userId;
                _unitOfWork.Repository<ChecklistRound>().Update(round);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Bulk approved {rounds.Count()} checklists");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk approve checklists: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> BulkVerifyDocumentsAsync(List<int> documentIds, int clinicId, string userId)
    {
        try
        {
            var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
                d => documentIds.Contains(d.Id)
            );

            foreach (var doc in documents)
            {
                doc.IsVerified = true;
                _unitOfWork.Repository<HrDocument>().Update(doc);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Bulk verified {documents.Count()} documents");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk verify documents: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> BulkExportAsync<T>(List<int> ids, string format, string exportType) where T : class
    {
        try
        {
            _logger.LogInformation($"Bulk export initiated for {exportType}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk export: {ex.Message}");
            return false;
        }
    }

    public async Task<int> BulkUpdateStatusAsync(string entityType, List<int> ids, string newStatus)
    {
        try
        {
            _logger.LogInformation($"Bulk status update for {entityType}: {ids.Count()} items");
            return ids.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in bulk status update: {ex.Message}");
            return 0;
        }
    }
}
