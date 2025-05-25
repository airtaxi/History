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

        var config = new Configuration(Constants.GoogleAuthAppleClientId, Constants.GoogleAuthWebClientId);

        var viewController = GetPresentedViewController();
        SignIn.SharedInstance.Configuration = config;
        SignIn.SharedInstance.SignInWithPresentingViewController(viewController, (signInResult, error) =>
        {
            if (error != null)
            {
                tcs.SetException(new Exception($"Error - {error.LocalizedDescription} - {Convert.ToInt32(error.Code)}"));
                return;
            }

            var user = signInResult.User;
            var idToken = user.IdToken.TokenString;

            if (!string.IsNullOrEmpty(idToken)) tcs.SetResult(idToken);
        });

        var token = await tcs.Task;
        return token;
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
        var window = UIApplication.SharedApplication.KeyWindow;

        var viewController = window.RootViewController;

        while (viewController.PresentingViewController != null)
            viewController = viewController.PresentingViewController;

        return viewController;
    }
}
#pragma warning restore CA1422 // Type or member is obsolete
#endif