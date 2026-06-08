using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class HrStaffConfiguration : IEntityTypeConfiguration<HrStaff>
{
    public void Configure(EntityTypeBuilder<HrStaff> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullNameEn)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.FullNameAr)
            .HasMaxLength(255);

        builder.Property(x => x.NationalId)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(255);

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.HrStaff)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Staff)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.HrStaff)
            .HasForeignKey(x => x.HrStaffId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
