using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class KPIConfiguration : IEntityTypeConfiguration<KPI>
{
    public void Configure(EntityTypeBuilder<KPI> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.NameAr)
            .HasMaxLength(255);

        builder.Property(x => x.TargetValue)
            .HasPrecision(10, 2);

        builder.Property(x => x.Frequency)
            .IsRequired();

        builder.Property(x => x.CalculationFormula)
            .HasMaxLength(1000);

        builder.Property(x => x.EvidenceRequired)
            .HasMaxLength(1000);

        builder.Property(x => x.EscalationRule)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.KPIs)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.KPIs)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.MonthlyEntries)
            .WithOne(x => x.KPI)
            .HasForeignKey(x => x.KPIId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
