using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.KakaoStory;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels;

// Kakao Story message view model: fills the shared message surface from the mail list
// response. The list carries only the summary, so the detail is fetched when the mail is
// opened; read state is tracked locally because Kakao Story has no mail read endpoint.
public partial class KakaoMessageViewModel : BaseMessageViewModel
{
    private readonly BaseViewModel _baseViewModel;

    public MailData.Mail Mail { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainText))]
    public partial MailData.MailDetail MailDetail { get; private set; }

    public KakaoMessageViewModel(MailData.Mail mail, BaseViewModel baseViewModel) : base(baseViewModel)
    {
        _baseViewModel = baseViewModel;
        Mail = mail;
        Update();
    }

    private void Update()
    {
        // The list response carries only the summary; the fetched detail is preferred because
        // it can hold the sender profile image the summary omitted.
        var sender = MailDetail?.sender ?? Mail.sender;

        Id = Mail.id;
        SenderName = sender?.display_name ?? sender?.id;
        IsSenderAdmin = false;
        IsSenderModerator = false;
        // The mail response can omit the sender image, so the friend cache fills it in.
        ProfileImageSource = CreateProfileImageSource(sender?.profile_thumbnail_url ?? sender?.profile_image_url ?? CommonShared.KakaoFriends?.FirstOrDefault(friend => friend.id == sender?.id)?.profile_thumbnail_url);

        var receiver = Mail.receivers?.FirstOrDefault();
        ReceiverName = Mail.type == "receive" ? "나" : receiver?.display_name ?? string.Empty;
        IsReceiverAdmin = false;
        IsReceiverModerator = false;

        // A received mail's recipient is the logged-in user and the mail list response does not
        // embed that profile, so the session cache supplies the image; sent mails carry the
        // recipient, with the friend cache as a fallback when it is missing.
        ReceiverProfileImageSource = CreateProfileImageSource(receiver?.profile_thumbnail_url ?? receiver?.profile_image_url ?? (Mail.type == "receive" ? CommonShared.KakaoProfileImageUrl : CommonShared.KakaoFriends?.FirstOrDefault(friend => friend.id == receiver?.id)?.profile_thumbnail_url));

        MainText = MailDetail?.content ?? Mail.summary;
        // Kakao Story timestamps are UTC; shift them into the local wall-clock time for display.
        var localCreatedAt = Mail.created_at.AddHours(DateTimeOffset.Now.Offset.TotalHours);
        TimestampText = KakaoStoryUtils.GetTimeString(Mail.created_at);
        DetailedTimestampText = localCreatedAt.ToString("yyyy년 M월 d일 tt h:mm");

        HasImage = false;
        ImageSource = null;
        MainTextForeground = Application.Current?.Resources["TextFillColorPrimaryBrush"] as Brush ?? new SolidColorBrush(Colors.White);

        IsUnread = Mail.type == "receive" && Mail.read_at == null;
        IsReplyButtonVisible = Mail.type == "receive" && Mail.sender?.id != CommonShared.KakaoUserId;
        ReplyButtonText = IsReplyButtonVisible ? "답장하기" : string.Empty;
    }

    // Kakao Story has no mail read endpoint; mark the item locally as read.
    public void MarkAsReadLocally()
    {
        if (!IsUnread) return;

        Mail.read_at = DateTime.UtcNow;
        IsUnread = false;
    }

    private static ImageSource CreateProfileImageSource(string imageUrl) => imageUrl != null ? new BitmapImage(new Uri(imageUrl)) : null;

    public override async Task HandleTapAsync()
    {
        var mailDetail = await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetMailDetail(Mail.id));
        if (mailDetail != null)
        {
            MailDetail = mailDetail;
            Update(); // The detail may carry the sender profile image the summary omitted.
            MarkAsReadLocally();
        }

        var dialog = new ViewMessageDialog(this);
        var dialogResult = await _baseViewModel.ShowContentDialogAsync(dialog);
        if (dialogResult == ContentDialogResult.Primary) await ReplyAsync();
    }

    public override async Task ReplyAsync()
    {
        var senderId = Mail.sender?.id;
        if (string.IsNullOrEmpty(senderId)) return;

        var dialogViewModel = new KakaoReplyMessageDialogViewModel(_baseViewModel, senderId, SenderName, ProfileImageSource);
        var dialog = new ReplyMessageDialog(dialogViewModel);
        await _baseViewModel.ShowContentDialogAsync(dialog);
    }
}
