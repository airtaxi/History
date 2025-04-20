using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons;

public class ApiHandler(string accessToken = null, string refreshToken = null)
{
    public static ApiHandler Public { get; } = new();
    private static readonly RestClient Client = new(CommonsConstants.ApiBaseUrl);

    private readonly bool _initialized = accessToken != null && refreshToken != null;
    private ApiHandler() : this(null, null) => _initialized = false;

    private RestRequest GenerateRestRequest(IBaseRequest request)
    {
        var restRequest = new RestRequest(request.Path, request.Method);

        if (request is IAuthRequiredRequest)
        {
            if (!_initialized) throw new InvalidOperationException("Access token and refresh token must be provided for authenticated requests.");
            restRequest.AddHeader("Authorization", $"Bearer {accessToken}");
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
        if (request is IRequestWithBody requestWithBody) restRequest.AddJsonBody(requestWithBody.Body);
        return restRequest;
    }

    public async Task<T> ExecuteRequestAsync<T>(IBaseRequest<T> request)
    {
        var restRequest = GenerateRestRequest(request);

        var response = await Client.ExecuteAsync<T>(restRequest);

        if (response.IsSuccessful) return response.Data;
        else throw new HttpRequestException(response.ErrorMessage, response.ErrorException, response.StatusCode);
    }

    public async Task ExecuteRequestAsync(IBaseRequest request)
    {
        var restRequest = GenerateRestRequest(request);

        var response = await Client.ExecuteAsync(restRequest);

        if (!response.IsSuccessful) throw new HttpRequestException(response.ErrorMessage, response.ErrorException, response.StatusCode);
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
}