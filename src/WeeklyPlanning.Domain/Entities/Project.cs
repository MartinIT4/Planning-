using WeeklyPlanning.Domain.Exceptions;

namespace WeeklyPlanning.Domain.Entities;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int? ChobiProjectId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsBillable { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Project() { }

    public static Project Create(string name, string? description = null, bool isBillable = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del proyecto es requerido.");

        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = true,
            IsBillable = isBillable,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description = null, bool isBillable = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del proyecto es requerido.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsBillable = isBillable;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetChobiProjectId(int chobiProjectId)
    {
        ChobiProjectId = chobiProjectId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
