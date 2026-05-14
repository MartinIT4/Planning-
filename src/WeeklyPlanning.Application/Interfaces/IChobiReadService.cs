using WeeklyPlanning.Application.DTOs;

namespace WeeklyPlanning.Application.Interfaces;

public interface IChobiReadService
{
    Task<IReadOnlyList<ChobiUserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ChobiProjectDto>> GetProjectsAsync(CancellationToken ct = default);
}
