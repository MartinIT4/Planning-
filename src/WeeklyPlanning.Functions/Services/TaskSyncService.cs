using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WeeklyPlanning.Domain.Entities;
using WeeklyPlanning.Domain.Interfaces;
using WeeklyPlanning.Functions.Models;

namespace WeeklyPlanning.Functions.Services;

/// <summary>
/// Orquesta la sincronización de tareas desde el sistema externo hacia el snapshot local.
/// Estrategia: upsert por ExternalTaskId + desactivar tareas ausentes.
/// El sistema externo nunca es modificado por este servicio.
/// </summary>
public class TaskSyncService
{
    private readonly ExternalApiClient _apiClient;
    private readonly IExternalTaskSnapshotRepository _snapshotRepository;
    private readonly ILogger<TaskSyncService> _logger;

    public TaskSyncService(
        ExternalApiClient apiClient,
        IExternalTaskSnapshotRepository snapshotRepository,
        ILogger<TaskSyncService> logger)
    {
        _apiClient = apiClient;
        _snapshotRepository = snapshotRepository;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el ciclo completo de sincronización:
    /// 1. Obtiene tareas de la API externa (solo lectura).
    /// 2. Crea o actualiza snapshots locales.
    /// 3. Desactiva los snapshots de tareas que ya no están en el sistema externo.
    /// </summary>
    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult();

        _logger.LogInformation("Starting task sync from external system...");

        try
        {
            // ── 1. Obtener tareas del sistema externo (solo lectura) ──────────
            var externalTasks = await _apiClient.GetTasksAsync(cancellationToken);
            result.TotalFromApi = externalTasks.Count;

            if (externalTasks.Count == 0)
            {
                _logger.LogWarning("External API returned 0 tasks. Aborting sync to avoid accidental data loss.");
                result.Success = false;
                result.ErrorMessage = "La API externa devolvió 0 tareas. Sync abortado para evitar pérdida de datos.";
                return result;
            }

            // ── 2. Cargar todos los snapshots actuales para comparación ──────
            var existingSnapshots = (await _snapshotRepository.GetAllAsync(cancellationToken))
                .ToDictionary(s => s.ExternalTaskId, StringComparer.OrdinalIgnoreCase);

            var externalIds = externalTasks.Select(t => t.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toCreate = new List<ExternalTaskSnapshot>();
            var toUpdate = new List<ExternalTaskSnapshot>();

            // ── 3. Upsert por ExternalTaskId ─────────────────────────────────
            foreach (var apiTask in externalTasks)
            {
                if (existingSnapshots.TryGetValue(apiTask.Id, out var existing))
                {
                    // Actualizar snapshot existente con datos frescos
                    existing.Update(
                        title: apiTask.Title,
                        description: apiTask.Description,
                        projectId: apiTask.Project?.Id,
                        projectName: apiTask.Project?.Name,
                        estimatedHours: apiTask.EstimatedHours,
                        loggedHours: apiTask.LoggedHours,
                        status: apiTask.Status,
                        assigneeExternalId: apiTask.Assignee?.Id,
                        assigneeName: apiTask.Assignee?.Name);

                    toUpdate.Add(existing);
                    result.Updated++;
                }
                else
                {
                    // Crear nuevo snapshot para tarea nueva en el sistema externo
                    var snapshot = ExternalTaskSnapshot.Create(
                        externalTaskId: apiTask.Id,
                        title: apiTask.Title,
                        description: apiTask.Description,
                        projectId: apiTask.Project?.Id,
                        projectName: apiTask.Project?.Name,
                        estimatedHours: apiTask.EstimatedHours,
                        loggedHours: apiTask.LoggedHours,
                        status: apiTask.Status,
                        assigneeExternalId: apiTask.Assignee?.Id,
                        assigneeName: apiTask.Assignee?.Name);

                    toCreate.Add(snapshot);
                    result.Created++;
                }
            }

            // ── 4. Desactivar tareas que ya no están en el sistema externo ───
            var toDeactivate = existingSnapshots.Values
                .Where(s => s.IsActive && !externalIds.Contains(s.ExternalTaskId))
                .ToList();

            foreach (var snapshot in toDeactivate)
            {
                snapshot.MarkInactive();
                result.Deactivated++;
            }

            // ── 5. Persistir todos los cambios en una sola unidad de trabajo ─
            if (toCreate.Count > 0)
                await _snapshotRepository.AddRangeAsync(toCreate, cancellationToken);

            if (toUpdate.Count > 0 || toDeactivate.Count > 0)
                await _snapshotRepository.UpdateRangeAsync(toUpdate.Concat(toDeactivate), cancellationToken);

            await _snapshotRepository.SaveChangesAsync(cancellationToken);

            sw.Stop();
            result.Success = true;
            result.Duration = sw.Elapsed;

            _logger.LogInformation(
                "Sync completed in {Duration}ms. Created: {Created}, Updated: {Updated}, Deactivated: {Deactivated}.",
                sw.ElapsedMilliseconds, result.Created, result.Updated, result.Deactivated);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = sw.Elapsed;
            _logger.LogError(ex, "Sync failed after {Duration}ms.", sw.ElapsedMilliseconds);
            throw;
        }

        return result;
    }
}
