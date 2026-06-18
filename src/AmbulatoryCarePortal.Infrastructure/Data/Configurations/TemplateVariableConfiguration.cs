using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class TemplateVariableConfiguration : IEntityTypeConfiguration<TemplateVariable>
{
    public void Configure(EntityTypeBuilder<TemplateVariable> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DefaultValue).HasMaxLength(2000);

        builder.HasOne(x => x.DocumentTemplate)
            .WithMany(x => x.TemplateVariables)
            .HasForeignKey(x => x.DocumentTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DocumentTemplateId, x.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
