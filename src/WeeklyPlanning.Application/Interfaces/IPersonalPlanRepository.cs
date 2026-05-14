using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Application.Interfaces;

public interface IPersonalPlanRepository
{
    Task<PersonalWeeklyPlan?> GetByOwnerAndWeekAsync(string ownerId, DateOnly weekStartDate, CancellationToken ct = default);
    Task<PersonalWeeklyPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PersonalWeeklyPlan> AddAsync(PersonalWeeklyPlan plan, CancellationToken ct = default);
    Task UpdateAsync(PersonalWeeklyPlan plan, CancellationToken ct = default);
}
