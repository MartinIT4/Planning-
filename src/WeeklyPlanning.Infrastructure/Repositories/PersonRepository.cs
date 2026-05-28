using Microsoft.EntityFrameworkCore;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;
using WeeklyPlanning.Infrastructure.Persistence;

namespace WeeklyPlanning.Infrastructure.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly WeeklyPlanningDbContext _context;

    public PersonRepository(WeeklyPlanningDbContext context)
    {
        _context = context;
    }

    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Persons.FindAsync(new object[] { id }, cancellationToken);

    public async Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Persons
            .FirstOrDefaultAsync(p => p.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<Person?> GetByEmailAsync(string email, string ownerId, CancellationToken cancellationToken = default) =>
        await _context.Persons
            .FirstOrDefaultAsync(
                p => p.Email == email.ToLowerInvariant() && p.OwnerId == ownerId.ToLowerInvariant(),
                cancellationToken);

    public async Task<IEnumerable<Person>> GetAllActiveAsync(string ownerId, CancellationToken cancellationToken = default) =>
        await _context.Persons
            .Where(p => p.IsActive && p.OwnerId == ownerId.ToLowerInvariant())
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        await _context.Persons.AddAsync(person, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Person person, CancellationToken cancellationToken = default)
    {
        _context.Persons.Update(person);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
