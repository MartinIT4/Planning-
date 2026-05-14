using Microsoft.EntityFrameworkCore;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;
using WeeklyPlanning.Infrastructure.Persistence;

namespace WeeklyPlanning.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly WeeklyPlanningDbContext _context;

    public ProjectRepository(WeeklyPlanningDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Projects.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IEnumerable<Project>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.Projects
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
