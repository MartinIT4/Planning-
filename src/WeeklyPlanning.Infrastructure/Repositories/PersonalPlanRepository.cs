using Microsoft.EntityFrameworkCore;
using WeeklyPlanning.Application.Interfaces;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Infrastructure.Persistence;

namespace WeeklyPlanning.Infrastructure.Repositories;

public class PersonalPlanRepository : IPersonalPlanRepository
{
    private readonly WeeklyPlanningDbContext _context;

    public PersonalPlanRepository(WeeklyPlanningDbContext context) => _context = context;

    public async Task<PersonalWeeklyPlan?> GetByOwnerAndWeekAsync(string ownerId, DateOnly weekStartDate, CancellationToken ct = default)
        => await _context.PersonalWeeklyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.OwnerId == ownerId && p.WeekStartDate == weekStartDate, ct);

    public async Task<PersonalWeeklyPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.PersonalWeeklyPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PersonalWeeklyPlan> AddAsync(PersonalWeeklyPlan plan, CancellationToken ct = default)
    {
        _context.PersonalWeeklyPlans.Add(plan);
        await _context.SaveChangesAsync(ct);
        return plan;
    }

    public async Task UpdateAsync(PersonalWeeklyPlan plan, CancellationToken ct = default)
    {
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var dbIds = _context.ChangeTracker
                .Entries<PersonalPlanItem>()
                .Select(e => e.Entity.Id)
                .ToHashSet();

            foreach (var item in plan.Items)
                if (!dbIds.Contains(item.Id))
                    _context.Set<PersonalPlanItem>().Add(item);

            _context.ChangeTracker.DetectChanges();
            await _context.SaveChangesAsync(ct);
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
