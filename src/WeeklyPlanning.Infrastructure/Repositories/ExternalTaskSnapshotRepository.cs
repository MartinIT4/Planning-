using Microsoft.EntityFrameworkCore;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;
using WeeklyPlanning.Infrastructure.Persistence;

namespace WeeklyPlanning.Infrastructure.Repositories;

public class ExternalTaskSnapshotRepository : IExternalTaskSnapshotRepository
{
    private readonly WeeklyPlanningDbContext _context;

    public ExternalTaskSnapshotRepository(WeeklyPlanningDbContext context)
    {
        _context = context;
    }

    public async Task<ExternalTaskSnapshot?> GetByExternalIdAsync(string externalTaskId, CancellationToken cancellationToken = default) =>
        await _context.ExternalTaskSnapshots
            .FirstOrDefaultAsync(s => s.ExternalTaskId == externalTaskId, cancellationToken);

    public async Task<ExternalTaskSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.ExternalTaskSnapshots
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IEnumerable<ExternalTaskSnapshot>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.ExternalTaskSnapshots
            .Where(s => s.IsActive)
            .OrderBy(s => s.ProjectName)
            .ThenBy(s => s.Title)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<ExternalTaskSnapshot>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.ExternalTaskSnapshots
            .OrderBy(s => s.ExternalTaskId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ExternalTaskSnapshot snapshot, CancellationToken cancellationToken = default) =>
        await _context.ExternalTaskSnapshots.AddAsync(snapshot, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<ExternalTaskSnapshot> snapshots, CancellationToken cancellationToken = default) =>
        await _context.ExternalTaskSnapshots.AddRangeAsync(snapshots, cancellationToken);

    public Task UpdateRangeAsync(IEnumerable<ExternalTaskSnapshot> snapshots, CancellationToken cancellationToken = default)
    {
        _context.ExternalTaskSnapshots.UpdateRange(snapshots);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
