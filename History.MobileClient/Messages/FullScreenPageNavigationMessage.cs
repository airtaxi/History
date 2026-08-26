using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

public class FullScreenPageNavigationMessage(Page page, bool disappear) : ValueChangedMessage<Page>(page)
{
    public bool Disappear { get; } = disappear;
}