using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ChecklistAnswerConfiguration : IEntityTypeConfiguration<ChecklistAnswer>
{
    public void Configure(EntityTypeBuilder<ChecklistAnswer> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnswerValue)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.EvidenceFilePath)
            .HasMaxLength(500);

        builder.HasOne(x => x.ChecklistRound)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.ChecklistRoundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChecklistItem)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.ChecklistItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
