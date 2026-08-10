#if IOS
#pragma warning disable CA1422 // Type or member is obsolete
using Foundation;
using Google.SignIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace History.MobileClient.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    public async Task<string> AuthenticateAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        var viewController = GetPresentedViewController();
        if (viewController == null) tcs.SetResult(null);
        else
        {
            var config = new Configuration(Constants.GoogleAuthAppleClientId, Constants.GoogleAuthWebClientId);

            SignIn.SharedInstance.Configuration = config;
            SignIn.SharedInstance.SignInWithPresentingViewController(viewController, (signInResult, error) =>
            {
                if (error != null)
                {
                    tcs.SetResult(null);
                    return;
                }

                var idToken = signInResult?.User?.IdToken?.TokenString;
                tcs.SetResult(string.IsNullOrEmpty(idToken) ? null : idToken);
            });
        }

        try { return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
        catch (TimeoutException) { return null; }
    }

    public Task<bool> SignOutAsync()
    {
        var signIn = SignIn.SharedInstance;
        if (signIn.CurrentUser != null)
        {
            signIn.SignOutUser();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private static UIViewController GetPresentedViewController()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(scene => scene.Windows)
            .FirstOrDefault(candidate => candidate.IsKeyWindow)
            ?? UIApplication.SharedApplication.KeyWindow;

        var viewController = window?.RootViewController;

        while (viewController?.PresentingViewController != null)
            viewController = viewController.PresentingViewController;

        return viewController;
    }
}
#pragma warning restore CA1422 // Type or member is obsolete
#endif