using DotNet.RateLimiter.ActionFilters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Web;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/auth/google")]
[RateLimit(Limit = 1, PeriodInSec = 1)]
public class GoogleController(IConfiguration configuration) : ControllerBase
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private readonly string _clientId = configuration["HISTORY_GOOGLE_CLIENT_ID"] ?? "401981104412-7n578mga4lggbspntkgg7gtikoqq3auk.apps.googleusercontent.com";
    private readonly string _clientSecret = configuration["HISTORY_GOOGLE_CLIENT_SECRET"] ?? throw new InvalidOperationException("Environment variable 'HISTORY_GOOGLE_CLIENT_SECRET' is required.");
    private readonly string _redirectUri = configuration["HISTORY_GOOGLE_REDIRECT_URI"] ?? "https://api.history.cenox.io/api/auth/google/callback";

    [HttpGet("login")]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(429)]
    public IActionResult Login([FromQuery] string redirectUrl) => Redirect($"{AuthorizationEndpoint}?response_type=code" +
        $"&client_id={HttpUtility.UrlEncode(_clientId)}" +
        $"&redirect_uri={HttpUtility.UrlEncode(_redirectUri)}" +
        $"&scope={HttpUtility.UrlEncode("openid email profile")}" +
        $"&state={HttpUtility.UrlEncode(redirectUrl)}");

    [HttpGet("callback")]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(429)]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToAction("Login", "Google");

        using var httpClient = new HttpClient();
        var tokenRequestParams = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "redirect_uri", _redirectUri },
            { "grant_type", "authorization_code" }
        };

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(tokenRequestParams)
        };

        var response = await httpClient.SendAsync(tokenRequest);
        var responseString = await response.Content.ReadAsStringAsync();
        var tokenData = JsonNode.Parse(responseString);

        string idToken = tokenData["id_token"]?.ToString();

        if (idToken == null) return StatusCode(500, "Failed to retrieve ID token from Google.");

        // state has redirect URL to return to after successful login
        var redirectUrl = $"{state}?id_token={HttpUtility.UrlEncode(idToken)}";
        return Redirect(redirectUrl);
    }
}