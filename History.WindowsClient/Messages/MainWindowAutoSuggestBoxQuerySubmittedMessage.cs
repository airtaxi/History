using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.WindowsClient.Messages;

public class MainWindowAutoSuggestBoxQuerySubmittedMessage(string query) : ValueChangedMessage<string>(query);
