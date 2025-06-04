using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/auth/google")]
public class GoogleController(ILogger<GoogleController> logger) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string redirectUrl)
    {
        if (string.IsNullOrEmpty(redirectUrl))
        {
            return BadRequest("RedirectUrl is required");
        }

        HttpContext.Session.SetString("RedirectUrl", redirectUrl);

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("Callback", "Google"),
            Items = { { "scheme", "Google" } }
        };

        return Challenge(properties, "Google");
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync("Google");

            if (!authenticateResult.Succeeded)
            {
                return BadRequest("Google authentication failed");
            }

            var claims = authenticateResult.Principal.Claims;
            var googleIdToken = claims.FirstOrDefault(c => c.Type == "id_token")?.Value;

            if (string.IsNullOrEmpty(googleIdToken))
            {
                var tokens = authenticateResult.Properties.GetTokens();
                googleIdToken = tokens.FirstOrDefault(t => t.Name == "id_token")?.Value;
            }

            if (string.IsNullOrEmpty(googleIdToken)) return BadRequest("Google ID Token not found");

            var redirectUrl = HttpContext.Session.GetString("RedirectUrl");
            if (string.IsNullOrEmpty(redirectUrl)) return BadRequest("RedirectUrl not found");

            var separator = redirectUrl.Contains('?') ? "&" : "?";
            var finalRedirectUrl = $"{redirectUrl}{separator}idToken={googleIdToken}&login=success";

            HttpContext.Session.Remove("RedirectUrl");

            return Redirect(finalRedirectUrl);
        }
        catch (Exception ex)
        {
            logger.LogError("Google OAuth callback error: {Message}", ex.Message);

            var redirectUrl = HttpContext.Session.GetString("RedirectUrl");
            if (!string.IsNullOrEmpty(redirectUrl))
            {
                var separator = redirectUrl.Contains('?') ? "&" : "?";
                var errorRedirectUrl = $"{redirectUrl}{separator}login=error&message={Uri.EscapeDataString("Authentication failed")}";
                return Redirect(errorRedirectUrl);
            }

            return BadRequest("Authentication failed");
        }
    }
}
