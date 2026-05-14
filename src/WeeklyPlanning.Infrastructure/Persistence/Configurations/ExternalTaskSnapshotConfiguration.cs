using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Infrastructure.Persistence.Configurations;

public class ExternalTaskSnapshotConfiguration : IEntityTypeConfiguration<ExternalTaskSnapshot>
{
    public void Configure(EntityTypeBuilder<ExternalTaskSnapshot> builder)
    {
        builder.ToTable("ExternalTaskSnapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalTaskId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.ProjectId)
            .HasMaxLength(100);

        builder.Property(x => x.ProjectName)
            .HasMaxLength(200);

        builder.Property(x => x.EstimatedHours)
            .HasColumnType("decimal(7,2)");

        builder.Property(x => x.LoggedHours)
            .HasColumnType("decimal(7,2)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AssigneeExternalId)
            .HasMaxLength(100);

        builder.Property(x => x.AssigneeName)
            .HasMaxLength(200);

        builder.Property(x => x.LastSyncedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // RemainingHours es calculado, no se persiste
        builder.Ignore(x => x.RemainingHours);

        builder.HasIndex(x => x.ExternalTaskId).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.LastSyncedAt);
    }
}
