using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WeeklyPlanning.Application.DTOs;
using WeeklyPlanning.Application.Interfaces;

namespace WeeklyPlanning.Infrastructure.ExternalSystem;

public class ChobiReadService : IChobiReadService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ChobiReadService> _logger;

    public ChobiReadService(HttpClient httpClient, ILogger<ChobiReadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChobiUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("lookups/active-users", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Chrobi users. StatusCode={StatusCode}", (int)response.StatusCode);
                return [];
            }

            var users = await response.Content.ReadFromJsonAsync<List<ChobiUserDto>>(JsonOptions, ct);
            return users ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to get Chrobi users.");
            return [];
        }
    }

    public async Task<IReadOnlyList<ChobiProjectDto>> GetProjectsAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "projects?page=1&pageSize=200&sortDesc=true&sortField=StartDate&filterProjectStateIds=2,3",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get Chrobi projects. StatusCode={StatusCode}", (int)response.StatusCode);
                return [];
            }

            var payload = await response.Content.ReadFromJsonAsync<ChobiProjectsResponse>(JsonOptions, ct);
            return payload?.Items ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to get Chrobi projects.");
            return [];
        }
    }

    private sealed record ChobiProjectsResponse(List<ChobiProjectDto>? Items);
}
