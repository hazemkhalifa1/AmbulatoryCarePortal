using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.NameAr)
            .HasMaxLength(255);

        builder.Property(x => x.CityEn)
            .HasMaxLength(100);

        builder.Property(x => x.CityAr)
            .HasMaxLength(100);

        builder.Property(x => x.ClinicType)
            .IsRequired();

        builder.Property(x => x.LogoPath)
            .HasMaxLength(500);

        builder.Property(x => x.LicenseNumber)
            .HasMaxLength(100);

        builder.Property(x => x.ComplianceScore)
            .HasPrecision(5, 2);

        // Relationships
        builder.HasMany(x => x.Users)
            .WithOne(x => x.Clinic)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Departments)
            .WithOne(x => x.Clinic)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PolicyDocuments)
            .WithOne(x => x.Clinic)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.KPIs)
            .WithOne(x => x.Clinic)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.LicenseNumber).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
