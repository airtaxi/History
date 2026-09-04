using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.WindowsClient.Messages;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace History.WindowsClient.ViewModels.Media;

// Carousel state view model for wrapped media contents. The control pushes the
// viewport width and the media items report their decoded pixel sizes, then the
// height is recalculated explicitly.
public sealed partial class WrappedMediaContentsViewModel : ObservableObject, IRecipient<MediaImageSizeReportedMessage>
{
    private const double MaxCarouselHeight = 400;
    private const double UnwrappedMaxCarouselHeight = 640;
    private static double s_cachedViewportWidth = double.NaN;
    private static Dictionary<string, double> s_cachedAspectRatio = [];

    private double _viewportWidth;
    private int _previousPosition = -1;

    public WrappedMediaContentsViewModel() => WeakReferenceMessenger.Default.Register(this);

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

public void Update(List<MediaContent> mediaContents, List<MediaContent> allMediaContents, PostType postType, bool isParentPost)
    {
        PostType = postType;
        var medias = new List<MediaContentViewModel>();
        if (mediaContents != null)
        {
            foreach (var mediaContent in mediaContents)
            {
                medias.Add(new MediaContentViewModel(mediaContent, allMediaContents, postType, isParentPost));
            }
        }
        Medias = medias;

        _previousPosition = -1;
        CarouselPosition = 0;

        if (Medias.Count == 0) return;

        var currentItem = Medias[Math.Clamp(CarouselPosition, 0, Medias.Count - 1)];
        if (!double.IsNaN(s_cachedViewportWidth) && (currentItem.HasPixelSize || s_cachedAspectRatio.ContainsKey(currentItem.MediaContent.MediaId)))
        {
            _viewportWidth = s_cachedViewportWidth;
            RecalculateCarouselHeight();
        }
    }

    public void Receive(MediaImageSizeReportedMessage message)
    {
        var sender = message.Value;

        // Only react to size reports coming from media items this carousel currently owns,
        // so concurrent carousels (multiple posts on screen) do not interfere with each other.
        if (sender == null || Medias.Count == 0 || !Medias.Contains(sender)) return;

        var currentItem = Medias[Math.Clamp(CarouselPosition, 0, Medias.Count - 1)];
        if (currentItem != sender) return;

        RecalculateCarouselHeight();
    }

    public void UpdateViewportWidth(double width)
    {
        if (_viewportWidth == width) return;

        _viewportWidth = width;
        s_cachedViewportWidth = _viewportWidth;

        RecalculateCarouselHeight();
    }

    private void RecalculateCarouselHeight()
    {
        if (Medias.Count == 0)
        {
            CarouselHeight = 0;
            return;
        }
        if (_viewportWidth <= 0) return;

        var currentItem = Medias[Math.Clamp(CarouselPosition, 0, Medias.Count - 1)];
        currentItem.Width = _viewportWidth;

        double aspectRatio = double.NaN;
        var hasNaturalSize = currentItem.PixelWidth > 0 && currentItem.PixelHeight > 0;
        if (!hasNaturalSize && !s_cachedAspectRatio.TryGetValue(currentItem.MediaContent.MediaId, out aspectRatio)) return;
        if (double.IsNaN(aspectRatio)) aspectRatio = (double)currentItem.PixelHeight / currentItem.PixelWidth;
        s_cachedAspectRatio[currentItem.MediaContent.MediaId] = aspectRatio;

        var height = Math.Min(_viewportWidth * aspectRatio, PostType == PostType.Unwrapped ? UnwrappedMaxCarouselHeight : MaxCarouselHeight);

        CarouselHeight = height;
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