using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Message;
using History.Commons.Api.Post;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Enums;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class NotificationViewModel : ObservableObject
{
    public NotificationViewModel(NotificationResponseDto notification)
    {
        Notification = notification;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAcceptButtonVisible))]
    public partial bool IsAccepted { get; private set; }

    public bool IsUnread => Notification.IsUnread;

    public string Title => Notification.Title;
    public string Body => Notification.Body;
    public bool IsBodyVisible => !string.IsNullOrEmpty(Notification.Body);
    public string TimestampText => Utils.GenerateFriendlyTimestamp(Notification.CreatedAt, null);
    public ImageViewModel ImageMedia => !string.IsNullOrEmpty(Notification.ImageUrl) ? new(Notification.ImageUrl) { Aspect = Aspect.AspectFill } : null;
    public bool IsImageVisible => !string.IsNullOrEmpty(Notification.ImageUrl) && Notification.Type != NotificationType.FriendRequest && !IsAcceptButtonVisible;
    public bool IsFriendRequest => Notification.Type == NotificationType.FriendRequest;
    public bool IsAcceptButtonVisible => IsFriendRequest && !IsAccepted;

    public IMediaViewModel ProfileMedia => Notification.User.UsesAnimatedProfileMedia
        ? new ImageViewModel(Utils.GenerateMediaUri(Notification.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
        : new ImageViewModel(Utils.GenerateMediaUri(Notification.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        if(Notification.Data == null) return;
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

            var viewModel = new MessageViewModel(messageResult.Value);
            await App.PushModalAsync(new MessagePage(viewModel));
        }
        else if (type == NotificationType.FriendRequest)
        {
            if (!Notification.Data.TryGetValue("UserId", out var userId)) return;

            var page = new UserPage(userId);
            await App.PushAsync(page);
        }
        else if (type == NotificationType.InviteCodeRequest) await App.PushAsync(new InviteCodeRequestsPage());
        else if (type == NotificationType.InviteCodeRequestResult) await App.PushAsync(new InviteCodesPage());
        else
        {
            if (!Notification.Data.TryGetValue("PostId", out var postId)) return;

            var postResult = await App.ExecuteRequestAsync(new GetPost(postId));
            if (!postResult.IsSuccess) return;

            var post = postResult.Value;
            var viewModel = new HistoryPostViewModel(post, PostType.Unwrapped);
            var page = new PostPage(viewModel);
            await App.PushAsync(page);
        }
    }

    [RelayCommand]
    public async Task HandleProfileTapAsync()
    {
        var profilePage = new UserPage(Notification.User.UserId);
        await App.PushAsync(profilePage);
    }

    [RelayCommand]
    public async Task AcceptFriendRequestAsync()
    {
        if (Notification.Type != NotificationType.FriendRequest) return;
        if (!Notification.Data.TryGetValue("UserId", out var userId)) return;

        var result = await App.ExecuteRequestAsync(new AcceptFriendRequest(userId));
        if (result.IsSuccess)
        {
            IsAccepted = true;
            await Toast.Make("친구 요청을 수락했습니다.").Show();
        }
    }

    public async Task MarkAsReadAsync()
    {
        if (!IsUnread) return;

        var success = await Shared.ApiHandler.TryExecuteRequestAsync(new ReadNotifications([Notification.Id]));
        if (success)
        {
            Notification.IsUnread = false;
            OnPropertyChanged(nameof(IsUnread));
        }
    }
}
