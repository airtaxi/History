using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Helpers;
using History.Commons.Interfaces;
using RestSharp;
using System.Net;
using System.Text.Json;

namespace History.Commons;

public class ApiHandler(string accessToken = null, string refreshToken = null)
{
    public static ApiHandler Public { get; } = new();
    private static readonly RestClient Client = new(CommonConstants.ApiBaseUrl);
    private static readonly SemaphoreSlim s_refreshSemaphore = new(1, 1);
    private static readonly TimeSpan s_refreshLockTimeout = TimeSpan.FromSeconds(30);

    public static string ApplicationVersion { get; set; } = "unknown";
    public static string Platform { get; set; } = "unknown";

    private readonly bool _initialized = accessToken != null && refreshToken != null;
    private ApiHandler() : this(null, null) => _initialized = false;

    private RestRequest GenerateRestRequest(IBaseRequest request)
    {
        var restRequest = new RestRequest(request.Path, request.Method);

        restRequest.AddHeader("User-Agent", $"history-client/official/{Platform}/{ApplicationVersion}");


        if (request is IAuthRequiredRequest)
        {
            if (!_initialized) throw new InvalidOperationException("Access token and refresh token must be provided for authenticated requests.");
            restRequest.AddHeader("Authorization", $"Bearer {accessToken}");
        }

        if (request is IOptionalAuthRequest)
        {
            if (_initialized)
            {
                restRequest.AddHeader("Authorization", $"Bearer {accessToken}");
            }
        }

        if (request is IRequestWithUrlParameters requestWithUrlParameters)
        {
            foreach (var parameter in requestWithUrlParameters.UrlParameters)
            {
                restRequest.AddUrlSegment(parameter.Key, parameter.Value);
            }
        }

        if (request is IRequestWithQueryParameters requestWithQueryParameters)
        {
            foreach (var parameter in requestWithQueryParameters.QueryParameters)
            {
                restRequest.AddQueryParameter(parameter.Key, parameter.Value);
            }
        }

        // Add form file
        if (request is IRequestWithFile requestWithFile)
        {
            restRequest.AddFile("File", requestWithFile.FileContent, requestWithFile.FileName, MimeTypes.GetMimeType(requestWithFile.FileName));
        }

        // Add form files
        if (request is IRequestWithFiles requestWithFiles)
        {
            foreach (var file in requestWithFiles.Files)
            {
                restRequest.AddFile("Files", file.Value, file.Key, MimeTypes.GetMimeType(file.Key));
            }
        }

        // Add form data with files (for sticker creation, etc.)
        if (request is IRequestWithFormData requestWithFormData)
        {
            restRequest.AlwaysMultipartFormData = true;
            foreach (var formField in requestWithFormData.FormData)
            {
                restRequest.AddParameter(formField.Key, formField.Value);
            }
            foreach (var file in requestWithFormData.Files)
            {
                // File key format: "paramName|fileName"
                var parts = file.Key.Split('|');
                var paramName = parts[0];
                var fileName = parts.Length > 1 ? parts[1] : file.Key;
                restRequest.AddFile(paramName, file.Value, fileName, MimeTypes.GetMimeType(fileName));
            }
        }

        if (request is IRequestWithBody requestWithBody) restRequest.AddJsonBody(requestWithBody.Body);
        if (request is IRequestWithForm requestWithForm)
        {
            restRequest.AlwaysMultipartFormData = true;
            restRequest.AddParameter("JsonData", JsonSerializer.Serialize(requestWithForm.Body));
        }
        return restRequest;
    }

    public async Task<T> ExecuteRequestAsync<T>(IBaseRequest<T> request)
    {
        var restRequest = GenerateRestRequest(request);

        var response = await Client.ExecuteAsync<T>(restRequest);

        if (response.IsSuccessStatusCode) return response.Data;
        else if (response.StatusCode == HttpStatusCode.Unauthorized && request is not RefreshToken)
        {
            await RefreshTokensAsync();

            return await ExecuteRequestAsync(request);
        }
        else throw new HttpRequestException(response.Content, response.ErrorException, response.StatusCode);
    }

    public async Task ExecuteRequestAsync(IBaseRequest request)
    {
        var restRequest = GenerateRestRequest(request);

        var response = await Client.ExecuteAsync(restRequest);

        if (!response.IsSuccessful && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await RefreshTokensAsync();

            await ExecuteRequestAsync(request);
        }
        else if (!response.IsSuccessful) throw new HttpRequestException(response.Content, response.ErrorException, response.StatusCode);
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