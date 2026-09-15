using CommunityToolkit.Mvvm.Messaging.Messages;
using History.WindowsClient.Enums;

namespace History.WindowsClient.Messages;

// The main page applied a feed mode; the title bar syncs every toggle from this single value.
public class MainFeedModeChangedMessage(MainFeedMode mode) : ValueChangedMessage<MainFeedMode>(mode);
