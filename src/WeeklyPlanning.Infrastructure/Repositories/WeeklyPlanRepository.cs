using Microsoft.EntityFrameworkCore;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;
using WeeklyPlanning.Infrastructure.Persistence;

namespace WeeklyPlanning.Infrastructure.Repositories;

public class WeeklyPlanRepository : IWeeklyPlanRepository
{
    private readonly WeeklyPlanningDbContext _context;

    public WeeklyPlanRepository(WeeklyPlanningDbContext context)
    {
        _context = context;
    }

    public async Task<WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.WeeklyPlans
            .Include(p => p.Assignments)
                .ThenInclude(a => a.Person)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<WeeklyPlan?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default) =>
        await _context.WeeklyPlans
            .Include(p => p.Assignments)
                .ThenInclude(a => a.Person)
            .FirstOrDefaultAsync(p => p.WeekStartDate == weekStartDate, cancellationToken);

    public async Task<IEnumerable<WeeklyPlan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.WeeklyPlans
            .Include(p => p.Assignments)
                .ThenInclude(a => a.Person)
            .OrderByDescending(p => p.WeekStartDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken = default)
    {
        await _context.WeeklyPlans.AddAsync(weeklyPlan, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken = default)
    {
        // Disable auto-detect to avoid cascading side effects when querying/adding entities.
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            // Snapshot of DB-loaded assignments (before any changes).
            var dbAssignmentIds = _context.ChangeTracker
                .Entries<TaskAssignment>()
                .Select(e => e.Entity.Id)
                .ToHashSet();

            // Explicitly add new assignments to the context as Added.
            // (DetectChanges would add them as Modified instead, causing a concurrency error.)
            foreach (var assignment in weeklyPlan.Assignments)
            {
                if (!dbAssignmentIds.Contains(assignment.Id))
                    _context.Set<TaskAssignment>().Add(assignment);
            }

            // Detect scalar-property changes (e.g. UpdatedAt) and removed assignments.
            // Removed items are automatically marked as Deleted via the navigation snapshot diff.
            _context.ChangeTracker.DetectChanges();

            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
