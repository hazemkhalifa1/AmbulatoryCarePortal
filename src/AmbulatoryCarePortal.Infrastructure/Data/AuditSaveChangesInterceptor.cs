using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    public AuditSaveChangesInterceptor(ILogger<AuditSaveChangesInterceptor> logger)
    {
        _logger = logger;
    }

    private static readonly HashSet<string> SkippedTypes = ["AuditTrail", "AuditTrailAttachment"];
    private static readonly int ClinicIdPropertyIndex = typeof(BaseEntity).GetProperties().Length;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is not AppDbContext context)
            return await base.SavingChangesAsync(eventData, result, ct);

        var auditEntries = new List<AuditTrail>();
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var typeName = entry.Entity.GetType().Name;
            if (SkippedTypes.Contains(typeName))
                continue;

            var clinicId = ResolveClinicId(entry);

            var auditEntry = new AuditTrail
            {
                ActionDate = now,
                ClinicId = clinicId,
                TargetObjectType = typeName,
                TargetObjectId = entry.Entity.Id,
                Description = entry.State switch
                {
                    EntityState.Added => $"Created {typeName} (Id: {entry.Entity.Id})",
                    EntityState.Modified => $"Updated {typeName} (Id: {entry.Entity.Id})",
                    EntityState.Deleted => $"Deleted {typeName} (Id: {entry.Entity.Id})",
                    _ => $"{entry.State} {typeName} (Id: {entry.Entity.Id})"
                }
            };

            if (entry.State == EntityState.Modified)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified && prop.Metadata.Name != "UpdatedAt")
                    {
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                if (oldValues.Count > 0)
                {
                    auditEntry.OldValues = JsonSerializer.Serialize(oldValues);
                    auditEntry.NewValues = JsonSerializer.Serialize(newValues);
                }

                if (entry.Entity.IsDeleted && !Equals(entry.OriginalValues["IsDeleted"], entry.CurrentValues["IsDeleted"]))
                {
                    auditEntry.Description = $"Soft-deleted {typeName} (Id: {entry.Entity.Id})";
                }
            }
            else if (entry.State == EntityState.Added)
            {
                var newValues = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    if (prop.CurrentValue != null && prop.Metadata.Name != "Id" && prop.Metadata.Name != "CreatedAt")
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                if (newValues.Count > 0)
                    auditEntry.NewValues = JsonSerializer.Serialize(newValues);
            }

            auditEntries.Add(auditEntry);
        }

        foreach (var audit in auditEntries)
        {
            context.AuditTrails.Add(audit);
        }

        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private static int? ResolveClinicId(EntityEntry entry)
    {
        var entity = entry.Entity;

        if (entity is Clinic clinic && clinic.Id > 0)
            return clinic.Id;

        var clinicIdProp = entity.GetType().GetProperty("ClinicId", BindingFlags.Public | BindingFlags.Instance);
        if (clinicIdProp != null && clinicIdProp.PropertyType == typeof(int))
        {
            var value = clinicIdProp.GetValue(entity);
            if (value is int id && id > 0)
                return id;
        }

        return null;
    }
}
