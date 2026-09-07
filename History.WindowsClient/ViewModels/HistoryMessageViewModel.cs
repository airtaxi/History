using History.Commons;
using History.Commons.Api.Message;
using History.Commons.Api.User;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

public partial class HistoryMessageViewModel : BaseMessageViewModel
{
    public MessageResponseDto Message { get; }

    public HistoryMessageViewModel(MessageResponseDto message, BaseViewModel baseViewModel) : base(baseViewModel)
    {
        Message = message;
        Update(message);
    }

    private void Update(MessageResponseDto message)
    {
        Id = message.Id;

        var sender = message.Sender;
        SenderName = sender?.Nickname ?? sender?.Handle ?? sender?.UserId ?? string.Empty;
        IsSenderAdmin = sender?.Rank == Rank.Admin;
        IsSenderModerator = sender?.Rank == Rank.Moderator;

        if (sender?.ProfileThumbnailMediaId != null) ProfileImageSource = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(sender.ProfileThumbnailMediaId)));
        else if (sender?.ProfileMediaId != null) ProfileImageSource = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(sender.ProfileMediaId)));

        var receiver = message.Receiver;
        ReceiverName = receiver?.Nickname ?? receiver?.Handle ?? receiver?.UserId ?? string.Empty;
        IsReceiverAdmin = receiver?.Rank == Rank.Admin;
        IsReceiverModerator = receiver?.Rank == Rank.Moderator;

        if (receiver?.ProfileThumbnailMediaId != null) ReceiverProfileImageSource = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(receiver.ProfileThumbnailMediaId)));
        else if (receiver?.ProfileMediaId != null) ReceiverProfileImageSource = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(receiver.ProfileMediaId)));

        MainText = message.Contents.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
        TimestampText = PostHelper.GenerateFriendlyTimestamp(message.CreatedAt, null);
        DetailedTimestampText = message.CreatedAt.ToLocalTime().ToString("yyyy년 M월 d일 tt h:mm");

        var mediaContent = message.Contents.OfType<MediaContent>().FirstOrDefault();
        if (mediaContent?.MediaId != null)
        {
            var mediaUrl = CommonUtils.GenerateMediaUri(mediaContent.MediaId);
            ImageSource = new BitmapImage(new Uri(mediaUrl));
            HasImage = true;
            MainTextForeground = new SolidColorBrush(Colors.White);
        }
        else
        {
            HasImage = false;
            ImageSource = null;
            MainTextForeground = Application.Current?.Resources["TextFillColorPrimaryBrush"] as Brush ?? new SolidColorBrush(Colors.White);
        }

        IsUnread = receiver?.UserId == CommonShared.UserId && message.ReadAt == null;
        IsReplyButtonVisible = sender?.UserId != CommonShared.UserId;
        ReplyButtonText = IsReplyButtonVisible ? "답장하기" : string.Empty;
    }

    public override async Task HandleTapAsync()
    {
        if (IsUnread && Message.Receiver?.UserId == CommonShared.UserId && Message.ReadAt == null)
        {
            var result = await BaseViewModel.ExecuteRequestAsync(new MarkMessageAsRead(Id));
            if (result.IsSuccess)
            {
                IsUnread = false;
                _ = CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByMessageId(Id));
            }
        }

        var dialog = new ViewMessageDialog(this);
        var dialogResult = await BaseViewModel.ShowContentDialogAsync(dialog);
        if (dialogResult == ContentDialogResult.Primary) await ReplyAsync();
    }

    public override async Task ReplyAsync()
    {
        var senderId = Message?.Sender?.UserId;
        if (string.IsNullOrEmpty(senderId)) return;

        var dialogViewModel = new ReplyMessageDialogViewModel(BaseViewModel, senderId, SenderName, ProfileImageSource);
        var dialog = new ReplyMessageDialog(dialogViewModel);
        await BaseViewModel.ShowContentDialogAsync(dialog);
    }
}
