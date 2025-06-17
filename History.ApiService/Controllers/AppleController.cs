using System.Text.Json.Nodes;
using System.Web;
using Google.Apis.Auth.OAuth2;
using History.ApiService.Helpers;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/auth/apple")]
public class AppleController : ControllerBase
{
    private const string KeyId = "DGK52ABR8V";
    private const string TeamId = "UP6EXS2HJJ";
    private const string ClientId = "com.airtaxi.history";
    private readonly static string PrivateKeyPath = Path.Combine(AppContext.BaseDirectory, "AuthKey_DGK52ABR8V.p8");
    private const string AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize";
    private const string RedirectUri = "https://api.history.cenox.io/api/auth/apple/callback";

    public static string GenerateJwtToken() => AppleIdTokenHelper.GenerateJwtToken(KeyId, TeamId, ClientId, PrivateKeyPath);

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string redirectUrl) => Redirect($"{AuthorizationEndpoint}?response_type=code" +
        $"&client_id={HttpUtility.UrlEncode(ClientId)}" +
        $"&redirect_uri={HttpUtility.UrlEncode(RedirectUri)}" +
        $"&scope={HttpUtility.UrlEncode("name email")}" +
        $"&response_mode=form_post" +
        $"&state={HttpUtility.UrlEncode(redirectUrl)}");

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromForm] string code, [FromForm] string state, [FromForm] string user)
    {
        if (string.IsNullOrEmpty(code))
            return RedirectToAction("Login", "Apple");

        string idToken;
        using (var client = new RestClient("https://appleid.apple.com/auth/token"))
        {
            var request = new RestRequest() { Method = Method.Post };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", ClientId, ParameterType.GetOrPost);
            request.AddParameter("client_secret", GenerateJwtToken(), ParameterType.GetOrPost);
            request.AddParameter("code", code, ParameterType.GetOrPost);
            request.AddParameter("grant_type", "authorization_code", ParameterType.GetOrPost);
            request.AddParameter("redirect_uri", RedirectUri, ParameterType.GetOrPost);

            var response = await client.ExecuteAsync(request);
            var content = response.Content;
            var data = JsonNode.Parse(response.Content).AsObject();
            idToken = (string)data["id_token"];
        }

        if (idToken == null) return StatusCode(500, "Failed to retrieve ID token from Apple.");

        JsonNode userInfo = null;
        if (!string.IsNullOrEmpty(user)) userInfo = JsonNode.Parse(user);

        var redirectUrl = $"{state}?id_token={HttpUtility.UrlEncode(idToken)}";

        if (userInfo != null) redirectUrl += $"&user={HttpUtility.UrlEncode(user)}";

        return Redirect(redirectUrl);
    }

}
