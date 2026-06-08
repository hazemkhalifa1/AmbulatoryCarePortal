using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class EvidenceAttachmentConfiguration : IEntityTypeConfiguration<EvidenceAttachment>
{
    public void Configure(EntityTypeBuilder<EvidenceAttachment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.DocumentNameAr)
            .HasMaxLength(255);

        builder.Property(x => x.FilePath)
            .HasMaxLength(500);

        builder.Property(x => x.FileType)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(x => x.PolicyDocument)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.PolicyDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany(x => x.EvidenceAttachments)
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
