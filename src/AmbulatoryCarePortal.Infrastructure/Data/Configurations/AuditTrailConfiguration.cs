using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TargetObjectType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.OldValues)
            .HasMaxLength(2000);

        builder.Property(x => x.NewValues)
            .HasMaxLength(2000);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(50);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.AuditTrails)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.User)
            .WithMany(x => x.AuditTrails)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClinicId, x.ActionDate })
            .IsDescending(false, true);
    }
}
