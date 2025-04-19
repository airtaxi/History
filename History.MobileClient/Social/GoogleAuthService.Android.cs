#if ANDROID
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using AndroidX.Credentials;
using Java.Interop;
using Kotlin.Coroutines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.GoogleAndroid.Libraries.Identity.GoogleId;

namespace History.MobileClient.Social;

public class GoogleAuthService
{
    public GoogleAuthService()
    {
        var context = Platform.CurrentActivity;

        // CANNOT USE CredentialManager.Create(context) because it is not available in Xamarin.Android
        //var credentialManager = CredentialManager.Create(context);

        //var googleIdOption = new GetGoogleIdOption.Builder()
        //    .SetFilterByAuthorizedAccounts(false)
        //    .SetServerClientId("INPUT_WEB_CLIENT_ID")
        //    .Build();

        //var request = new GetCredentialRequest.Builder()
        //    .AddCredentialOption(googleIdOption)
        //    .Build();

    }
}
#endif