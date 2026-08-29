using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace History.ApiService.Helpers;

/// <summary>
/// Creates and validates HMAC-signed OAuth state values.
/// The state carries the redirect URL and an expiration timestamp, and binds them
/// cryptographically so that a callback cannot redirect to an attacker-controlled URL.
/// </summary>
public class OAuthStateProtector(string signingSecret, string[] allowedRedirectUrls)
{
    public static readonly string[] DefaultAllowedRedirectUrls =
    [
        "http://localhost/",
        "https://historyweb.cc",
        "https://historyweb.cc/",
        "https://historyweb.cc/auth/callback",
        "history-app://auth/google",
        "history-app://auth/apple"
    ];

    private const int StateLifetimeMinutes = 10;

    private readonly byte[] _signingKey = SHA256.HashData(Encoding.UTF8.GetBytes(signingSecret));

    public static OAuthStateProtector CreateFromConfiguration(IConfiguration configuration) => new(configuration["HISTORY_OAUTH_STATE_SECRET"] ?? throw new InvalidOperationException("Environment variable 'HISTORY_OAUTH_STATE_SECRET' is required."), configuration["HISTORY_OAUTH_ALLOWED_REDIRECT_URLS"]?.Split(',') ?? DefaultAllowedRedirectUrls);

    public bool IsAllowedRedirectUrl(string redirectUrl) => allowedRedirectUrls.Contains(redirectUrl);

    public string Protect(string redirectUrl)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(StateLifetimeMinutes).ToUnixTimeSeconds();
        var payloadBytes = Encoding.UTF8.GetBytes($"{redirectUrl}|{expiresAt}");
        var signature = HMACSHA256.HashData(_signingKey, payloadBytes);
        return $"{Base64Url.EncodeToString(payloadBytes)}.{Base64Url.EncodeToString(signature)}";
    }

    public bool TryUnprotect(string state, out string redirectUrl)
    {
        redirectUrl = null;
        if (string.IsNullOrEmpty(state)) return false;

        var parts = state.Split('.');
        if (parts.Length != 2) return false;

        byte[] payloadBytes;
        byte[] signature;
        try
        {
            payloadBytes = Base64Url.DecodeFromChars(parts[0]);
            signature = Base64Url.DecodeFromChars(parts[1]);
        }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }

        if (!CryptographicOperations.FixedTimeEquals(signature, HMACSHA256.HashData(_signingKey, payloadBytes))) return false;

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var separatorIndex = payload.LastIndexOf('|');
        if (separatorIndex < 0) return false;

        if (!long.TryParse(payload[(separatorIndex + 1)..], out var expiresAt) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt) return false;

        redirectUrl = payload[..separatorIndex];
        return IsAllowedRedirectUrl(redirectUrl);
    }
}