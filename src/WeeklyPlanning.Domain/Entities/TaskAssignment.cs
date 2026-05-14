using WeeklyPlanning.Domain.Exceptions;

namespace WeeklyPlanning.Domain.Entities;

/// <summary>
/// Representa la asignación tentativa de una tarea (de sistema externo) a una persona en una semana.
/// No implica imputación de horas reales.
/// </summary>
public class TaskAssignment
{
    public Guid Id { get; private set; }
    public Guid WeeklyPlanId { get; private set; }
    public Guid PersonId { get; private set; }

    /// <summary>Identificador de la tarea en el sistema externo. Solo referencia, no se modifica.</summary>
    public string ExternalTaskId { get; private set; } = default!;

    /// <summary>Título de la tarea cacheado localmente para visualización (no fuente de verdad).</summary>
    public string TaskTitle { get; private set; } = default!;

    /// <summary>Horas planificadas tentativas. No son horas imputadas.</summary>
    public decimal PlannedHours { get; private set; }

    public string? Notes { get; private set; }
    public DateTime? SentToExternalAt { get; private set; }
    public string? ExternalCreatedTaskId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Person Person { get; private set; } = default!;
    public WeeklyPlan WeeklyPlan { get; private set; } = default!;

    private TaskAssignment() { }

    public static TaskAssignment Create(
        Guid weeklyPlanId,
        Guid personId,
        string externalTaskId,
        string taskTitle,
        decimal plannedHours,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(externalTaskId))
            throw new DomainException("El identificador de tarea externa es requerido.");
        if (string.IsNullOrWhiteSpace(taskTitle))
            throw new DomainException("El título de la tarea es requerido.");
        if (plannedHours <= 0 || plannedHours > 40)
            throw new DomainException("Las horas planificadas deben estar entre 0.5 y 40.");

        return new TaskAssignment
        {
            Id = Guid.NewGuid(),
            WeeklyPlanId = weeklyPlanId,
            PersonId = personId,
            ExternalTaskId = externalTaskId.Trim(),
            TaskTitle = taskTitle.Trim(),
            PlannedHours = plannedHours,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string taskTitle, decimal plannedHours, string? notes)
    {
        if (string.IsNullOrWhiteSpace(taskTitle))
            throw new DomainException("El título de la tarea es requerido.");
        if (plannedHours <= 0 || plannedHours > 40)
            throw new DomainException("Las horas planificadas deben estar entre 0.5 y 40.");

        TaskTitle = taskTitle.Trim();
        PlannedHours = plannedHours;
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSent(string externalCreatedTaskId)
    {
        ExternalCreatedTaskId = externalCreatedTaskId;
        SentToExternalAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
