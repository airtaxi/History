using History.Commons;
using History.WindowsClient.Helpers;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels.Message;

// Kakao Story reply-message dialog: replies are sent through the Kakao Story message API.
public partial class KakaoReplyMessageDialogViewModel(BaseViewModel baseViewModel, string receiverId, string receiverName, ImageSource receiverProfileImage = null) : HistoryReplyMessageDialogViewModel(baseViewModel, receiverId, receiverName, receiverProfileImage)
{
    public override bool IsAttachmentAvailable => false;
    public override bool IsKakaoStoryMode => true;

    protected override async Task<Result> SendContentsAsync(string text) => await KakaoStoryUtils.SendMailAsync(BaseViewModel, ReceiverId, text);
}
