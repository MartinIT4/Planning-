namespace WeeklyPlanning.Domain.Entities;

/// <summary>
/// Snapshot local de una tarea proveniente del sistema externo.
/// Solo lectura: este sistema NUNCA modifica datos en el origen.
/// Se utiliza para planificación tentativa sin reemplazar la fuente de verdad.
/// </summary>
public class ExternalTaskSnapshot
{
    public Guid Id { get; private set; }

    /// <summary>ID de la tarea en el sistema externo. Inmutable.</summary>
    public string ExternalTaskId { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? ProjectId { get; private set; }
    public string? ProjectName { get; private set; }

    /// <summary>Estimación de horas cargada en el sistema externo.</summary>
    public decimal EstimatedHours { get; private set; }

    /// <summary>Horas ya imputadas en el sistema externo (solo referencia para planificación).</summary>
    public decimal LoggedHours { get; private set; }

    /// <summary>Horas restantes estimadas = EstimatedHours - LoggedHours.</summary>
    public decimal RemainingHours => Math.Max(0, EstimatedHours - LoggedHours);

    /// <summary>Estado de la tarea en el sistema externo (ej: "todo", "in_progress", "done").</summary>
    public string Status { get; private set; } = default!;

    public string? AssigneeExternalId { get; private set; }    public string? AssigneeName { get; private set; }

    /// <summary>Última vez que se sincronizó este snapshot desde el sistema externo.</summary>
    public DateTime LastSyncedAt { get; private set; }

    /// <summary>False si la tarea ya no apareció en la última sincronización.</summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ExternalTaskSnapshot() { }

    public static ExternalTaskSnapshot Create(
        string externalTaskId,
        string title,
        string? description,
        string? projectId,
        string? projectName,
        decimal estimatedHours,
        decimal loggedHours,
        string status,
        string? assigneeExternalId,
        string? assigneeName)
    {
        return new ExternalTaskSnapshot
        {
            Id = Guid.NewGuid(),
            ExternalTaskId = externalTaskId,
            Title = title,
            Description = description,
            ProjectId = projectId,
            ProjectName = projectName,
            EstimatedHours = Math.Max(0, estimatedHours),
            LoggedHours = Math.Max(0, loggedHours),
            Status = status,
            AssigneeExternalId = assigneeExternalId,
            AssigneeName = assigneeName,
            LastSyncedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Actualiza el snapshot con los datos más recientes del sistema externo.</summary>
    public void Update(
        string title,
        string? description,
        string? projectId,
        string? projectName,
        decimal estimatedHours,
        decimal loggedHours,
        string status,
        string? assigneeExternalId,
        string? assigneeName)
    {
        Title = title;
        Description = description;
        ProjectId = projectId;
        ProjectName = projectName;
        EstimatedHours = Math.Max(0, estimatedHours);
        LoggedHours = Math.Max(0, loggedHours);
        Status = status;
        AssigneeExternalId = assigneeExternalId;
        AssigneeName = assigneeName;
        LastSyncedAt = DateTime.UtcNow;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Marca la tarea como inactiva cuando ya no aparece en el sistema externo.</summary>
    public void MarkInactive()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
