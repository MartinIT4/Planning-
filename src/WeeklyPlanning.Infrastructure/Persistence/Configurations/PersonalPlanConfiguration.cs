using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Infrastructure.Persistence.Configurations;

public class PersonalPlanConfiguration : IEntityTypeConfiguration<PersonalWeeklyPlan>
{
    public void Configure(EntityTypeBuilder<PersonalWeeklyPlan> builder)
    {
        builder.ToTable("PersonalWeeklyPlans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.WeekStartDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.WeekEndDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.OwnerId, x.WeekStartDate }).IsUnique();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.PersonalWeeklyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items).HasField("_items");
    }
}

public class PersonalPlanItemConfiguration : IEntityTypeConfiguration<PersonalPlanItem>
{
    public void Configure(EntityTypeBuilder<PersonalPlanItem> builder)
    {
        builder.ToTable("PersonalPlanItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ExternalTaskId).HasMaxLength(100);
        builder.Property(x => x.ChobiProjectId);
        builder.Property(x => x.ExternalTaskUrl).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.EstimatedHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
