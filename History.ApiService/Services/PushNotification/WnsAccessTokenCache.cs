namespace History.ApiService.Services.PushNotification;

/// <summary>
/// Thread-safe cache for the WNS OAuth access token. The token is valid for 24 hours and is
/// refreshed shortly before expiry or when WNS rejects it with 401 Unauthorized.
/// </summary>
public class WnsAccessTokenCache
{
    private const int RefreshBufferSeconds = 300;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    /// <summary>
    /// Returns the cached access token, refreshing it through <paramref name="refreshAsync"/> when it is expired or missing.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(Func<Task<(string AccessToken, int ExpiresInSeconds)>> refreshAsync)
    {
        if (!IsExpired) return _accessToken;

        await _refreshLock.WaitAsync();
        try
        {
            if (!IsExpired) return _accessToken;

            var (accessToken, expiresInSeconds) = await refreshAsync();
            if (string.IsNullOrEmpty(accessToken)) return null;

            _accessToken = accessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(expiresInSeconds - RefreshBufferSeconds, 60));
            return _accessToken;
        }
        finally { _refreshLock.Release(); }
    }

    /// <summary>
    /// Forces the next token request to refresh, used when WNS rejects the cached token with 401 Unauthorized.
    /// </summary>
    public void Invalidate()
    {
        _accessToken = null;
        _expiresAt = DateTimeOffset.MinValue;
    }

    private bool IsExpired => string.IsNullOrEmpty(_accessToken) || DateTimeOffset.UtcNow >= _expiresAt;
}