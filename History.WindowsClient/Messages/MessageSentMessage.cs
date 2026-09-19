namespace History.WindowsClient.Messages;

// A mail that was sent from a compose dialog; the messages side bar reloads its list when
// the sent mail belongs to its platform.
public sealed class MessageSentMessage(bool isKakaoStoryMode)
{
    public bool IsKakaoStoryMode { get; } = isKakaoStoryMode;
}
