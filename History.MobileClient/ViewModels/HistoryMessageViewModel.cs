using CommunityToolkit.Maui.Alerts;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class HistoryMessageViewModel(MessageResponseDto message) : BaseMessageViewModel
{
    public override string Id => message.Id;
    public UserResponseDto Sender => message.Sender;
    public UserResponseDto Receiver => message.Receiver;

    public List<BaseContent> Contents => message.Contents;
    public DateTime CreatedAt => message.CreatedAt;
    public DateTime? ReadAt => message.ReadAt;
    public override bool IsUnread => Receiver.UserId == CommonShared.UserId && ReadAt == null;

    public override string MainText => Contents.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
    public override string ImageUrl => Contents.OfType<MediaContent>().FirstOrDefault()?.MediaId != null ? Utils.GenerateMediaUri(Contents.OfType<MediaContent>().First().MediaId) : null;
    public override bool HasImage => !string.IsNullOrEmpty(ImageUrl);

    public override string SenderName => Sender?.Nickname ?? Sender?.Handle ?? Sender?.UserId;
    public override bool IsSenderAdmin => Sender?.Rank == Commons.Enums.Rank.Admin;
    public override bool IsSenderModerator => Sender?.Rank == Commons.Enums.Rank.Moderator;
    public override IMediaViewModel SenderProfileMedia => Sender?.UsesAnimatedProfileMedia == true
        ? new ImageViewModel(Utils.GenerateMediaUri(Sender.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
        : new ImageViewModel(Utils.GenerateMediaUri(Sender.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public override string ReceiverName => Receiver?.Nickname ?? Receiver?.Handle ?? Receiver?.UserId;
    public override bool IsReceiverAdmin => Receiver?.Rank == Commons.Enums.Rank.Admin;
    public override bool IsReceiverModerator => Receiver?.Rank == Commons.Enums.Rank.Moderator;
    public override IMediaViewModel ReceiverProfileMedia => Receiver?.UsesAnimatedProfileMedia == true
        ? new ImageViewModel(Utils.GenerateMediaUri(Receiver.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
        : new ImageViewModel(Utils.GenerateMediaUri(Receiver.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public override string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, null);

    public override bool IsReplyButtonVisible => Sender?.UserId != CommonShared.UserId;

    public override async Task OpenMessageAsync() => await App.PushModalAsync(new MessagePage(this));

    public override async Task HandleProfileTapAsync()
    {
        // Don't open own profile
        if (Sender.UserId == CommonShared.UserId)
        {
            await Toast.Make("내 프로필입니다").Show();
            return;
        }

        var page = new BlazorUserPage(Sender.UserId);
        await App.PushModalAsync(page);
    }
}
