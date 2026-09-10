using History.Commons;
using History.WindowsClient.Helpers;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels;

// Kakao Story reply-message dialog: replies are sent through the Kakao Story message API.
public partial class KakaoReplyMessageDialogViewModel(BaseViewModel baseViewModel, string receiverId, string receiverName, ImageSource receiverProfileImage = null) : HistoryReplyMessageDialogViewModel(baseViewModel, receiverId, receiverName, receiverProfileImage)
{
    public override bool IsAttachmentAvailable => false;

    protected override async Task<Result> SendContentsAsync(string text) => await KakaoStoryUtils.SendMailAsync(BaseViewModel, ReceiverId, text);
}
