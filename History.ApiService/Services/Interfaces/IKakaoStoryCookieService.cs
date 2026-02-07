namespace History.ApiService.Services.Interfaces;

public interface IKakaoStoryCookieService
{
    Task<(string Cookie, DateTimeOffset ExpiresAt, bool FromCache)> GetCookieAsync(
        string loginId,
        string password,
        bool forceRefresh,
        CancellationToken cancellationToken);
}
