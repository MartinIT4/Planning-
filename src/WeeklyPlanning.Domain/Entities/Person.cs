using WeeklyPlanning.Domain.Exceptions;

namespace WeeklyPlanning.Domain.Entities;

public class Person
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public decimal WeeklyCapacityHours { get; private set; }
    public int? ChobiUserId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Person() { }

    public static Person Create(string name, string? email, decimal weeklyCapacityHours = 40)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la persona es requerido.");
        if (weeklyCapacityHours <= 0 || weeklyCapacityHours > 60)
            throw new DomainException("La capacidad semanal debe estar entre 1 y 60 horas.");

        var resolvedEmail = string.IsNullOrWhiteSpace(email)
            ? $"noreply-{Guid.NewGuid():N}@local"
            : email.Trim().ToLowerInvariant();

        return new Person
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = resolvedEmail,
            WeeklyCapacityHours = weeklyCapacityHours,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? email, decimal weeklyCapacityHours)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la persona es requerido.");
        if (weeklyCapacityHours <= 0 || weeklyCapacityHours > 60)
            throw new DomainException("La capacidad semanal debe estar entre 1 y 60 horas.");

        Name = name.Trim();
        // Only update email if provided; keep existing otherwise
        if (!string.IsNullOrWhiteSpace(email))
            Email = email.Trim().ToLowerInvariant();
        WeeklyCapacityHours = weeklyCapacityHours;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetChobiUserId(int chobiUserId)
    {
        ChobiUserId = chobiUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
