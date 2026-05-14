using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeeklyPlanning.Functions.Models;
using WeeklyPlanning.Functions.Services;

namespace WeeklyPlanning.Functions.Functions;

/// <summary>
/// Trigger HTTP para sincronización manual on-demand.
/// 
/// POST /api/sync-tasks
/// Responde con el resultado de la sincronización en JSON.
/// 
/// Útil para:
/// - Forzar una sincronización inmediata antes de una sesión de planificación.
/// - Debugging y verificación manual.
/// </summary>
public class TaskSyncHttpFunction
{
    private readonly TaskSyncService _syncService;
    private readonly ILogger<TaskSyncHttpFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public TaskSyncHttpFunction(TaskSyncService syncService, ILogger<TaskSyncHttpFunction> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [Function(nameof(TaskSyncHttpFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync-tasks")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual sync triggered via HTTP by {Caller}.", req.Url);

        SyncResult result;
        try
        {
            result = await _syncService.SyncAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual sync failed.");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new
            {
                success = false,
                error = "Sync failed. See logs for details.",
                message = ex.Message
            }, JsonOptions), cancellationToken);
            return errorResponse;
        }

        var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");

        await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptions), cancellationToken);
        return response;
    }
}
