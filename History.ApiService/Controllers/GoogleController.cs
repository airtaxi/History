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
public class GoogleController : ControllerBase
{
    private const string ClientId = "401981104412-7n578mga4lggbspntkgg7gtikoqq3auk.apps.googleusercontent.com";
    private const string ClientSecret = "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";
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string RedirectUri = "https://api.history.cenox.io/api/auth/google/callback";

    [HttpGet("login")]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(429)]
    public IActionResult Login([FromQuery] string redirectUrl) => Redirect($"{AuthorizationEndpoint}?response_type=code" +
        $"&client_id={HttpUtility.UrlEncode(ClientId)}" +
        $"&redirect_uri={HttpUtility.UrlEncode(RedirectUri)}" +
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
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "redirect_uri", RedirectUri },
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
