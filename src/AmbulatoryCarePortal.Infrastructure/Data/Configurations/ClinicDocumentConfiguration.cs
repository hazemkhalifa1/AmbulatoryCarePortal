using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ClinicDocumentConfiguration : IEntityTypeConfiguration<ClinicDocument>
{
    public void Configure(EntityTypeBuilder<ClinicDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentStatus).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.OfficialPdfPath).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.ClinicDocuments)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DocumentTemplate)
            .WithMany(x => x.ClinicDocuments)
            .HasForeignKey(x => x.DocumentTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.ClinicDocument)
            .HasForeignKey(x => x.ClinicDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClinicId, x.DocumentTemplateId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
