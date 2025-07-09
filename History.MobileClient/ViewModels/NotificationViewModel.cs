using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Message;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class NotificationViewModel(NotificationResponseDto notification) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Body))]
    [NotifyPropertyChangedFor(nameof(IsBodyVisible))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    [NotifyPropertyChangedFor(nameof(ImageMedia))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    public partial NotificationResponseDto Notification { get; private set; } = notification;

    public string Title => Notification.Title;
    public string Body => Notification.Body;
    public bool IsBodyVisible => !string.IsNullOrEmpty(Notification.Body);
    public string TimestampText => Utils.GenerateFriendlyTimestamp(Notification.CreatedAt, null);
    public ImageViewModel ImageMedia => !string.IsNullOrEmpty(Notification.ImageUrl) ? new(Notification.ImageUrl) { Aspect = Aspect.AspectFill } : null;
    public bool IsImageVisible => !string.IsNullOrEmpty(Notification.ImageUrl) && Notification.Type != NotificationType.FriendRequest;
    public IMediaViewModel ProfileMedia => Notification.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(Notification.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(Notification.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        if(Notification.Data == null) return;
        var type = Notification.Type;

        if (type == NotificationType.Restriction)
        {
            var accept = await App.Page.DisplayAlert("제재 내역", Notification.Body, Constants.PromptOk, "소명 신청하기");
            if (!accept)
            {
                var copy = await App.Page.DisplayAlert("알림", "공식 디스코드에서 소명 신청을 받고 있습니다.", "디스코드 초대 URL 복사", "확인");
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
        else
        {
            if (!Notification.Data.TryGetValue("PostId", out var postId)) return;

            var postResult = await App.ExecuteRequestAsync(new GetPost(postId));
            if (!postResult.IsSuccess) return;

            var post = postResult.Value;
            var viewModel = new PostViewModel(post, PostType.Unwrapped);
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
}
