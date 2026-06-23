using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class TemplateSignerConfiguration : IEntityTypeConfiguration<TemplateSigner>
{
    public void Configure(EntityTypeBuilder<TemplateSigner> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SignerCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SignerDisplayName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.SignerTitle).IsRequired().HasMaxLength(255);

        builder.HasOne(x => x.DocumentTemplate)
            .WithMany(x => x.TemplateSigners)
            .HasForeignKey(x => x.DocumentTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DocumentTemplateId, x.SignerCode }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
