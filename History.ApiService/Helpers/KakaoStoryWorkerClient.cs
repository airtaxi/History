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
    private readonly HttpClient _httpClient = new();
    private readonly string _workerUrl = configuration["KakaoStoryPolling:WorkerUrl"];
    private readonly string _workerSecret = configuration["KakaoStoryPolling:WorkerSecret"];

    public bool IsConfigured => !string.IsNullOrEmpty(_workerUrl) && !string.IsNullOrEmpty(_workerSecret);

    public async Task<KakaoStoryWorkerBatchResponse> PostBatchAsync(KakaoStoryWorkerBatchRequest request, CancellationToken cancellationToken = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _workerUrl)
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Worker-Secret", _workerSecret);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KakaoStoryWorkerBatchResponse>(cancellationToken);
    }
}
