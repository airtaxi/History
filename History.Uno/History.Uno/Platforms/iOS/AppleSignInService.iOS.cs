#if __IOS__
using System.Runtime.InteropServices;
using AuthenticationServices;
using Foundation;
using UIKit;

namespace History.Uno.Services;

/// <summary>
/// Native iOS Apple Sign-In using AuthenticationServices framework.
/// Uno does not have MAUI Essentials' AppleSignInAuthenticator, so we use
/// ASAuthorizationAppleIdProvider directly (per Uno official docs: interop/apple-login.md).
/// </summary>
public class AppleSignInService
{
    private TaskCompletionSource<AppleSignInResult> _tcs;
    private AuthorizationControllerDelegate _delegate;
    private PresentationContextProvider _presentationContextProvider;

    /// <summary>
    /// Initiates the native Apple Sign-In flow and returns the idToken + fullName.
    /// </summary>
    public Task<AppleSignInResult> AuthenticateAsync()
    {
        _tcs = new TaskCompletionSource<AppleSignInResult>();

        var appleIDProvider = new ASAuthorizationAppleIdProvider();
        var request = appleIDProvider.CreateRequest();
        request.RequestedScopes = new[] { ASAuthorizationScope.FullName, ASAuthorizationScope.Email };

        var authorizationController = new ASAuthorizationController(new ASAuthorizationRequest[] { request });

        _delegate = new AuthorizationControllerDelegate(tcs: _tcs);
        _presentationContextProvider = new PresentationContextProvider();

        authorizationController.Delegate = _delegate;
        authorizationController.PresentationContextProvider = _presentationContextProvider;
        authorizationController.PerformRequests();

        return _tcs.Task;
    }
}

/// <summary>
/// Result of a native Apple Sign-In flow.
/// </summary>
public record AppleSignInResult(string IdToken, string FullName);

/// <summary>
/// Handles the authorization callback from ASAuthorizationController.
/// Extracts idToken (IdentityToken) and full name from the credential.
/// </summary>
public class AuthorizationControllerDelegate : ASAuthorizationControllerDelegate
{
    private readonly TaskCompletionSource<AppleSignInResult> _tcs;

    public AuthorizationControllerDelegate(TaskCompletionSource<AppleSignInResult> tcs) => _tcs = tcs;

    public override void DidComplete(ASAuthorizationController controller, ASAuthorization authorization)
    {
        try
        {
            var credential = authorization.GetCredential<ASAuthorizationAppleIdCredential>();
            if (credential == null)
            {
                _tcs.TrySetResult(new AppleSignInResult(null, null));
                return;
            }

            // IdentityToken is NSData — convert to base64 string
            var idToken = credential.IdentityToken?.ToString(NSStringEncoding.UTF8);

            // Full name — only available on first sign-in
            string fullName = null;
            var personName = credential.FullName;
            if (personName != null)
            {
                // Korean name format: lastName + firstName
                var lastName = personName.FamilyName ?? "";
                var firstName = personName.GivenName ?? "";
                fullName = lastName + firstName;
            }

            _tcs.TrySetResult(new AppleSignInResult(idToken, fullName));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Apple Sign-In failed: {exception.Message}");
            _tcs.TrySetResult(new AppleSignInResult(null, null));
        }
    }

    public override void DidComplete(ASAuthorizationController controller, NSError error)
    {
        System.Diagnostics.Debug.WriteLine($"Apple Sign-In error: {error?.LocalizedDescription}");
        _tcs.TrySetResult(new AppleSignInResult(null, null));
    }
}

/// <summary>
/// Provides the presentation window for the Apple Sign-In sheet.
/// </summary>
public class PresentationContextProvider : NSObject, IASAuthorizationControllerPresentationContextProviding
{
    public UIWindow GetPresentationAnchor(ASAuthorizationController controller)
        => UIApplication.SharedApplication.KeyWindow;
}
#endif