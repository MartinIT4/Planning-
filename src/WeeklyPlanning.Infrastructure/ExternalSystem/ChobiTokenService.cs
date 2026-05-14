using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WeeklyPlanning.Infrastructure.ExternalSystem;

/// <summary>
/// Holds the current Chrobi access/refresh tokens in memory and handles renewal.
/// Singleton lifetime so all HttpClients share the same token state.
/// </summary>
public sealed class ChobiTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ILogger<ChobiTokenService> _logger;
    private readonly string _baseUrl;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string _accessToken;
    private string _refreshToken;

    public ChobiTokenService(IConfiguration configuration, ILogger<ChobiTokenService> logger)
    {
        _logger = logger;
        _baseUrl = configuration["ExternalApi:BaseUrl"]?.TrimEnd('/') + "/"
            ?? throw new InvalidOperationException("ExternalApi:BaseUrl is required.");
        _accessToken = (configuration["ExternalApi:ApiKey"]
            ?? throw new InvalidOperationException("ExternalApi:ApiKey is required."))
            .Replace("\r", "").Replace("\n", "").Trim();
        _refreshToken = (configuration["ExternalApi:RefreshToken"]
            ?? throw new InvalidOperationException("ExternalApi:RefreshToken is required."))
            .Replace("\r", "").Replace("\n", "").Trim();
    }

    public string AccessToken => _accessToken;

    /// <summary>
    /// Uses the refresh token to obtain a new access token.
    /// Thread-safe: concurrent callers wait for the first refresh to complete.
    /// </summary>
    public async Task<string> RefreshAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            var payload = new { accessToken = _accessToken, refreshToken = _refreshToken };
            using var response = await http.PostAsJsonAsync("auth/refresh", payload, JsonOptions, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Chrobi token refresh failed. StatusCode={StatusCode}", (int)response.StatusCode);
                throw new InvalidOperationException($"Chrobi token refresh failed with status {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, ct)
                ?? throw new InvalidOperationException("Empty response from Chrobi refresh endpoint.");

            _accessToken = result.AccessToken;
            _refreshToken = result.RefreshToken;
            _logger.LogInformation("Chrobi token refreshed successfully.");
            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed record TokenResponse(string AccessToken, string RefreshToken);
}
