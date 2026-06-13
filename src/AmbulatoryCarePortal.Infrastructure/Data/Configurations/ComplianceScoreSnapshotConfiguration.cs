using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ComplianceScoreSnapshotConfiguration : IEntityTypeConfiguration<ComplianceScoreSnapshot>
{
    public void Configure(EntityTypeBuilder<ComplianceScoreSnapshot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OverallScore).HasPrecision(5, 2);
        builder.Property(x => x.PolicyScore).HasPrecision(5, 2);
        builder.Property(x => x.KpiScore).HasPrecision(5, 2);
        builder.Property(x => x.ChecklistScore).HasPrecision(5, 2);
        builder.Property(x => x.HrScore).HasPrecision(5, 2);
        builder.Property(x => x.DocumentScore).HasPrecision(5, 2);
        builder.Property(x => x.PolicyWeight).HasPrecision(5, 2);
        builder.Property(x => x.KpiWeight).HasPrecision(5, 2);
        builder.Property(x => x.ChecklistWeight).HasPrecision(5, 2);
        builder.Property(x => x.HrWeight).HasPrecision(5, 2);
        builder.Property(x => x.DocumentWeight).HasPrecision(5, 2);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.ComplianceScoreSnapshots)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClinicId, x.CalculatedAt })
            .IsDescending(false, true);
    }
}
