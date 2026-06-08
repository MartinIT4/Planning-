using WeeklyPlanning.Domain.Enums;
using WeeklyPlanning.Domain.Exceptions;

namespace WeeklyPlanning.Domain.Entities;

public class WeeklyPlan
{
    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = default!;

    /// <summary>Lunes de la semana planificada.</summary>
    public DateOnly WeekStartDate { get; private set; }

    /// <summary>Viernes de la semana planificada.</summary>
    public DateOnly WeekEndDate { get; private set; }

    public string? Notes { get; private set; }
    public WeeklyPlanStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private List<TaskAssignment> _assignments = new();
    public IReadOnlyCollection<TaskAssignment> Assignments => _assignments.AsReadOnly();

    private WeeklyPlan() { }

    public static WeeklyPlan Create(string ownerId, DateOnly weekStartDate, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new DomainException("El owner del plan es requerido.");
        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new DomainException("La fecha de inicio del plan debe ser un lunes.");

        return new WeeklyPlan
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId.Trim().ToLowerInvariant(),
            WeekStartDate = weekStartDate,
            WeekEndDate = weekStartDate.AddDays(4),
            Notes = notes?.Trim(),
            Status = WeeklyPlanStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (Status != WeeklyPlanStatus.Draft)
            throw new DomainException("Solo se puede confirmar un plan en estado Draft.");
        Status = WeeklyPlanStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status == WeeklyPlanStatus.Closed)
            throw new DomainException("El plan ya está cerrado.");
        Status = WeeklyPlanStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevertToDraft()
    {
        if (Status == WeeklyPlanStatus.Closed)
            throw new DomainException("No se puede reabrir un plan cerrado.");
        Status = WeeklyPlanStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
    }

    public TaskAssignment AddAssignment(
        Guid personId,
        string externalTaskId,
        string taskTitle,
        decimal plannedHours,
        string? notes = null)
    {
        if (Status == WeeklyPlanStatus.Closed)
            throw new DomainException("No se pueden agregar asignaciones a un plan cerrado.");

        var assignment = TaskAssignment.Create(Id, personId, externalTaskId, taskTitle, plannedHours, notes);
        _assignments.Add(assignment);
        UpdatedAt = DateTime.UtcNow;
        return assignment;
    }

    public void RemoveAssignment(Guid assignmentId)
    {
        if (Status == WeeklyPlanStatus.Closed)
            throw new DomainException("No se pueden eliminar asignaciones de un plan cerrado.");

        var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new DomainException($"No se encontró la asignación con id '{assignmentId}'.");

        _assignments.Remove(assignment);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula las horas planificadas totales por persona para validación de capacidad.
    /// </summary>
    public Dictionary<Guid, decimal> GetHoursByPerson() =>
        _assignments
            .GroupBy(a => a.PersonId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.PlannedHours));
}
