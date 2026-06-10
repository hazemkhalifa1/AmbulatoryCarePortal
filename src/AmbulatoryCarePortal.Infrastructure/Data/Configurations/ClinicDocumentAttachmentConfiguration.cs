using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ClinicDocumentAttachmentConfiguration : IEntityTypeConfiguration<ClinicDocumentAttachment>
{
    public void Configure(EntityTypeBuilder<ClinicDocumentAttachment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.FileType).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.ClinicDocument)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.ClinicDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
