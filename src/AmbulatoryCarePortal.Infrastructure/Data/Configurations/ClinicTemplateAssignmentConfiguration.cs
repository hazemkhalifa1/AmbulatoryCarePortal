using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ClinicTemplateAssignmentConfiguration : IEntityTypeConfiguration<ClinicTemplateAssignment>
{
    public void Configure(EntityTypeBuilder<ClinicTemplateAssignment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssignmentStatus).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.ClinicTemplateAssignments)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DocumentTemplate)
            .WithMany(x => x.ClinicAssignments)
            .HasForeignKey(x => x.DocumentTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.VariableValues)
            .WithOne(x => x.ClinicTemplateAssignment)
            .HasForeignKey(x => x.ClinicTemplateAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.GeneratedDocuments)
            .WithOne(x => x.ClinicTemplateAssignment)
            .HasForeignKey(x => x.ClinicTemplateAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClinicId, x.DocumentTemplateId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
