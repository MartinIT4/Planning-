using Microsoft.EntityFrameworkCore;
using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Infrastructure.Persistence;

public class WeeklyPlanningDbContext : DbContext
{
    public WeeklyPlanningDbContext(DbContextOptions<WeeklyPlanningDbContext> options)
        : base(options) { }

    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<ExternalTaskSnapshot> ExternalTaskSnapshots => Set<ExternalTaskSnapshot>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<PersonalWeeklyPlan> PersonalWeeklyPlans => Set<PersonalWeeklyPlan>();
    public DbSet<PersonalPlanItem> PersonalPlanItems => Set<PersonalPlanItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeeklyPlanningDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
