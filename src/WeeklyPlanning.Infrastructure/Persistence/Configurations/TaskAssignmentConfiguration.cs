using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Infrastructure.Persistence.Configurations;

public class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
{
    public void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        builder.ToTable("TaskAssignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalTaskId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.TaskTitle)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.PlannedHours)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.SentToExternalAt);
        builder.Property(x => x.ExternalCreatedTaskId).HasMaxLength(200);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.WeeklyPlanId, x.PersonId });

        builder.HasOne(x => x.Person)
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
