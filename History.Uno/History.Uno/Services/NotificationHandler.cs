using System.Text.Json;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.Uno.DataTypes;

namespace History.Uno.Services;

public static class NotificationHandler
{
    /// <summary>
    /// Called when the user taps a push notification.
    /// If the app shell is not yet loaded, stores the push data for later processing.
    /// If the app is loaded, immediately handles the notification (navigates to the relevant page).
    /// </summary>
    public static void OnNotificationTapped(object sender, Plugin.Firebase.CloudMessaging.EventArgs.FCMNotificationTappedEventArgs e)
    {
        var data = e.Notification.Data;
        var pushData = JsonSerializer.Serialize(data);

        // Check if the app is loaded by verifying the root frame has content.
        // Unlike MAUI's AppShell.IsLoaded, Uno uses the root frame's current page.
        if (App.RootFrame?.Content == null)
        {
            // App not loaded yet — store push data for later processing after login
            Configuration.SetValue("PushData", pushData);
        }
        else
        {
            // App is loaded — handle immediately on the UI thread
            _ = HandlePushNotificationAsync(pushData);
        }
    }

    /// <summary>
    /// Called when a push notification is received while the app is in the foreground.
    /// Updates the in-app data context (posts, users, notifications, friends) without navigating.
    /// </summary>
    public static void OnNotificationReceived(object sender, Plugin.Firebase.CloudMessaging.EventArgs.FCMNotificationReceivedEventArgs e)
        => _ = UpdateNotificationContextAsync(e.Notification.Data);

    /// <summary>
    /// Updates in-app data based on notification payload.
    /// Fetches the relevant post/user, then refreshes notifications and friends lists.
    /// </summary>
    public static async Task UpdateNotificationContextAsync(IDictionary<string, string> data)
    {
        if (data == null) return;
        if (!data.TryGetValue("Type", out var rawType) || !Enum.TryParse<NotificationType>(rawType, out var type)) return;
        if (Shared.ApiHandler == null) return;

        try
        {
            // Fetch the relevant entity based on notification type
            if (data.TryGetValue("PostId", out var postId))
            {
                var post = await Shared.ApiHandler.ExecuteRequestAsync(new GetPost(postId));
                _ = App.MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post)));
            }
            else if (type == NotificationType.FriendRequest && data.TryGetValue("UserId", out var userId))
            {
                var user = await Shared.ApiHandler.ExecuteRequestAsync(new GetUser(userId));
                _ = App.MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    WeakReferenceMessenger.Default.Send(new ValueChangedMessage<UserResponseDto>(user)));
            }

            // Always refresh notifications and friends lists
            var notifications = await Shared.ApiHandler.ExecuteRequestAsync(new GetNotifications());
            _ = App.MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                WeakReferenceMessenger.Default.Send(new NotificationsMessage(notifications)));

            var friends = await Shared.ApiHandler.ExecuteRequestAsync(new GetFriends(Shared.UserId));
            Shared.Friends = friends;
        }
        catch { }
    }

    /// <summary>
    /// Handles a push notification tap by navigating to the relevant page.
    /// Called after the app has loaded (from LoginPage or directly if already loaded).
    /// </summary>
    public static async Task HandlePushNotificationAsync(string pushData)
    {
        // Clear the stored push data
        Configuration.SetValue("PushData", "");

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(pushData);
        if (data == null) return;
        if (!data.TryGetValue("Type", out var rawType)) return;
        if (!Enum.TryParse<NotificationType>(rawType, out var type)) return;

        // TODO: 2-3단계 페이지 이전 후 각 케이스별 페이지 이동 활성화
        // 현재는 데이터 동기화만 수행하고 페이지 이동은 스킵한다.
        // 페이지가 구현되면 아래 switch 문에서 해당 페이지로 이동한다.

        switch (type)
        {
            case NotificationType.FriendRequest:
                // TODO: await App.PushAsync(typeof(UserPage), userId);
                // data.TryGetValue("UserId", out var userId);
                break;

            case NotificationType.Message:
                // TODO: var messageResult = await App.ExecuteRequestAsync(new GetMessage(messageId));
                // TODO: await App.PushAsync(typeof(MessagePage), messageViewModel);
                break;

            case NotificationType.Restriction:
                // TODO: await App.DisplayAlertAsync("제재 내역", data["Body"], Constants.PromptOk, "소명 신청하기");
                break;

            case NotificationType.InviteCodeRequest:
                // TODO: await App.PushAsync(typeof(InviteCodeRequestsPage));
                break;

            case NotificationType.InviteCodeRequestResult:
                // TODO: await App.PushAsync(typeof(InviteCodesPage));
                break;

            default:
                // Comment, CommentMention, CommentLike, Share, Repost, PostReaction, PostMention, FavoriteFriendNewPost, Birthday, Report
                // TODO: var postResult = await App.ExecuteRequestAsync(new GetPost(postId));
                // TODO: await App.PushAsync(typeof(PostPage), postViewModel);
                break;
        }

        // Perform data synchronization regardless
        await UpdateNotificationContextAsync(data);
    }
}