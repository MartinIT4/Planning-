using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Enums;

namespace WeeklyPlanning.Infrastructure.Persistence.Configurations;

public class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlan>
{
    public void Configure(EntityTypeBuilder<WeeklyPlan> builder)
    {
        builder.ToTable("WeeklyPlans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WeekStartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.WeekEndDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.OwnerId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.OwnerId, x.WeekStartDate }).IsUnique();

        builder.HasMany(x => x.Assignments)
            .WithOne(x => x.WeeklyPlan)
            .HasForeignKey(x => x.WeeklyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
