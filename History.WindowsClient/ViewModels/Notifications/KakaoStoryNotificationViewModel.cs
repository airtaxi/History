using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels.Notifications;

// Kakao Story notification list item view model: holds the Kakao notification DTO and the
// display surface used by the notifications flyout template. Kakao Story has no read
// endpoint, so the unread marker clears locally only.
public partial class KakaoStoryNotificationViewModel : BaseNotificationViewModel
{
    private readonly BaseViewModel _baseViewModel;

    public Notification Notification { get; }

    public override string Title => Notification.message ?? string.Empty;
    public override string Body => Notification.content ?? string.Empty;
    public override bool IsBodyVisible => !string.IsNullOrEmpty(Notification.content);
    public override string TimestampText => KakaoStoryUtils.GetTimeString(Notification.created_at);
    // Kakao Story notification keys classify the notification type; "invt:" is a friend request.
    public bool IsFriendRequest => Notification.key?.StartsWith("invt:", StringComparison.OrdinalIgnoreCase) == true;
    public override bool IsImageVisible => !string.IsNullOrEmpty(Notification.thumbnail_url) && !IsFriendRequest;

    public override ImageSource ProfileImageSource => Notification.actor?.profile_image_url != null ? new BitmapImage(new Uri(Notification.actor.profile_image_url)) : Notification.actor?.profile_thumbnail_url != null ? new BitmapImage(new Uri(Notification.actor.profile_thumbnail_url)) : null;
    public override ImageSource ImageSource => string.IsNullOrEmpty(Notification.thumbnail_url) ? null : new BitmapImage(new Uri(Notification.thumbnail_url));

    public KakaoStoryNotificationViewModel(Notification notification, BaseViewModel baseViewModel) : base(notification.is_new)
    {
        _baseViewModel = baseViewModel;
        Notification = notification;
    }

    // Entry point for notification taps: navigates to the notification target through the
    // deep link scheme and marks the notification as read locally.
    public override async Task HandleTapAsync()
    {
        var scheme = Notification.scheme;
        if (scheme == null)
        {
            await _baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "알림 대상 게시글을 찾을 수 없습니다."));
            return;
        }

        // Post notification (e.g. comment/emotion/UP): the scheme contains the activity id after "activities/".
        var postId = GetPostId();
        if (postId != null)
        {
            try
            {
                var post = await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(postId));
                if (post != null)
                {
                    WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(post));
                    SetUnread(false); // The fetch above already marked the notification as read.
                    _baseViewModel.RequestNavigation(typeof(PostPage), post);
                }
                else await _baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "해당 게시글을 불러올 수 없습니다."));
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리 API 오류가 발생하였습니다: {KakaoStoryUtils.GetApiErrorMessage(exception)}")); }
        }
        // Profile notification (e.g. friend request): the scheme is a kakaostory:// deep link to the profile.
        else if (scheme.Contains("kakaostory://profiles/"))
        {
            var profileId = scheme.Replace("kakaostory://profiles/", "");
            if (string.IsNullOrEmpty(profileId)) await _baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "프로필을 불러올 수 없습니다."));
            // "myfollowers" is a Kakao Story deep link to the my-followers page, which is
            // not implemented in this app yet.
            else if (profileId == "myfollowers") await _baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "소식받는 친구 목록 조회는 아직 지원하지 않습니다."));
            else
            {
                _ = MarkAsReadAsync();
                _baseViewModel.RequestNavigation(typeof(ProfilePage), new KakaoProfileParameters(profileId));
            }
        }
        else await _baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "아직 지원하지 않는 알림입니다."));
    }

    // Extracts the post id from a post notification scheme (e.g. "activities/{postId}?...").
    // Returns null when the notification does not target a post.
    public string GetPostId()
    {
        var scheme = Notification.scheme;
        if (scheme == null || !scheme.Contains("?profile_id=") || !scheme.Contains("activities/")) return null;

        var postId = scheme.Split(new[] { "activities/" }, StringSplitOptions.None)[1];
        var queryIndex = postId.IndexOf('?');
        if (queryIndex >= 0) postId = postId[..queryIndex];
        return postId;
    }

    // Kakao Story marks a notification as read server-side when its target is fetched, so
    // the read request performs the same API call as the tap handler without navigating
    // anywhere. The unread marker clears only after the fetch confirms the target exists.
    public override async Task MarkAsReadAsync()
    {
        if (!IsUnread) return;

        var scheme = Notification.scheme;
        if (scheme == null)
        {
            SetUnread(false);
            return;
        }

        try
        {
            var postId = GetPostId();
            if (postId != null)
            {
                var post = await KakaoStoryApiHandler.GetPost(postId);
                if (post == null) return;
            }
            else if (scheme.Contains("kakaostory://profiles/"))
            {
                var profileId = scheme.Replace("kakaostory://profiles/", "");
                if (string.IsNullOrEmpty(profileId)) return;

                var profileObject = await KakaoStoryApiHandler.GetProfileFeed(profileId, null);
                if (profileObject?.profile == null) return;
            }
            else
            {
                SetUnread(false);
                return;
            }
        }
        catch (Exception) { return; }

        SetUnread(false);
    }

    // Keeps the DTO and the bindable surface in sync when the notification is marked as read.
    protected override void OnUnreadStateChanged(bool value) => Notification.is_new = value;
}
