#if WINDOWS
using History.MobileClient.Pages;

namespace History.MobileClient.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    public async Task<string> AuthenticateAsync()
    {
        var page = new GoogleLoginPage();
        await App.PushModalAsync(page);
        return await page.GetResultAsync();
    }

    public Task<bool> SignOutAsync() => Task.FromResult(true);
}
#endif
