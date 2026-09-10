using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.WindowsClient.Messages;

public class KakaoStoryModeChangedMessage(bool isKakaoStoryMode) : ValueChangedMessage<bool>(isKakaoStoryMode);
