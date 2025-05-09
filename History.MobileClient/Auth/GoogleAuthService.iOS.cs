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
    private TaskCompletionSource<string> _tcs;

    public GoogleAuthService()
    {
        SignIn.SharedInstance.Scopes = ["https://www.googleapis.com/auth/userinfo.email"];
        SignIn.SharedInstance.ClientId = Constants.GoogleAuthAppleClientId;
    }

    public async Task<string> AuthenticateAsync()
    {
        _tcs = new TaskCompletionSource<string>();

        SignIn.SharedInstance.SignedIn += OnSharedInstanceSignedIn;
        PreparePresentedViewController();
        SignIn.SharedInstance.SignInUser();

        var token = await _tcs.Task;

        SignIn.SharedInstance.SignedIn -= OnSharedInstanceSignedIn;
        return token;
    }


    private void OnSharedInstanceSignedIn(object sender, SignInDelegateEventArgs arg)
    {
        if (arg.Error != null) throw new Exception($"Error - {arg.Error.LocalizedDescription} - {Convert.ToInt32(arg.Error.Code)}");

        SignIn.SharedInstance.CurrentUser.Authentication.GetTokens((Authentication auth, NSError error) =>
        {
            if (error == null || auth?.IdToken != null) _tcs.SetResult(auth.IdToken);
            else _tcs.SetException(new Exception($"Error - {error.LocalizedDescription} - {Convert.ToInt32(error.Code)}"));
        });
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

    private static void PreparePresentedViewController()
    {
        var window = UIApplication.SharedApplication.KeyWindow;

        var viewController = window.RootViewController;

        while (viewController.PresentingViewController != null)
            viewController = viewController.PresentingViewController;

        SignIn.SharedInstance.PresentingViewController = viewController;
    }
}
#pragma warning restore CA1422 // Type or member is obsolete
#endif