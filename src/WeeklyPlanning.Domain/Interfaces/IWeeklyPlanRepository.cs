using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Domain.Interfaces;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WeeklyPlan?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<WeeklyPlan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken = default);
    Task UpdateAsync(WeeklyPlan weeklyPlan, CancellationToken cancellationToken = default);
}
