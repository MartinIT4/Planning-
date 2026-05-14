namespace WeeklyPlanning.Application.DTOs;

/// <summary>
/// Datos de la tarea personal del PM que se desea crear en el sistema externo.
/// Solo se mapean los campos mínimos necesarios.
/// </summary>
public record SendTaskToExternalRequest(
    /// <summary>ID del ítem en el plan personal (para actualizar su estado tras el envío).</summary>
    Guid PersonalPlanItemId,

    string Title,
    string? Description,

    /// <summary>Estimación de horas. Opcional: el sistema externo puede tener su propio default.</summary>
    decimal? EstimatedHours,
    int AssigneeChobiUserId,
    int? ChobiProjectId,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    int CreatorChobiUserId
);

/// <summary>
/// Resultado de la operación de creación en el sistema externo.
/// No lanza excepción: el caller decide cómo manejar el fallo.
/// </summary>
public record SendTaskToExternalResult(
    bool Success,

    /// <summary>ID asignado por el sistema externo al crear la tarea. Null si falló.</summary>
    string? ExternalTaskId,

    /// <summary>URL de la tarea en el sistema externo. Null si falló o no aplica.</summary>
    string? ExternalTaskUrl,

    /// <summary>Mensaje de error descriptivo. Null si fue exitoso.</summary>
    string? ErrorMessage,

    /// <summary>Código HTTP asociado al resultado. Null si no aplica.</summary>
    int? HttpStatusCode,

    /// <summary>Indica si el fallo provino de Chrobi/red o de una validación local.</summary>
    bool IsExternalError
)
{
    /// <summary>Resultado de éxito.</summary>
    public static SendTaskToExternalResult Ok(string externalTaskId, string? externalTaskUrl = null) =>
        new(true, externalTaskId, externalTaskUrl, null, null, false);

    /// <summary>Resultado de fallo tipado.</summary>
    public static SendTaskToExternalResult Fail(string errorMessage, int? statusCode = null, bool isExternalError = false) =>
        new(false, null, null, errorMessage, statusCode, isExternalError);
}

public record PublishPersonalPlanToExternalResult(
    bool Success,
    string? ErrorMessage,
    IReadOnlyList<PersonalPlanItemExternalPublishResult> Items
);

public record PersonalPlanItemExternalPublishResult(
    Guid PersonalPlanItemId,
    string Title,
    bool Success,
    string? ExternalTaskId,
    string? ExternalTaskUrl,
    string? ErrorMessage,
    int? HttpStatusCode
);
