using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ClinicTemplateValueConfiguration : IEntityTypeConfiguration<ClinicTemplateValue>
{
    public void Configure(EntityTypeBuilder<ClinicTemplateValue> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Value).HasMaxLength(4000);
        builder.Property(x => x.ImagePath).HasMaxLength(500);

        builder.HasOne(x => x.ClinicTemplateAssignment)
            .WithMany(x => x.VariableValues)
            .HasForeignKey(x => x.ClinicTemplateAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TemplateVariable)
            .WithMany(x => x.ClinicValues)
            .HasForeignKey(x => x.TemplateVariableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClinicTemplateAssignmentId, x.TemplateVariableId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
