using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Interfaces;

namespace WeeklyPlanning.Infrastructure.ExternalSystem;

public class ExternalTaskCreationService : IExternalTaskCreationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalTaskCreationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ExternalTaskCreationService(
        HttpClient httpClient,
        ILogger<ExternalTaskCreationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SendTaskToExternalResult> SendAsync(
        SendTaskToExternalRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending task to external system. PersonalPlanItemId={ItemId}, Title={Title}",
            request.PersonalPlanItemId, request.Title);

        var payload = MapToExternalPayload(request);

        HttpResponseMessage? response = null;
        try
        {
            response = await ExecuteWithOneRetryAsync(
                () => _httpClient.PostAsJsonAsync("tasks", payload, JsonOptions, cancellationToken),
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger.LogWarning(ex,
                "Network error sending task {ItemId} to external system.",
                request.PersonalPlanItemId);
            return SendTaskToExternalResult.Fail($"Error de red: {ex.Message}", isExternalError: true);
        }

        return await ParseResponseAsync(response, request.PersonalPlanItemId, cancellationToken);
    }

    private static ExternalCreateTaskPayload MapToExternalPayload(SendTaskToExternalRequest request) =>
        new(
            Name: request.Title.Trim(),
            Description: string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IdProject: request.ChobiProjectId,
            IdTaskType: 1,
            IdTaskState: 2,
            IdWorkItemType: 4,
            IdAssignedUsers: [request.AssigneeChobiUserId],
            IdCreatorUser: request.CreatorChobiUserId,
            EstimatedHours: request.EstimatedHours,
            RemainingHours: request.EstimatedHours,
            StartDate: ToChrobiUtcDateString(request.WeekStartDate),
            EstimatedEndDate: ToChrobiUtcDateString(request.WeekEndDate),
            Priority: 0);

    private static string ToChrobiUtcDateString(DateOnly date) =>
        new DateTime(date.Year, date.Month, date.Day, 3, 0, 0, DateTimeKind.Utc)
            .ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private async Task<HttpResponseMessage> ExecuteWithOneRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        CancellationToken cancellationToken)
    {
        var response = await operation();

        if (IsTransientError(response.StatusCode))
        {
            _logger.LogWarning(
                "Transient error {StatusCode} from external API. Retrying in 2s…",
                (int)response.StatusCode);

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            response = await operation();
        }

        return response;
    }

    private static bool IsTransientError(HttpStatusCode status) =>
        status is HttpStatusCode.TooManyRequests
               or HttpStatusCode.ServiceUnavailable
               or HttpStatusCode.GatewayTimeout;

    private async Task<SendTaskToExternalResult> ParseResponseAsync(
        HttpResponseMessage response,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("Chrobi task creation response body for item {ItemId}: {Body}", itemId, rawBody.Length > 500 ? rawBody[..500] : rawBody);

                ExternalCreatedTaskResponse? created = null;
                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    created = System.Text.Json.JsonSerializer.Deserialize<ExternalCreatedTaskResponse>(rawBody, JsonOptions);
                }

                var externalTaskId = ExtractExternalTaskId(created?.Id);
                if (string.IsNullOrWhiteSpace(externalTaskId))
                {
                    // Chrobi returned success but no parseable ID — use a fallback so the item is still marked sent
                    externalTaskId = $"CHROBI-{statusCode}-{itemId}";
                    _logger.LogWarning(
                        "External API returned {StatusCode} but no task ID for item {ItemId}. Using fallback ID. Body: {Body}",
                        statusCode, itemId, rawBody.Length > 200 ? rawBody[..200] : rawBody);
                }
                else
                {
                    _logger.LogInformation(
                        "Task created in external system. ItemId={ItemId}, ExternalTaskId={ExternalId}",
                        itemId, externalTaskId);
                }

                return SendTaskToExternalResult.Ok(externalTaskId, created?.Url);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize external API response for item {ItemId}.", itemId);
                // Still treat as success — Chrobi created the task
                return SendTaskToExternalResult.Ok($"CHROBI-{statusCode}-{itemId}");
            }
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var shortBody = body.Length > 300 ? body[..300] + "…" : body;

        _logger.LogWarning(
            "External API returned {StatusCode} for item {ItemId}. Body: {Body}",
            statusCode, itemId, shortBody);

        var errorMessage = statusCode switch
        {
            400 => $"La API externa rechazó la tarea (400): {shortBody}",
            401 => "Sin autorización para crear tareas en el sistema externo (401).",
            403 => "Acceso denegado al sistema externo (403).",
            404 => "Endpoint de creación de tareas no encontrado en el sistema externo (404).",
            422 => $"Datos inválidos según el sistema externo (422): {shortBody}",
            429 => "El sistema externo está limitando las solicitudes (429). Reintentá más tarde.",
            503 => "El sistema externo no está disponible (503). Reintentá más tarde.",
            _ => $"Error del sistema externo ({statusCode}): {shortBody}"
        };

        return SendTaskToExternalResult.Fail(errorMessage, statusCode, true);
    }

    private static string? ExtractExternalTaskId(JsonElement? id)
    {
        if (id is null)
            return null;

        return id.Value.ValueKind switch
        {
            JsonValueKind.String => id.Value.GetString(),
            JsonValueKind.Number => id.Value.GetRawText(),
            _ => null
        };
    }
}

internal record ExternalCreateTaskPayload(
    string Name,
    string? Description,
    int? IdProject,
    int IdTaskType,
    int IdTaskState,
    int IdWorkItemType,
    int[] IdAssignedUsers,
    int IdCreatorUser,
    decimal? EstimatedHours,
    decimal? RemainingHours,
    string? StartDate,
    string? EstimatedEndDate,
    int Priority
);

internal record ExternalCreatedTaskResponse(
    JsonElement? Id,
    string? Title,
    string? Status,
    string? Url
);
