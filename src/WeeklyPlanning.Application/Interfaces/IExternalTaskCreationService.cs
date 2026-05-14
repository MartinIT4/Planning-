using WeeklyPlanning.Application.DTOs;

namespace WeeklyPlanning.Application.Interfaces;

/// <summary>
/// Contrato para crear una tarea en el sistema externo de gestión de proyectos.
/// La implementación concreta reside en Infrastructure para aislar el acoplamiento HTTP.
/// </summary>
public interface IExternalTaskCreationService
{
    /// <summary>
    /// Crea una tarea en el sistema externo a partir de los datos de una tarea personal del PM.
    /// </summary>
    /// <returns>
    /// Siempre retorna un resultado (nunca lanza). El caller debe verificar <see cref="SendTaskToExternalResult.Success"/>.
    /// </returns>
    Task<SendTaskToExternalResult> SendAsync(
        SendTaskToExternalRequest request,
        CancellationToken cancellationToken = default);
}
