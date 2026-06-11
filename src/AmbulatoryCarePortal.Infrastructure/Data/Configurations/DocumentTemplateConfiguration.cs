using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class DocumentTemplateConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StandardCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TitleEn).IsRequired().HasMaxLength(255);
        builder.Property(x => x.TitleAr).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DepartmentCategory).HasMaxLength(100);
        builder.Property(x => x.ClinicType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
        builder.Property(x => x.TemplateFilePath).HasMaxLength(500);

        builder.HasIndex(x => x.StandardCode).IsUnique();
    }
}
