using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Infrastructure.Data.Configurations;

public class ChecklistRoundConfiguration : IEntityTypeConfiguration<ChecklistRound>
{
    public void Configure(EntityTypeBuilder<ChecklistRound> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ChecklistTemplate)
            .WithMany(x => x.Rounds)
            .HasForeignKey(x => x.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Clinic)
            .WithMany(x => x.ChecklistRounds)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.ChecklistRounds)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ExecutedByUser)
            .WithMany(x => x.ChecklistRounds)
            .HasForeignKey(x => x.ExecutedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Answers)
            .WithOne(x => x.ChecklistRound)
            .HasForeignKey(x => x.ChecklistRoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClinicId, x.ChecklistTemplateId, x.ExecutedAt })
            .IsDescending(false, false, true)
            .HasFilter("[IsDeleted] = 0");
    }
}
