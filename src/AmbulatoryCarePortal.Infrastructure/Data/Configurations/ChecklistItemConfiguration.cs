using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuestionText)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.QuestionTextAr)
            .HasMaxLength(500);

        builder.Property(x => x.ItemOrder)
            .IsRequired();

        builder.Property(x => x.Weight)
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasOne(x => x.ChecklistTemplate)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Answers)
            .WithOne(x => x.ChecklistItem)
            .HasForeignKey(x => x.ChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
