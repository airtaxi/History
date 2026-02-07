using History.ApiService.Services.Interfaces;
using Microsoft.Playwright;

namespace History.ApiService.Services;

public class KakaoStoryCookieService : IKakaoStoryCookieService
{
    private static readonly SemaphoreSlim LoginSemaphore = new(1, 1);
    private static readonly object CacheLock = new();
    private static string s_cachedCookie;
    private static DateTimeOffset s_cachedExpiresAt;

    public async Task<(string Cookie, DateTimeOffset ExpiresAt, bool FromCache)> GetCookieAsync(string loginId, string password, bool forceRefresh, CancellationToken cancellationToken)
    {
        var cached = GetCachedCookie();
        if (!forceRefresh && cached.Cookie != null && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return (cached.Cookie, cached.ExpiresAt, true);

        await LoginSemaphore.WaitAsync(cancellationToken);
        try
        {
            cached = GetCachedCookie();
            if (!forceRefresh && cached.Cookie != null && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
                return (cached.Cookie, cached.ExpiresAt, true);

            var result = await LoginAndGetCookiesAsync(loginId, password, cancellationToken);
            SetCachedCookie(result.Cookie, result.ExpiresAt);
            return (result.Cookie, result.ExpiresAt, false);
        }
        finally
        {
            LoginSemaphore.Release();
        }
    }

    private static (string Cookie, DateTimeOffset ExpiresAt) GetCachedCookie()
    {
        lock (CacheLock)
        {
            return (s_cachedCookie, s_cachedExpiresAt);
        }
    }

    private static void SetCachedCookie(string cookie, DateTimeOffset expiresAt)
    {
        lock (CacheLock)
        {
            s_cachedCookie = cookie;
            s_cachedExpiresAt = expiresAt;
        }
    }

    private static async Task<(string Cookie, DateTimeOffset ExpiresAt)> LoginAndGetCookiesAsync(string loginId, string password, CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        IBrowser browser;
        try
        {
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = "chrome"
            });
        }
        catch (PlaywrightException)
        {
            // Fallback to bundled Chromium when Chrome channel is unavailable.
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0"
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("https://story.kakao.com/", new PageGotoOptions { WaitUntil = WaitUntilState.Load });

        await page.WaitForURLAsync("**/accounts.kakao.com/login/**", new PageWaitForURLOptions { Timeout = 30000 });

        await page.GetByRole(AriaRole.Textbox, new() { Name = "Enter Account Information" }).FillAsync(loginId);
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Enter Password" }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log In", Exact = true }).ClickAsync();

        await page.WaitForURLAsync("https://story.kakao.com/**", new PageWaitForURLOptions { Timeout = 30000 });

        var cookies = await context.CookiesAsync();
        var filtered = cookies.Where(c => c.Domain.Contains("kakao.com", StringComparison.OrdinalIgnoreCase)).ToList();
        if (filtered.Count == 0) throw new InvalidOperationException("Failed to collect kakao.com cookies.");

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var c in filtered)
        {
            if (!map.ContainsKey(c.Name)) map[c.Name] = c.Value;
        }

        var cookieString = string.Join("; ", map.Select(kv => $"{kv.Key}={kv.Value}"));
        var expiresAt = DateTimeOffset.UtcNow.AddHours(20);
        return (cookieString, expiresAt);
    }
}
