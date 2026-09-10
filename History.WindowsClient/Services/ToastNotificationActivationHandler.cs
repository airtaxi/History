using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
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

        if (type == NotificationType.KakaoStory)
        {
            await HandleKakaoStoryAsync(parameters);
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

    // Routes Kakao Story toast arguments to the notification target using the same scheme
    // classification as the notifications flyout: an "activities/" scheme targets a post and
    // a "kakaostory://profiles/" deep link targets a profile. A missing Kakao Story session
    // shows the login flow first.
    private static async Task HandleKakaoStoryAsync(NameValueCollection parameters)
    {
        var scheme = parameters["Scheme"];
        if (string.IsNullOrEmpty(scheme))
        {
            await ShowMessageDialogAsync("안내", "알림 대상 게시글을 찾을 수 없습니다.");
            return;
        }

        // Post notification (e.g. comment/emotion/UP): the scheme contains the activity id after "activities/".
        var postId = GetKakaoPostId(scheme);
        if (postId != null)
        {
            try
            {
                if (!await KakaoStoryUtils.EnsureLoggedInAsync()) return;
                var post = await KakaoStoryApiHandler.GetPost(postId);
                if (post != null) NavigateToPage(typeof(PostPage), post);
                else await ShowMessageDialogAsync("안내", "해당 게시글을 불러올 수 없습니다.");
            }
            catch (Exception exception) { await ShowMessageDialogAsync(Constants.ErrorTitle, $"카카오스토리 API 오류가 발생하였습니다: {KakaoStoryUtils.GetApiErrorMessage(exception)}"); }
            return;
        }

        // Profile notification (e.g. friend request): the scheme is a kakaostory:// deep link to the profile.
        if (scheme.StartsWith("kakaostory://profiles/", StringComparison.OrdinalIgnoreCase))
        {
            var profileId = scheme["kakaostory://profiles/".Length..];
            if (string.IsNullOrEmpty(profileId)) await ShowMessageDialogAsync("안내", "프로필을 불러올 수 없습니다.");
            // "myfollowers" is a Kakao Story deep link to the my-followers page, which is
            // not implemented in this app yet.
            else if (profileId == "myfollowers") await ShowMessageDialogAsync("안내", "소식받는 친구 목록 조회는 아직 지원하지 않습니다.");
            else NavigateToPage(typeof(ProfilePage), new KakaoProfileParameters(profileId));
            return;
        }

        await ShowMessageDialogAsync("안내", "아직 지원하지 않는 알림입니다.");
    }

    // Extracts the post id from a post notification scheme (e.g. "activities/{postId}?...").
    // Returns null when the scheme does not target a post.
    private static string GetKakaoPostId(string scheme)
    {
        if (!scheme.Contains("?profile_id=") || !scheme.Contains("activities/")) return null;

        var postId = scheme.Split(new[] { "activities/" }, StringSplitOptions.None)[1];
        var queryIndex = postId.IndexOf('?');
        if (queryIndex >= 0) postId = postId[..queryIndex];
        return postId;
    }

    // Focuses the window and navigates its root frame, marshalling to the UI thread because
    // the Kakao Story API continuation can resume on a background thread.
    private static void NavigateToPage(Type pageType, object parameter)
    {
        MainWindow.SetForegroundWindow();
        var frame = MainWindow.Frame;
        if (frame.DispatcherQueue.HasThreadAccess) frame.Navigate(pageType, parameter);
        else frame.DispatcherQueue.TryEnqueue(() => frame.Navigate(pageType, parameter));
    }

    // Shows a message dialog on the window's UI thread so callers on any continuation thread
    // can report toast processing failures.
    private static async Task ShowMessageDialogAsync(string title, string message)
    {
        var frame = MainWindow.Frame;
        if (frame.DispatcherQueue.HasThreadAccess)
        {
            await frame.ShowMessageDialogAsync(new MessageDialogParameters(title, message));
            return;
        }

        var taskCompletionSource = new TaskCompletionSource<ContentDialogResult>();
        frame.DispatcherQueue.TryEnqueue(async () => taskCompletionSource.TrySetResult(await frame.ShowMessageDialogAsync(new MessageDialogParameters(title, message))));
        await taskCompletionSource.Task;
    }
}