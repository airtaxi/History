using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

// Kakao Story message view model: fills the shared message surface from the mail
// list/detail responses. The mail list carries only the summary; the detail is
// fetched lazily when the message is opened.
public partial class KakaoMessageViewModel(MailData.Mail mail) : BaseMessageViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainText))]
    public partial MailData.MailDetail MailDetail { get; private set; }

    public override string Id => mail.id;
    public string SenderId => mail.sender?.id;
    public override string SenderName => mail.sender?.display_name ?? mail.sender?.id;
    public override bool IsSenderAdmin => false;
    public override bool IsSenderModerator => false;
    public override IMediaViewModel SenderProfileMedia => mail.sender?.profile_image_url != null ? new ImageViewModel(mail.sender.profile_image_url) : null;
    public override string ReceiverName => mail.type == "receive" ? "나" : (mail.receivers?.FirstOrDefault()?.display_name ?? string.Empty);
    public override bool IsReceiverAdmin => false;
    public override bool IsReceiverModerator => false;
    public override bool IsUnread => mail.type == "receive" && mail.read_at == null;
    public override string MainText => MailDetail?.content ?? mail.summary;
    public override string ImageUrl => null;
    public override bool HasImage => false;
    public override string TimestampText => KakaoStoryUtils.GetTimeString(mail.created_at);
    public override bool IsReplyButtonVisible => mail.type == "receive" && mail.sender?.id != Shared.KakaoUserId;
    public override bool IsDeleteButtonVisible => true;

    // Kakao Story has no mail read endpoint; mark the item locally as read.
    public void MarkAsReadLocally()
    {
        if (!IsUnread) return;

        mail.read_at = DateTime.UtcNow;
        OnPropertyChanged(nameof(IsUnread));

        if (Shared.KakaoStoryUnreadMailCount > 0) Shared.KakaoStoryUnreadMailCount--;
    }

    public override async Task OpenMessageAsync()
    {
        var mailDetail = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetMailDetail(mail.id));
        if (mailDetail != null)
        {
            MailDetail = mailDetail;
            if (mail.type == "receive" && mail.read_at == null) MarkAsReadLocally();
        }

        var page = new MessagePage(this);
        await App.PushModalAsync(page);
    }

    public override async Task HandleProfileTapAsync()
    {
        // Don't open own profile
        if (mail.sender?.id == Shared.KakaoUserId)
        {
            await Toast.Make("내 프로필입니다").Show();
            return;
        }

        if (mail.sender?.id == null)
        {
            await App.Page.DisplayAlertAsync("안내", "프로필을 불러올 수 없습니다.", Constants.PromptOk);
            return;
        }

        var page = new UserPage(mail.sender.id, true);
        await App.PushAsync(page);
    }

    public override async Task DeleteAsync(bool popModal)
    {
        var confirm = await App.Page.DisplayAlertAsync("쪽지 삭제", "정말로 쪽지를 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        try
        {
            var success = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteMail(mail.id));
            if (!success)
            {
                await App.Page.DisplayAlertAsync("오류", "쪽지 삭제에 실패하였습니다.", Constants.PromptOk);
                return;
            }

            WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<MailData.Mail>(mail));
            if (popModal) await App.PopModalAsync();
        }
        catch (Exception exception)
        {
            await App.Page.DisplayAlertAsync("오류", $"쪽지 삭제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk);
        }
    }
}
