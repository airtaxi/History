using CommunityToolkit.Mvvm.Messaging.Messages;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Messages;

public class ResizeCarouselViewMessage(ImageViewModel value) : ValueChangedMessage<ImageViewModel>(value);
