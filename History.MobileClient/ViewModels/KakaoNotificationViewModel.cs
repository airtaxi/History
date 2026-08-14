using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public partial class KakaoNotificationViewModel : BaseNotificationViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnread))]
    public partial Notification Notification { get; private set; }

    public KakaoNotificationViewModel(Notification notification)
    {
        Notification = notification;
    }

    public override bool IsUnread => Notification.is_new;
    public override string Title => Notification.message ?? string.Empty;
    public override string Body => Notification.content ?? string.Empty;
    public override bool IsBodyVisible => !string.IsNullOrEmpty(Notification.content);
    public override string TimestampText => KakaoStoryUtils.GetTimeString(Notification.created_at);
    public override ImageViewModel ImageMedia => !string.IsNullOrEmpty(Notification.thumbnail_url) ? new ImageViewModel(Notification.thumbnail_url) { Aspect = Aspect.AspectFill } : null;
    // Kakao Story notification keys classify the notification type; "invt:" is a friend request.
    public override bool IsFriendRequest => Notification.key?.StartsWith("invt:", StringComparison.OrdinalIgnoreCase) == true;
    public override bool IsImageVisible => ImageMedia != null && !IsFriendRequest;

    public override IMediaViewModel ProfileMedia => Notification.actor?.profile_image_url != null
        ? new ImageViewModel(Notification.actor.profile_image_url)
        : Notification.actor?.profile_thumbnail_url != null ? new ImageViewModel(Notification.actor.profile_thumbnail_url) : null;

    public override async Task HandleTapAsync()
    {
        var scheme = Notification.scheme;
        if (scheme == null)
        {
            await App.Page.DisplayAlertAsync("안내", "알림 대상 게시글을 찾을 수 없습니다.", Constants.PromptOk);
            return;
        }

        // Post notification (e.g. comment/emotion/UP): scheme contains the activity id after "activities/".
        if (scheme.Contains("?profile_id=") && scheme.Contains("activities/"))
        {
            var postId = scheme.Split(new[] { "activities/" }, StringSplitOptions.None)[1];
            var post = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(postId));
            if (post != null)
            {
                var postViewModel = new KakaoPostViewModel(post, PostType.Unwrapped);
                var postPage = new PostPage(postViewModel);
                await App.PushAsync(postPage);
            }
            else await App.Page.DisplayAlertAsync("안내", "해당 게시글을 불러올 수 없습니다.", Constants.PromptOk);
        }
        // Profile notification (e.g. friend request): scheme is a kakaostory:// deep link to the profile.
        else if (scheme.Contains("kakaostory://profiles/"))
        {
            var profileId = scheme.Replace("kakaostory://profiles/", "");
            if (string.IsNullOrEmpty(profileId)) await App.Page.DisplayAlertAsync("안내", "프로필을 불러올 수 없습니다.", Constants.PromptOk);
            else await App.PushAsync(new BlazorUserPage(profileId, true));
        }
        else await App.Page.DisplayAlertAsync("안내", "아직 지원하지 않는 알림입니다.", Constants.PromptOk);
    }

    public override async Task HandleProfileTapAsync()
    {
        var profileId = Notification.actor?.id;
        if (profileId == null)
        {
            await App.Page.DisplayAlertAsync("안내", "프로필을 불러올 수 없습니다.", Constants.PromptOk);
            return;
        }

        var profilePage = new BlazorUserPage(profileId, true);
        await App.PushAsync(profilePage);
    }

    // Mirror HistoryNotificationViewModel: accept the friend request from the row.
    public override async Task AcceptFriendRequestAsync()
    {
        if (!IsFriendRequest) return;

        var userId = Notification.actor?.id;
        if (userId == null)
        {
            await App.Page.DisplayAlertAsync("안내", "친구 요청을 보낸 사용자를 찾을 수 없습니다.", Constants.PromptOk);
            return;
        }

        try
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.AcceptFriendRequest(userId, false));
            IsAccepted = true;
            await Toast.Make("친구 요청을 수락했습니다.").Show();
        }
        catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 요청 수락에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
    }

    // Kakao Story has no notification read endpoint; mark the item locally as read.
    public override async Task MarkAsReadAsync()
    {
        if (!IsUnread) return;

        Notification.is_new = false;
        OnPropertyChanged(nameof(IsUnread));
    }
}
