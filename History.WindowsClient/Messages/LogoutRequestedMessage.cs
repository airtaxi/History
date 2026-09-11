namespace History.WindowsClient.Messages;

// Raised after a successful sign-out or account withdrawal: the main window navigates its
// root frame back to the login page and clears the navigation stack. A dedicated message is
// used because the settings window is a separate modal window that cannot navigate the main
// window's frame directly.
public class LogoutRequestedMessage;
