using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FFImageLoading;
using FFImageLoading.Config;
using FFImageLoading.Maui;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;

namespace History.MobileClient.ViewModels;

public partial class ImageViewModel(string uri, PostType postType = PostType.Unwrapped) : ObservableObject, IMediaViewModel
{
    public PostType PostType { get; } = postType;

    // Per-image configuration override (e.g. custom download headers).
    // Defaults to the global configuration; CachedImage picks it up via binding.
    public IConfiguration Configuration { get; set; } = ImageService.Instance.Configuration;

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
    public partial bool IsFullScreen { get; set; } = false;

    [ObservableProperty]
    public partial bool IsInZoomMode { get; set; } = false;

    [ObservableProperty]
    public partial bool FullScreenSwipeable { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAnimated { get; set; }

    public double MaxWidth { get; set; } = double.PositiveInfinity;

    public CarouselView CarouselView;
    public CachedImage Image;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public void ResizeCarouselView(CarouselView carouselView, CachedImage image, int imageWidth, int imageHeight)
    {
        CarouselView = carouselView;
        Image = image;
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;

        WeakReferenceMessenger.Default.Send(new ResizeCarouselViewMessage(this));
    }
}

