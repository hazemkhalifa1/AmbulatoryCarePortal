using AmbulatoryCarePortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class DocumentTemplateVersionConfiguration : IEntityTypeConfiguration<DocumentTemplateVersion>
{
    public void Configure(EntityTypeBuilder<DocumentTemplateVersion> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(500);
        builder.Property(x => x.ChangeLog).HasMaxLength(2000);

        builder.HasOne(x => x.DocumentTemplate)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.DocumentTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DocumentTemplateId, x.VersionNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
