namespace WeeklyPlanning.Functions.Models;

/// <summary>
/// Respuesta paginada de GET /api/tasks de Chrobi.
/// Campo "items" contiene las tareas; "totalCount" el total para paginación.
/// Si Chrobi usa nombre diferente, ajustar las propiedades con [JsonPropertyName].
/// </summary>
public class ExternalApiTaskListResponse
{
    /// <summary>Lista de tareas en la página actual.</summary>
    public List<ExternalApiTask> Items { get; set; } = new();

    /// <summary>Alias por si el campo se llama "tasks" en lugar de "items".</summary>
    public List<ExternalApiTask>? Tasks
    {
        get => Items;
        set { if (value is not null) Items = value; }
    }

    /// <summary>Total de tareas (todas las páginas).</summary>
    public int TotalCount { get; set; }

    /// <summary>Alias por si el campo se llama "total" en lugar de "totalCount".</summary>
    public int Total
    {
        get => TotalCount;
        set { if (TotalCount == 0) TotalCount = value; }
    }
}

/// <summary>
/// Tarea tal como la devuelve la API de Chrobi (GET /api/tasks).
/// Solo lectura: nunca se escribe de vuelta al sistema externo desde este cliente.
/// </summary>
public class ExternalApiTask
{
    /// <summary>Identificador único en Chrobi.</summary>
    public string Id { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>Proyecto al que pertenece la tarea.</summary>
    public ExternalApiProject? Project { get; set; }

    /// <summary>Horas estimadas en Chrobi.</summary>
    public decimal EstimatedHours { get; set; }

    /// <summary>Horas ya registradas en Chrobi (fuente de verdad, solo lectura).</summary>
    public decimal LoggedHours { get; set; }

    /// <summary>Estado de la tarea en Chrobi: "todo" | "in_progress" | "done" | "blocked".</summary>
    public string Status { get; set; } = "todo";

    public ExternalApiAssignee? Assignee { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ExternalApiProject
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class ExternalApiAssignee
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
}

/// <summary>
/// Resultado de una ejecución del job de sincronización.
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Deactivated { get; set; }
    public int TotalFromApi { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}
