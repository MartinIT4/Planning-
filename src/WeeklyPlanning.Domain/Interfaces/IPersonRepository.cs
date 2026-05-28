using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Domain.Interfaces;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Person?> GetByEmailAsync(string email, string ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> GetAllActiveAsync(string ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Person person, CancellationToken cancellationToken = default);
    Task UpdateAsync(Person person, CancellationToken cancellationToken = default);
}
