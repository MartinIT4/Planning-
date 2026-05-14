using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Requests;

namespace WeeklyPlanning.Application.Interfaces;

public interface IPersonService
{
    Task<PersonDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PersonDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<PersonDto> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default);
    Task<PersonDto> UpdateAsync(Guid id, UpdatePersonRequest request, CancellationToken cancellationToken = default);
    Task<PersonDto> SetChobiUserIdAsync(Guid id, int chobiUserId, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
