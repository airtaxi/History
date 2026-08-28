using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.ViewModels.Media;

// Carousel state view model for wrapped media contents, mirroring the MAUI
// BaseWrappedMediaContentsViewModel. The MAUI version computes the carousel height inside
// property getters; here the control pushes the viewport width and the media items report
// their decoded pixel sizes, then the height is recalculated explicitly.
public sealed partial class WrappedMediaContentsViewModel : ObservableObject
{
    private const double MinCarouselHeight = 10;

    private double _viewportWidth;
    private int _previousPosition = -1;

    public PostType PostType { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselSwipeEnabled), nameof(CarouselPositionText))]
    public partial List<MediaContentViewModel> Medias { get; private set; } = [];

    public bool CarouselSwipeEnabled => Medias.Count > 1;

    public string CarouselPositionText => $"{CarouselPosition + 1} / {Medias.Count}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    public partial int CarouselPosition { get; set; }

    [ObservableProperty]
    public partial double CarouselHeight { get; private set; }

    [ObservableProperty]
    public partial double CarouselWidth { get; private set; } = double.NaN;

    [ObservableProperty]
    public partial HorizontalAlignment CarouselHorizontalAlignment { get; private set; } = HorizontalAlignment.Stretch;

    public void Update(List<MediaContent> mediaContents, List<MediaContent> allMediaContents, PostType postType, bool isParentPost)
    {
        PostType = postType;
        var medias = new List<MediaContentViewModel>();
        if (mediaContents != null)
        {
            foreach (var mediaContent in mediaContents)
            {
                var mediaViewModel = new MediaContentViewModel(mediaContent, allMediaContents, postType, isParentPost);
                mediaViewModel.ImageSizeReported += RecalculateCarouselHeight;
                medias.Add(mediaViewModel);
            }
        }
        Medias = medias;

        _previousPosition = -1;
        CarouselPosition = 0;
        RecalculateCarouselHeight();
    }

    public void UpdateViewportWidth(double width)
    {
        if (_viewportWidth == width) return;

        _viewportWidth = width;
        RecalculateCarouselHeight();
    }

    // Same rule as the MAUI client: unwrapped posts size freely (10px floor), while timeline-like
    // surfaces cap the carousel at a 1:1 aspect ratio and clamp its width to the natural media width.
    private void RecalculateCarouselHeight()
    {
        if (Medias.Count == 0)
        {
            CarouselHeight = 0;
            return;
        }
        if (_viewportWidth <= 0) return;

        var currentItem = Medias[Math.Clamp(CarouselPosition, 0, Medias.Count - 1)];
        var hasNaturalSize = currentItem.PixelWidth > 0 && currentItem.PixelHeight > 0;
        var displayedWidth = hasNaturalSize ? Math.Min(_viewportWidth, currentItem.PixelWidth) : _viewportWidth;
        var height = hasNaturalSize ? displayedWidth * currentItem.PixelHeight / (double)currentItem.PixelWidth : displayedWidth;
        if (PostType == PostType.Unwrapped) height = Math.Max(height, MinCarouselHeight);
        else height = Math.Min(height, displayedWidth);

        CarouselHeight = height;
        CarouselWidth = displayedWidth < _viewportWidth ? displayedWidth : double.NaN;
        CarouselHorizontalAlignment = double.IsNaN(CarouselWidth) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
    }

    partial void OnCarouselPositionChanged(int value)
    {
        if (Medias.Count == 0) return;

        // Reset the previously selected media so its video stops and its spoiler overlay reappears.
        if (_previousPosition >= 0 && _previousPosition < Medias.Count && _previousPosition != value) Medias[_previousPosition].ResetForReuse();
        _previousPosition = value;
        RecalculateCarouselHeight();
    }
}