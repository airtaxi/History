using History.Uno.ViewModels;

namespace History.Uno.DataTypes;

public class CarouselPositionChangedMessage(MediaContentViewModel viewModel) : ValueChangedMessage<MediaContentViewModel>(viewModel);
