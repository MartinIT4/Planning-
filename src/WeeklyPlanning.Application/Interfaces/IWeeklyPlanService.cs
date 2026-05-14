using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Requests;

namespace WeeklyPlanning.Application.Interfaces;

public interface IWeeklyPlanService
{
    Task<WeeklyPlanDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WeeklyPlanDto?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<WeeklyPlanDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WeeklyPlanDto> CreateAsync(CreateWeeklyPlanRequest request, CancellationToken cancellationToken = default);
    Task<WeeklyPlanDto> UpdateAsync(Guid id, UpdateWeeklyPlanRequest request, CancellationToken cancellationToken = default);
    Task<WeeklyPlanDto> UpdateStatusAsync(Guid id, UpdateWeeklyPlanStatusRequest request, CancellationToken cancellationToken = default);
    Task<TaskAssignmentDto> AddAssignmentAsync(Guid weeklyPlanId, AddTaskAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<TaskAssignmentDto> UpdateAssignmentAsync(Guid weeklyPlanId, Guid assignmentId, UpdateTaskAssignmentRequest request, CancellationToken cancellationToken = default);
    Task RemoveAssignmentAsync(Guid weeklyPlanId, Guid assignmentId, CancellationToken cancellationToken = default);
    Task<CapacitySummaryDto> GetCapacitySummaryAsync(Guid weeklyPlanId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskAssignmentDto>> CopyAssignmentsAsync(Guid targetPlanId, IEnumerable<Guid> sourceAssignmentIds, CancellationToken cancellationToken = default);
    Task<SendTaskToExternalResult> SendAssignmentToExternalAsync(Guid weeklyPlanId, Guid assignmentId, CancellationToken cancellationToken = default);
}
