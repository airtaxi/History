using CommunityToolkit.Mvvm.Messaging.Messages;
using History.WindowsClient.ViewModels.Media;

namespace History.WindowsClient.Messages;

public class MediaImageSizeReportedMessage(MediaContentViewModel sender) : ValueChangedMessage<MediaContentViewModel>(sender);