using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class HrDocumentConfiguration : IEntityTypeConfiguration<HrDocument>
{
    public void Configure(EntityTypeBuilder<HrDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.DocumentNameAr)
            .HasMaxLength(255);

        builder.Property(x => x.FilePath)
            .HasMaxLength(500);

        builder.HasOne(x => x.HrStaff)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.HrStaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany(x => x.HrDocuments)
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.HrStaffId, x.ExpiryDate })
            .HasFilter("[IsDeleted] = 0");
    }
}
