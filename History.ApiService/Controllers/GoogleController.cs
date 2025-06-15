using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Web;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/auth/google")]
public class GoogleController : ControllerBase
{
    private const string ClientId = "401981104412-7n578mga4lggbspntkgg7gtikoqq3auk.apps.googleusercontent.com";
    private const string ClientSecret = "***REMOVED***"; // Replace with your actual client secret

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string redirectUrl)
    {
        string clientId = "401981104412-7n578mga4lggbspntkgg7gtikoqq3auk.apps.googleusercontent.com";
        string redirectUri = "https://api.history.cenox.io/api/auth/google/callback";

        string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

        var url = $"{authorizationEndpoint}?response_type=code" +
                  $"&client_id={HttpUtility.UrlEncode(clientId)}" +
                  $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
                  $"&scope={HttpUtility.UrlEncode("openid email profile")}" +
                  $"&state={HttpUtility.UrlEncode(redirectUrl)}";

        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToAction("Login", "Google");

        string redirectUri = "https://api.history.cenox.io/api/auth/google/callback";

        using var httpClient = new HttpClient();
        var tokenRequestParams = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "redirect_uri", redirectUri },
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

        // state에 원래 요청된 callback URL이 있다면 거기로 redirect
        var redirectUrl = $"{state}?id_token={HttpUtility.UrlEncode(idToken)}";
        return Redirect(redirectUrl);
    }
}
