using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class PolicyDocumentConfiguration : IEntityTypeConfiguration<PolicyDocument>
{
    public void Configure(EntityTypeBuilder<PolicyDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.TitleAr)
            .HasMaxLength(255);

        builder.Property(x => x.StandardCode)
            .HasMaxLength(50);

        builder.Property(x => x.OfficialPdfPath)
            .HasMaxLength(500);

        builder.Property(x => x.DocumentStatus)
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Department)
            .WithMany(x => x.PolicyDocuments)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.PolicyDocuments)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.PolicyDocument)
            .HasForeignKey(x => x.PolicyDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AuditTrails)
            .WithOne()
            .HasForeignKey("PolicyDocumentId")
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => new { x.ClinicId, x.StandardCode }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.ClinicId, x.DocumentStatus, x.ExpiryDate })
            .HasFilter("[IsDeleted] = 0");
    }
}
