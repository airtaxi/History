using System.Net.Http.Json;
using History.ApiService.DataTypes;

namespace History.ApiService.Helpers;

/// <summary>
/// HTTP client for the Cloudflare Worker that proxies Kakao Story API requests
/// from Cloudflare's IP pool. The worker is a stateless proxy: it receives a
/// batch of { url, idToken } pairs and returns the raw responses.
/// </summary>
public class KakaoStoryWorkerClient(IConfiguration configuration)
{
    private readonly string _workerUrl = configuration["KakaoStoryPolling:WorkerUrl"];
    private readonly string _workerSecret = configuration["KakaoStoryPolling:WorkerSecret"];

    // Cloudflare closes idle pooled connections after a short period. A short
    // connection lifetime makes the pool discard stale sockets before they can
    // be reused, and the single send retry in PostBatchAsync absorbs the
    // remaining "forcibly closed by remote host" (10054) failures.
    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
        ConnectTimeout = TimeSpan.FromSeconds(10)
    });

    public bool IsConfigured => !string.IsNullOrEmpty(_workerUrl) && !string.IsNullOrEmpty(_workerSecret);

    public async Task<KakaoStoryWorkerBatchResponse> PostBatchAsync(KakaoStoryWorkerBatchRequest request, CancellationToken cancellationToken = default)
    {
        // A stale pooled connection killed by the remote host fails only once:
        // the immediate retry sends on a fresh connection.
        try { return await PostBatchCoreAsync(request, cancellationToken); }
        catch (HttpRequestException) { return await PostBatchCoreAsync(request, cancellationToken); }
    }

    private async Task<KakaoStoryWorkerBatchResponse> PostBatchCoreAsync(KakaoStoryWorkerBatchRequest request, CancellationToken cancellationToken)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _workerUrl) { Content = JsonContent.Create(request) };
        httpRequest.Headers.Add("X-Worker-Secret", _workerSecret);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KakaoStoryWorkerBatchResponse>(cancellationToken);
    }
}
