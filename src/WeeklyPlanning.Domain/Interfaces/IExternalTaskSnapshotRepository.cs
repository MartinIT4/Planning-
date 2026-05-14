using WeeklyPlanning.Domain.Entities;

namespace WeeklyPlanning.Domain.Interfaces;

public interface IExternalTaskSnapshotRepository
{
    Task<ExternalTaskSnapshot?> GetByExternalIdAsync(string externalTaskId, CancellationToken cancellationToken = default);
    Task<ExternalTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExternalTaskSnapshot>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ExternalTaskSnapshot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ExternalTaskSnapshot snapshot, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ExternalTaskSnapshot> snapshots, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<ExternalTaskSnapshot> snapshots, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
