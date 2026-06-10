using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<PolicyDocument> PolicyDocuments { get; set; }
    public DbSet<EvidenceAttachment> EvidenceAttachments { get; set; }
    public DbSet<KPI> KPIs { get; set; }
    public DbSet<KPIEntry> KPIEntries { get; set; }
    public DbSet<ChecklistTemplate> ChecklistTemplates { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }
    public DbSet<ChecklistRound> ChecklistRounds { get; set; }
    public DbSet<ChecklistAnswer> ChecklistAnswers { get; set; }
    public DbSet<Form> Forms { get; set; }
    public DbSet<FormVersion> FormVersions { get; set; }
    public DbSet<HrStaff> HrStaffs { get; set; }
    public DbSet<HrDocument> HrDocuments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditTrail> AuditTrails { get; set; }
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
    public DbSet<ClinicDocument> ClinicDocuments { get; set; }
    public DbSet<ClinicDocumentAttachment> ClinicDocumentAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Force Restrict delete behavior for all FKs to prevent SQL Server "multiple cascade paths" errors
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Global query filters for soft delete
        modelBuilder.Entity<Clinic>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<PolicyDocument>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<EvidenceAttachment>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<KPI>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<KPIEntry>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ChecklistTemplate>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ChecklistItem>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ChecklistRound>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ChecklistAnswer>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Form>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<FormVersion>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<HrStaff>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<HrDocument>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<AuditTrail>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<DocumentTemplate>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ClinicDocument>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ClinicDocumentAttachment>().HasQueryFilter(x => !x.IsDeleted);
    }
}
