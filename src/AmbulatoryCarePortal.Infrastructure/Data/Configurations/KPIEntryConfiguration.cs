using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class KPIEntryConfiguration : IEntityTypeConfiguration<KPIEntry>
{
    public void Configure(EntityTypeBuilder<KPIEntry> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActualValue)
            .HasPrecision(10, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.EvidenceFilePath)
            .HasMaxLength(500);

        builder.HasOne(x => x.KPI)
            .WithMany(x => x.MonthlyEntries)
            .HasForeignKey(x => x.KPIId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.KPIId, x.PeriodYear, x.PeriodMonth })
            .IsUnique();
    }
}
