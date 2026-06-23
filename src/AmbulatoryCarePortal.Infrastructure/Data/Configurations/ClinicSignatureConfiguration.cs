using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ClinicSignatureConfiguration : IEntityTypeConfiguration<ClinicSignature>
{
    public void Configure(EntityTypeBuilder<ClinicSignature> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SignerCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SignerName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.SignerTitle).IsRequired().HasMaxLength(255);
        builder.Property(x => x.SignatureImagePath).HasMaxLength(500);
        builder.Property(x => x.SignatureType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.ClinicSignatures)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClinicId, x.SignerCode }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
