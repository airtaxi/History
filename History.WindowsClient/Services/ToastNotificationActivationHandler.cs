using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Pages;
using History.WindowsClient.Views;
using System.Web;

namespace History.WindowsClient.Services;

// Routes toast deep-link arguments, delivered as "history-app://toast?..." protocol
// activations, to the notification target — mirroring the notification flyout tap behavior.
// Targets without a destination in this project stay no-op stubs.
public static class ToastNotificationActivationHandler
{
    private static string _pendingArguments;

    public static async Task HandleAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        var parameters = HttpUtility.ParseQueryString(query);
        var typeText = parameters["Type"];
        if (string.IsNullOrEmpty(typeText) || !Enum.TryParse<NotificationType>(typeText, out var type)) return;

        if (type == NotificationType.KakaoStory) return; // TODO: Open the Kakao Story post via the Scheme once the Kakao Story mode supports it.
        if (type == NotificationType.Message) return; // TODO: Open the message thread once a message page exists.
        if (type == NotificationType.InviteCodeRequest || type == NotificationType.InviteCodeRequestResult) return; // TODO: Open the invite code request pages once they exist.
        if (type == NotificationType.Restriction) return; // TODO: Show the restriction notice with the appeal flow.
        if (type == NotificationType.Birthday) return; // TODO: Open the birthday profile once the birthday flow exists.

        // A toast clicked during a cold start can arrive before the login completes; the
        // arguments are replayed once the user signs in.
        if (string.IsNullOrEmpty(CommonShared.UserId))
        {
            _pendingArguments = query;
            return;
        }

        if (type == NotificationType.FriendRequest)
        {
            var userId = parameters["UserId"];
            if (string.IsNullOrEmpty(userId)) return;

            // Bring the window forward: a toast activation does not focus the window by itself.
            MainWindow.SetForegroundWindow();
            MainWindow.Frame.Navigate(typeof(ProfilePage), userId);
            return;
        }

        var postId = parameters["PostId"];
        if (string.IsNullOrEmpty(postId)) return;

        try
        {
            var post = await CommonShared.ApiHandler.ExecuteRequestAsync<PostResponseDto>(new GetPost(postId));
            MainWindow.SetForegroundWindow();
            MainWindow.Frame.Navigate(typeof(PostPage), post);
        }
        catch { } // The post may have been deleted or hidden; the app stays on the current page.
    }

    // Replays a toast deep link that arrived before login; called once after signing in.
    public static void HandlePending()
    {
        var pendingArguments = _pendingArguments;
        if (pendingArguments == null) return;
        _pendingArguments = null;
        _ = HandleAsync(pendingArguments);
    }
}