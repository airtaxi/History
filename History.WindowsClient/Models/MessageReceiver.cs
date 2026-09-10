using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.Models;

// Receiver identity for the message dialogs, shared by the History and Kakao Story flows.
public record MessageReceiver(string Id, string Name, ImageSource ProfileImage, bool IsModerator, bool IsAdmin);
