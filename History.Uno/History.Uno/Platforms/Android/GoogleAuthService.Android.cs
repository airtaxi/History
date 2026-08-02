#if __ANDROID__
#pragma warning disable CS0618
using Android.Gms.Auth.Api.SignIn;
using History.Uno.Droid;

namespace History.Uno.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private GoogleSignInClient _googleSignInClient;

    public GoogleAuthService()
    {
        var options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestIdToken(Constants.GoogleAuthWebClientId)
            .RequestEmail()
            .Build();

        _googleSignInClient = GoogleSignIn.GetClient(MainActivity.CurrentActivity, options);
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

        MainActivity.CurrentActivity.StartActivityForResult(_googleSignInClient.SignInIntent, Constants.GoogleAuthRequestCode);

        return await tcs.Task;
    }

    public async Task<bool> SignOutAsync()
    {
        await _googleSignInClient.SignOutAsync();
        return true;
    }
}
#pragma warning restore CS0618
#endif