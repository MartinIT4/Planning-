using System.Net;
using System.Net.Http.Headers;

namespace WeeklyPlanning.Infrastructure.ExternalSystem;

/// <summary>
/// Automatically refreshes the Chrobi JWT when a 401 is received and retries the request once.
/// </summary>
public sealed class ChobiAuthHandler : DelegatingHandler
{
    private readonly ChobiTokenService _tokenService;

    public ChobiAuthHandler(ChobiTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenService.AccessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token expired — refresh and retry once
            var newToken = await _tokenService.RefreshAsync(cancellationToken);

            // The original request content may have been consumed; clone it
            var retryRequest = await CloneRequestAsync(request);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

            response.Dispose();
            response = await base.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
