using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Message;
using History.Commons.Api.Post;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Messages;
using History.Commons.Enums;
using History.MobileClient.Pages;
using History.Commons;

namespace History.MobileClient.ViewModels;

public partial class HistoryNotificationViewModel : BaseNotificationViewModel
{
    public HistoryNotificationViewModel(NotificationResponseDto notification)
    {
        Notification = notification;

        if (Notification.Type == NotificationType.FriendRequest && Notification.Data != null && Notification.Data.TryGetValue("FriendshipStatus", out var friendshipStatus)) IsAccepted = friendshipStatus != nameof(FriendshipStatus.Waiting);

        WeakReferenceMessenger.Default.Register<NotificationsReadAllMessage>(this, OnNotificationsReadAllMessage);
        WeakReferenceMessenger.Default.Register<NotificationPostReadMessage>(this, OnNotificationPostReadMessage);
        WeakReferenceMessenger.Default.Register<NotificationMessageReadMessage>(this, OnNotificationMessageReadMessage);
        WeakReferenceMessenger.Default.Register<NotificationFriendUserReadMessage>(this, OnNotificationFriendUserReadMessage);
        WeakReferenceMessenger.Default.Register<NotificationTypeReadMessage>(this, OnNotificationTypeReadMessage);
    }

    private void OnNotificationsReadAllMessage(object recipient, NotificationsReadAllMessage message)
    {
        if (!IsUnread) return;
        Notification.IsUnread = false;
        OnPropertyChanged(nameof(IsUnread));
    }

    private void OnNotificationPostReadMessage(object recipient, NotificationPostReadMessage message)
    {
        if (!IsUnread) return;
        if (Notification.Data == null || !Notification.Data.TryGetValue("PostId", out var postId)) return;
        if (postId != message.Value) return;
        Notification.IsUnread = false;
        OnPropertyChanged(nameof(IsUnread));
    }

    private void OnNotificationMessageReadMessage(object recipient, NotificationMessageReadMessage message)
    {
        if (!IsUnread) return;
        if (Notification.Data == null || !Notification.Data.TryGetValue("MessageId", out var messageId)) return;
        if (messageId != message.Value) return;
        Notification.IsUnread = false;
        OnPropertyChanged(nameof(IsUnread));
    }

    private void OnNotificationFriendUserReadMessage(object recipient, NotificationFriendUserReadMessage message)
    {
        if (!IsUnread) return;
        if (Notification.Type != NotificationType.FriendRequest) return;
        if (Notification.Data == null || !Notification.Data.TryGetValue("UserId", out var userId)) return;
        if (userId != message.Value) return;
        Notification.IsUnread = false;
        OnPropertyChanged(nameof(IsUnread));
    }

    private void OnNotificationTypeReadMessage(object recipient, NotificationTypeReadMessage message)
    {
        if (!IsUnread) return;
        if (Notification.Type != message.Value) return;
        Notification.IsUnread = false;
        OnPropertyChanged(nameof(IsUnread));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Body))]
    [NotifyPropertyChangedFor(nameof(IsBodyVisible))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    [NotifyPropertyChangedFor(nameof(ImageMedia))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(IsUnread))]
    public partial NotificationResponseDto Notification { get; private set; }

    public override bool IsUnread => Notification.IsUnread;
    public override string Title => Notification.Title;
    public override string Body => Notification.Body;
    public override bool IsBodyVisible => !string.IsNullOrEmpty(Notification.Body);
    public override string TimestampText => Utils.GenerateFriendlyTimestamp(Notification.CreatedAt, null);
    public override ImageViewModel ImageMedia => !string.IsNullOrEmpty(Notification.ImageUrl) ? new(Notification.ImageUrl) { Aspect = Aspect.AspectFill } : null;
    public override bool IsImageVisible => !string.IsNullOrEmpty(Notification.ImageUrl) && Notification.Type != NotificationType.FriendRequest && !IsAcceptButtonVisible;
    public override bool IsFriendRequest => Notification.Type == NotificationType.FriendRequest;
    public override bool IsAcceptButtonVisible => IsFriendRequest && !IsAccepted;

    public override IMediaViewModel ProfileMedia => Notification.User.UsesAnimatedProfileMedia
        ? new ImageViewModel(Utils.GenerateMediaUri(Notification.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
        : new ImageViewModel(Utils.GenerateMediaUri(Notification.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public override async Task HandleTapAsync()
    {
        if (Notification.Data == null) return;
        var type = Notification.Type;

        if (type == NotificationType.Restriction)
        {
            _ = MarkAsReadAsync();
            var accept = await App.Page.DisplayAlertAsync("제재 내역", Notification.Body, Constants.PromptOk, "소명 신청하기");
            if (!accept)
            {
                var copy = await App.Page.DisplayAlertAsync("알림", "공식 디스코드에서 소명 신청을 받고 있습니다.", "디스코드 초대 URL 복사", "확인");
                if (copy)
                {
                    await Clipboard.SetTextAsync(Constants.DiscordInviteUrl);
                    await Toast.Make("디스코드 초대 URL이 클립보드에 복사되었습니다.").Show();
                }
            }
        }
        else if (type == NotificationType.Message)
        {
            if (!Notification.Data.TryGetValue("MessageId", out var messageId)) return;

            var messageResult = await App.ExecuteRequestAsync(new GetMessage(messageId));
            if (!messageResult.IsSuccess) return;

            var viewModel = new HistoryMessageViewModel(messageResult.Value);
            await App.PushModalAsync(new MessagePage(viewModel));
        }
        else if (type == NotificationType.FriendRequest)
        {
            if (!Notification.Data.TryGetValue("UserId", out var userId)) return;

            var page = new BlazorUserPage(userId);
            await App.PushAsync(page);
        }
        else if (type == NotificationType.InviteCodeRequest) await App.PushAsync(new InviteCodeRequestsPage());
        else if (type == NotificationType.InviteCodeRequestResult) await App.PushAsync(new InviteCodesPage());
        else
        {
            if (!Notification.Data.TryGetValue("PostId", out var postId)) return;

            var postResult = await App.ExecuteRequestAsync(new GetPost(postId));
            if (!postResult.IsSuccess) return;

            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(postResult.Value));
            var post = postResult.Value;
            var viewModel = new HistoryPostViewModel(post, PostType.Unwrapped);
            var page = new PostPage(viewModel);
            await App.PushAsync(page);
        }
    }

    public override async Task HandleProfileTapAsync()
    {
        var profilePage = new BlazorUserPage(Notification.User.UserId);
        await App.PushAsync(profilePage);
    }

    public override async Task AcceptFriendRequestAsync()
    {
        if (Notification.Type != NotificationType.FriendRequest) return;
        if (!Notification.Data.TryGetValue("UserId", out var userId)) return;

        var result = await App.ExecuteRequestAsync(new AcceptFriendRequest(userId));
        if (result.IsSuccess)
        {
            IsAccepted = true;
            await LoginPage.RefreshFriendsAsync();
            WeakReferenceMessenger.Default.Send(new FriendshipChangedMessage(userId, FriendshipStatus.Accepted, Notification.User));
            await Toast.Make("친구 요청을 수락했습니다.").Show();
        }
    }

    public override async Task MarkAsReadAsync()
    {
        if (!IsUnread) return;

        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotifications([Notification.Id]));
        if (success)
        {
            Notification.IsUnread = false;
            OnPropertyChanged(nameof(IsUnread));
        }
    }
}
