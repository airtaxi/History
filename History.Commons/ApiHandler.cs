using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Helpers;
using History.Commons.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace History.Commons;

public class ApiHandler(string accessToken = null, string refreshToken = null)
{
    public static ApiHandler Public { get; } = new();
    private static readonly HttpClient Client = new(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All, UseCookies = false }) { BaseAddress = new Uri(CommonConstants.ApiBaseUrl) };
    private static readonly SemaphoreSlim s_refreshSemaphore = new(1, 1);
    private static readonly TimeSpan s_refreshLockTimeout = TimeSpan.FromSeconds(30);

    public static string ApplicationVersion { get; set; } = "unknown";
    public static string Platform { get; set; } = "unknown";

    private readonly bool _initialized = accessToken != null && refreshToken != null;
    private ApiHandler() : this(null, null) => _initialized = false;

    private static HttpMethod GetHttpMethod(HttpRequestMethod method) => method switch
    {
        HttpRequestMethod.Get => HttpMethod.Get,
        HttpRequestMethod.Post => HttpMethod.Post,
        HttpRequestMethod.Put => HttpMethod.Put,
        HttpRequestMethod.Delete => HttpMethod.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static string BuildRequestPath(IBaseRequest request)
    {
        var path = request.Path;

        if (request is IRequestWithUrlParameters requestWithUrlParameters)
        {
            foreach (var parameter in requestWithUrlParameters.UrlParameters)
            {
                path = path.Replace($"{{{parameter.Key}}}", Uri.EscapeDataString(parameter.Value));
            }
        }

        if (request is IRequestWithQueryParameters requestWithQueryParameters)
        {
            if (requestWithQueryParameters.QueryParameters.Count > 0)
            {
                var query = string.Join("&", requestWithQueryParameters.QueryParameters.Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
                path += $"?{query}";
            }
        }

        return path.TrimStart('/');
    }

    private static void AddFilePart(MultipartFormDataContent multipartContent, string name, string fileName, byte[] fileContent)
    {
        var filePart = new ByteArrayContent(fileContent);
        filePart.Headers.ContentType = new MediaTypeHeaderValue(MimeTypes.GetMimeType(fileName));
        multipartContent.Add(filePart, name, fileName);
    }

    private HttpRequestMessage GenerateHttpRequestMessage(IBaseRequest request)
    {
        var httpRequest = new HttpRequestMessage(GetHttpMethod(request.Method), BuildRequestPath(request));

        httpRequest.Headers.TryAddWithoutValidation("User-Agent", $"history-client/official/{Platform}/{ApplicationVersion}");
        httpRequest.Headers.TryAddWithoutValidation("Accept", "application/json, text/json, text/x-json, text/javascript, application/xml, text/xml");

        if (request is IAuthRequiredRequest)
        {
            if (!_initialized) throw new InvalidOperationException("Access token and refresh token must be provided for authenticated requests.");
            httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        }

        if (request is IOptionalAuthRequest)
        {
            if (_initialized)
            {
                httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            }
        }

        var multipartContent = new MultipartFormDataContent();
        var hasMultipartContent = false;

        // Add form file
        if (request is IRequestWithFile requestWithFile)
        {
            AddFilePart(multipartContent, "File", requestWithFile.FileName, requestWithFile.FileContent);
            hasMultipartContent = true;
        }

        // Add form files
        if (request is IRequestWithFiles requestWithFiles)
        {
            foreach (var file in requestWithFiles.Files)
            {
                AddFilePart(multipartContent, "Files", file.Key, file.Value);
                hasMultipartContent = true;
            }
        }

        // Add form data with files (for sticker creation, etc.)
        if (request is IRequestWithFormData requestWithFormData)
        {
            foreach (var file in requestWithFormData.Files)
            {
                // File key format: "paramName|fileName"
                var parts = file.Key.Split('|');
                var paramName = parts[0];
                var fileName = parts.Length > 1 ? parts[1] : file.Key;
                AddFilePart(multipartContent, paramName, fileName, file.Value);
                hasMultipartContent = true;
            }

            foreach (var formField in requestWithFormData.FormData)
            {
                multipartContent.Add(new StringContent(formField.Value), formField.Key);
                hasMultipartContent = true;
            }
        }

        if (request is IRequestWithForm requestWithForm)
        {
            multipartContent.Add(new StringContent(JsonSerializer.Serialize(requestWithForm.Body, requestWithForm.BodyTypeInfo)), "JsonData");
            hasMultipartContent = true;
        }

        if (request is IRequestWithBody requestWithBody) httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestWithBody.Body, requestWithBody.BodyTypeInfo), Encoding.UTF8, "application/json");
        else if (hasMultipartContent) httpRequest.Content = multipartContent;
        else multipartContent.Dispose();

        return httpRequest;
    }

    public async Task<T> ExecuteRequestAsync<T>(IBaseRequest<T> request)
    {
        using var response = await SendRequestAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var content = response.Content == null ? null : await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content)) return default;
            try { return JsonSerializer.Deserialize(content, request.ResponseTypeInfo); }
            catch (JsonException) { return default; }
        }
        else if (response.StatusCode == HttpStatusCode.Unauthorized && request is not RefreshToken)
        {
            await RefreshTokensAsync();

            return await ExecuteRequestAsync(request);
        }
        else throw new HttpRequestException(response.Content == null ? null : await response.Content.ReadAsStringAsync(), null, response.StatusCode);
    }

    public async Task ExecuteRequestAsync(IBaseRequest request)
    {
        using var response = await SendRequestAsync(request);

        if (!response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await RefreshTokensAsync();

            await ExecuteRequestAsync(request);
        }
        else if (!response.IsSuccessStatusCode) throw new HttpRequestException(response.Content == null ? null : await response.Content.ReadAsStringAsync(), null, response.StatusCode);
    }

    public async Task<bool> TryExecuteRequestAsync(IBaseRequest request)
    {
        try
        {
            await ExecuteRequestAsync(request);
            return true;
        }
        catch { return false; }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(IBaseRequest request)
    {
        using var httpRequest = GenerateHttpRequestMessage(request);

        // The previous transport surfaced a timeout as an HttpRequestException with status
        // code 0; keep that shape so existing catch (HttpRequestException) callers behave
        // the same when the 100 second client timeout elapses.
        try { return await Client.SendAsync(httpRequest); }
        catch (TaskCanceledException exception) { throw new HttpRequestException(exception.Message, exception, (HttpStatusCode)0); }
    }

    // Refreshes the token pair, serialized inside this process and across the other client
    // process on the same machine. The persisted pair is re-read after the locks are taken
    // because the other process may have already spent the single-use refresh token; using the
    // stale one would revoke the session and sign the user out.
    private async Task RefreshTokensAsync()
    {
        await s_refreshSemaphore.WaitAsync();
        try
        {
            using var crossProcessLock = CrossProcessTokenRefreshLock.Acquire(s_refreshLockTimeout);
            AdoptPersistedTokens();

            var refreshResponse = await ExecuteRequestAsync(new RefreshToken(refreshToken));
            accessToken = refreshResponse.AccessToken;
            refreshToken = refreshResponse.RefreshToken;
            Configuration.SetValue("AccessToken", accessToken);
            Configuration.SetValue("RefreshToken", refreshToken);
        }
        finally { s_refreshSemaphore.Release(); }
    }

    // Adopts a token pair another process persisted since this instance was created, so the
    // refresh spends the freshest refresh token instead of one the server already revoked.
    private void AdoptPersistedTokens()
    {
        Configuration.ReloadFromDisk();

        var persistedAccessToken = Configuration.GetValue<string>("AccessToken");
        var persistedRefreshToken = Configuration.GetValue<string>("RefreshToken");
        if (string.IsNullOrEmpty(persistedRefreshToken) || persistedRefreshToken == refreshToken) return;

        accessToken = persistedAccessToken;
        refreshToken = persistedRefreshToken;
    }
}
