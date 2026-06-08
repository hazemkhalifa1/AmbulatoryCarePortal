using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.NameAr)
            .HasMaxLength(255);

        builder.Property(x => x.Frequency)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.ChecklistTemplates)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.ChecklistTemplates)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.ChecklistTemplate)
            .HasForeignKey(x => x.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Rounds)
            .WithOne(x => x.ChecklistTemplate)
            .HasForeignKey(x => x.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
