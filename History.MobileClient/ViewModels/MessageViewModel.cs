using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class MessageViewModel : ObservableObject
{
    private MessageResponseDto _message;

    public string Id => _message.Id;
    public UserResponseDto Sender => _message.Sender;
    public UserResponseDto Receiver => _message.Receiver;

    public List<BaseContent> Contents => _message.Contents;
    public DateTime CreatedAt => _message.CreatedAt;
    public DateTime? ReadAt => _message.ReadAt;
    public bool IsUnread => Receiver.UserId == Shared.UserId && ReadAt == null;

    public string MainText => Contents.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
    public string ImageUrl => Contents.OfType<MediaContent>().FirstOrDefault()?.MediaId != null ? Utils.GenerateMediaUri(Contents.OfType<MediaContent>().First().MediaId) : null;
    public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

    public string SenderName => Sender?.Nickname ?? Sender?.Handle ?? Sender?.UserId;
    public bool IsSenderAdmin => Sender?.Rank == Commons.Enums.Rank.Admin;
    public bool IsSenderModerator => Sender?.Rank == Commons.Enums.Rank.Moderator;
    public IMediaViewModel SenderProfileMedia => Sender.UsesAnimatedProfileMedia
        ? new ImageViewModel(Utils.GenerateMediaUri(Sender.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
        : new ImageViewModel(Utils.GenerateMediaUri(Sender.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public string ReceiverName => Receiver?.Nickname ?? Receiver?.Handle ?? Receiver?.UserId;
    public bool IsReceiverAdmin => Receiver?.Rank == Commons.Enums.Rank.Admin;
    public bool IsReceiverModerator => Receiver?.Rank == Commons.Enums.Rank.Moderator;
    public IMediaViewModel ReceiverProfileMedia => Receiver.UsesAnimatedProfileMedia
        ? new ImageViewModel(Utils.GenerateMediaUri(Receiver.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
        : new ImageViewModel(Utils.GenerateMediaUri(Receiver.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, null);

    public MessageViewModel(MessageResponseDto message) => _message = message;

    [RelayCommand]
    public async Task OpenMessageAsync() => await App.PushModalAsync(new MessagePage(this));

    [RelayCommand]
    public async Task HandleProfileTapAsync()
    {
        // Don't open own profile
        if (Sender.UserId == Shared.UserId)
        {
            await Toast.Make("내 프로필입니다").Show();   
            return;
        }

        var page = new UserPage(Sender.UserId);
        await App.PushModalAsync(page);
    }
}
