using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeeklyPlanning.Functions.Models;

namespace WeeklyPlanning.Functions.Services;

/// <summary>
/// Cliente HTTP de solo lectura hacia el sistema externo de gestión de proyectos.
/// Implementa reintentos con backoff exponencial para errores de red transitorios.
/// NUNCA realiza operaciones de escritura (POST/PUT/PATCH/DELETE) sobre el sistema externo.
/// </summary>
public class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;
    private readonly int _maxRetries;

    // Delays para backoff exponencial: 2s, 4s, 8s
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExternalApiClient(HttpClient httpClient, IConfiguration config, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _maxRetries = int.TryParse(config["ExternalApi:MaxRetries"], out var r) ? r : 3;
    }

    /// <summary>
    /// Obtiene todas las tareas activas del sistema externo.
    /// Soporta paginación automática si la API la requiere.
    /// </summary>
    public async Task<IReadOnlyList<ExternalApiTask>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var allTasks = new List<ExternalApiTask>();
        int page = 1;
        bool hasMore = true;

        while (hasMore)
        {
            var response = await ExecuteWithRetryAsync(
                () => _httpClient.GetAsync($"tasks?page={page}&pageSize=100", cancellationToken),
                operationName: $"GET /tasks?page={page}",
                cancellationToken);

            await EnsureSuccessAsync(response, "GET /tasks");

            var result = await response.Content.ReadFromJsonAsync<ExternalApiTaskListResponse>(
                JsonOptions, cancellationToken);

            if (result?.Items is null || result.Items.Count == 0)
            {
                hasMore = false;
            }
            else
            {
                allTasks.AddRange(result.Items);
                hasMore = allTasks.Count < result.TotalCount;
                page++;
            }
        }

        _logger.LogInformation("External API returned {Count} tasks.", allTasks.Count);
        return allTasks;
    }

    /// <summary>
    /// Ejecuta una operación HTTP con reintentos ante errores de red transitorios.
    /// Solo reintenta en: errores de red (HttpRequestException), timeouts (TaskCanceledException)
    /// y respuestas 429/503/504 (throttling o servicio no disponible).
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var response = await operation();

                // Reintenta solo en errores transitorios del servidor
                if (attempt < _maxRetries && IsTransientError(response.StatusCode))
                {
                    var delay = RetryDelays[attempt - 1];
                    _logger.LogWarning(
                        "Attempt {Attempt}/{Max} for {Op}: received {StatusCode}. Retrying in {Delay}s...",
                        attempt, _maxRetries, operationName, (int)response.StatusCode, delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return response;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < _maxRetries)
            {
                var delay = RetryDelays[attempt - 1];
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{Max} for {Op} failed ({ExType}). Retrying in {Delay}s...",
                    attempt, _maxRetries, operationName, ex.GetType().Name, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        // Último intento sin capturar excepciones — propaga si falla
        _logger.LogWarning("Final attempt ({Max}) for {Op}.", _maxRetries, operationName);
        return await operation();
    }

    private static bool IsTransientError(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests       // 429
                   or HttpStatusCode.ServiceUnavailable    // 503
                   or HttpStatusCode.GatewayTimeout        // 504
                   or HttpStatusCode.RequestTimeout;       // 408

    private static bool IsTransientException(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or TimeoutException;

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var message = $"{operation} failed with {(int)response.StatusCode}: {body.Truncate(200)}";
            _logger.LogError("External API error: {Message}", message);
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }
}

file static class StringExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
