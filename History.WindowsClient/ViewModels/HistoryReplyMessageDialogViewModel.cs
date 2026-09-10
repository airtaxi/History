using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels;

public partial class HistoryReplyMessageDialogViewModel(BaseViewModel baseViewModel, string receiverId, string receiverName, ImageSource receiverProfileImage = null) : BaseMessageDialogViewModel(baseViewModel)
{
    public string ReceiverName { get; } = receiverName;
    public ImageSource ReceiverProfileImage { get; } = receiverProfileImage;

    protected override string ReceiverId => receiverId;
}
