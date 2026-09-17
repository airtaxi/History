using History.Commons;

namespace History.WindowsClient.Helpers;

// Single source of truth for the History session tokens. They live in the shared settings file
// instead of the packaged app settings for two reasons: a refreshed pair must survive a restart
// (the API server revokes the spent refresh token, so persisting it to only one store signs the
// user out), and the background notification service reads the same pair from the same file.
public static class AuthTokenStore
{
    private const string AccessTokenKey = "AccessToken";
    private const string RefreshTokenKey = "RefreshToken";

    public static string AccessToken => Configuration.GetValue<string>(AccessTokenKey);

    public static string RefreshToken => Configuration.GetValue<string>(RefreshTokenKey);

    public static void SetTokens(string accessToken, string refreshToken)
    {
        Configuration.SetValue(AccessTokenKey, accessToken);
        Configuration.SetValue(RefreshTokenKey, refreshToken);
    }

    public static void ClearTokens()
    {
        Configuration.SetValue(AccessTokenKey, null);
        Configuration.SetValue(RefreshTokenKey, null);
    }
}
