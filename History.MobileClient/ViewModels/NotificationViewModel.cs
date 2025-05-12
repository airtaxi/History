using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Post;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Pages;
using Java.Security.Acl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        if (type == NotificationType.FriendRequest)
        {
            if(!Notification.Data.TryGetValue("UserId", out var userId)) return;

            var page = new UserPage(userId);
            await App.PushModalAsync(page);
        }
        else
        {
            if (!Notification.Data.TryGetValue("PostId", out var postId)) return;

            var postResult = await App.ExecuteRequestAsync(new GetPost(postId));
            if (!postResult.IsSuccess) return;

            var post = postResult.Value;
            var viewModel = new PostViewModel(post, false);
            var page = new PostPage(viewModel);
            await App.PushModalAsync(page);
        }
    }

    [RelayCommand]
    public async Task HandleProfileTapAsync()
    {
        var profilePage = new UserPage(Notification.User.UserId);
        await App.PushModalAsync(profilePage);
    }
}
