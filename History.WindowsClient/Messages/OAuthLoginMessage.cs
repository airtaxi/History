using History.Commons.Enums;

namespace History.WindowsClient.Messages;

public sealed class OAuthLoginMessage(string idToken, SocialService provider, string userJson = null)
{
    public string IdToken { get; } = idToken;
    public SocialService Provider { get; } = provider;
    public string UserJson { get; } = userJson;
}
