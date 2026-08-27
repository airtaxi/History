using DotNet.RateLimiter;
using DotNet.RateLimiter.ActionFilters;
using Google.Apis.Auth.OAuth2;
using History.ApiService.Helpers;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using System.Text.Json.Nodes;
using System.Web;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/auth/apple")]
[RateLimit(Limit = 1, PeriodInSec = 1)]
public class AppleController(IConfiguration configuration) : ControllerBase
{
    private const string AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize";
    private readonly string _keyId = configuration["HISTORY_APPLE_KEY_ID"] ?? "DGK52ABR8V";
    private readonly string _teamId = configuration["HISTORY_APPLE_TEAM_ID"] ?? "UP6EXS2HJJ";
    private readonly string _clientId = configuration["HISTORY_APPLE_CLIENT_ID"] ?? "com.airtaxi.history.as";
    private readonly string _redirectUri = configuration["HISTORY_APPLE_REDIRECT_URI"] ?? "https://api.history.cenox.io/api/auth/apple/callback";
    private readonly string _privateKeyPath = configuration["HISTORY_APPLE_PRIVATE_KEY_PATH"] ?? Path.Combine(AppContext.BaseDirectory, "AuthKey_DGK52ABR8V.p8");
    private readonly OAuthStateProtector _stateProtector = OAuthStateProtector.CreateFromConfiguration(configuration);

    public string GenerateJwtToken() => AppleIdTokenHelper.GenerateJwtToken(_keyId, _teamId, _clientId, _privateKeyPath);

    [HttpGet("login")]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(429)]
    public IActionResult Login([FromQuery] string redirectUrl)
    {
        if (!_stateProtector.IsAllowedRedirectUrl(redirectUrl)) return BadRequest("Invalid redirect URL.");

        var state = _stateProtector.Protect(redirectUrl);
        return Redirect($"{AuthorizationEndpoint}?response_type=code" + $"&client_id={HttpUtility.UrlEncode(_clientId)}" + $"&redirect_uri={HttpUtility.UrlEncode(_redirectUri)}" + $"&scope={HttpUtility.UrlEncode("name email")}" + $"&response_mode=form_post" + $"&state={HttpUtility.UrlEncode(state)}");
    }

    [HttpPost("callback")]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(429)]
    public async Task<IActionResult> Callback([FromForm] string code, [FromForm] string state, [FromForm] string user)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToAction("Login", "Apple");

        // state carries the signed redirect URL to return to after successful login
        if (!_stateProtector.TryUnprotect(state, out var redirectUrl)) return BadRequest("Invalid or expired state.");

        string idToken;
        using (var client = new RestClient("https://appleid.apple.com/auth/token"))
        {
            var request = new RestRequest() { Method = Method.Post };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", _clientId, ParameterType.GetOrPost);
            request.AddParameter("client_secret", GenerateJwtToken(), ParameterType.GetOrPost);
            request.AddParameter("code", code, ParameterType.GetOrPost);
            request.AddParameter("grant_type", "authorization_code", ParameterType.GetOrPost);
            request.AddParameter("redirect_uri", _redirectUri, ParameterType.GetOrPost);

            var response = await client.ExecuteAsync(request);
            var content = response.Content;
            var data = JsonNode.Parse(response.Content).AsObject();
            idToken = (string)data["id_token"];
        }

        if (idToken == null) return StatusCode(500, "Failed to retrieve ID token from Apple.");

        JsonNode userInfo = null;
        if (!string.IsNullOrEmpty(user)) userInfo = JsonNode.Parse(user);

        var targetUrl = $"{redirectUrl}?id_token={HttpUtility.UrlEncode(idToken)}";
        if (userInfo != null) targetUrl += $"&user={HttpUtility.UrlEncode(user)}";

        return Redirect(targetUrl);
    }

}