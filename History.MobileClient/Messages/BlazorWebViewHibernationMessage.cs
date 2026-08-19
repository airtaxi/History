using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.MobileClient.Messages;

/// <summary>
/// Broadcast on window lifecycle transitions so the Blazor pages can hibernate
/// their Android WebViews. While hibernated (OnPause) the webview halts its JS
/// timers, animations, and video playback, so a foreground service keeping the
/// process alive cannot let a backgrounded Blazor page keep burning CPU.
/// </summary>
public class BlazorWebViewHibernationMessage(bool isHibernated) : ValueChangedMessage<bool>(isHibernated);
