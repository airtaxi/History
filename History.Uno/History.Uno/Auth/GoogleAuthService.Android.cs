#if ANDROID
#pragma warning disable CS0618 // Type or member is obsolete
using Android.Gms.Auth.Api.SignIn;
using History.MobileClient.Droid;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    private GoogleSignInClient _googleSignInClient;

    public GoogleAuthService()
    {
        var options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
        .RequestIdToken(Constants.GoogleAuthWebClientId)
        .RequestEmail()
        .Build();

        _googleSignInClient = GoogleSignIn.GetClient(Platform.CurrentActivity, options);
    }

    public async Task<string> AuthenticateAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        void OnLoginCompleted(object sender, string token)
        {
            tcs.SetResult(token);
            MainActivity.LoginCompleted -= OnLoginCompleted;
        }

        MainActivity.LoginCompleted += OnLoginCompleted;

        Platform.CurrentActivity.StartActivityForResult(_googleSignInClient.SignInIntent, Constants.GoogleAuthRequestCode);

        return await tcs.Task;
    }

    public async Task<bool> SignOutAsync()
    {
        await _googleSignInClient.SignOutAsync();
        return true;
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
#endif
