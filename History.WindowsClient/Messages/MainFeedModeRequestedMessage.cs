using CommunityToolkit.Mvvm.Messaging.Messages;
using History.WindowsClient.Enums;

namespace History.WindowsClient.Messages;

// A title bar feed toggle was switched by the user; the main page applies the requested feed mode.
public class MainFeedModeRequestedMessage(MainFeedMode mode) : ValueChangedMessage<MainFeedMode>(mode);
