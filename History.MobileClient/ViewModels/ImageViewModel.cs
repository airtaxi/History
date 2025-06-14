using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.DataTypes;

namespace History.MobileClient.ViewModels;

public partial class ImageViewModel(string uri) : ObservableObject, IMediaViewModel
{
    [ObservableProperty]
    public partial string Uri { get; set; } = uri;

    [ObservableProperty]
    public partial Aspect Aspect { get; set; } = Aspect.AspectFill;

    [ObservableProperty]
    public partial bool ResizeParentCarouselViewWhenSizeChanged { get; set; }

    [ObservableProperty]
    public partial LayoutOptions HorizontalContentOptions { get; set; } = LayoutOptions.Fill;

    [ObservableProperty]
    public partial LayoutOptions VerticalContentOptions { get; set; } = LayoutOptions.Fill;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotFullScreen))]
    public partial bool IsFullScreen { get; set; } = false;

    public bool IsNotFullScreen => !IsFullScreen;

    [ObservableProperty]
    public partial bool FullScreenSwipeable { get; set; } = true;

    public CarouselView CarouselView;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public void ResizeCarouselView(CarouselView carouselView, int imageWidth, int imageHeight)
    {
        CarouselView = carouselView;
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;

        WeakReferenceMessenger.Default.Send(new ResizeCarouselViewMessage(this));
    }
}

