using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.OS;

#pragma warning disable CS0618 // Type or member is obsolete
namespace History.MobileClient;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ResizeableActivity = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public static event EventHandler<string> LoginCompleted;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        NativeMedia.Platform.Init(this, savedInstanceState);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        if (NativeMedia.Platform.CheckCanProcessResult(requestCode, resultCode, data))
            NativeMedia.Platform.OnActivityResult(requestCode, resultCode, data);

        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == Constants.GoogleAuthRequestCode)
        {
            var result = Android.Gms.Auth.Api.Auth.GoogleSignInApi.GetSignInResultFromIntent(data);
            if (result.IsSuccess)
            {
                var token = result.SignInAccount?.IdToken;
                LoginCompleted?.Invoke(this, token);
            }
            else
            {
                LoginCompleted?.Invoke(this, null);
            }
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
