using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;
using History.Uno.Enums;

namespace History.Uno.ViewModels;

public partial class WrappedMediaContentsViewModel : ObservableObject, IContentViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    public partial int CarouselPosition { get; set; }

    // CarouselPosition can be negative due to the way the carousel control works, so we need to ensure that the displayed position is always at least 1.
    public string CarouselPositionText => $"{Math.Max(CarouselPosition, 0) + 1} / {_mediaContentsCount}";

    // Single media content won't be scrolled
    public bool CarouselSwipeEnabled { get; }

    public List<MediaContentViewModel> Medias { get; }
    public MediaContentViewModel FirstMedia { get; }

    private readonly int _mediaContentsCount;

    partial void OnCarouselPositionChanged(int value) => WeakReferenceMessenger.Default.Send(new CarouselPositionChangedMessage(Medias[value]));

    public WrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost = false)
    {
        _mediaContentsCount = mediaContents.Count();
        CarouselSwipeEnabled = _mediaContentsCount > 1;

        var medias = mediaContents.Select(mediaContent => new MediaContentViewModel(mediaContent, allMediaContents, postType, isParentPost)).ToList();
        FirstMedia = medias.FirstOrDefault() ?? throw new InvalidOperationException("No media contents available.");
        Medias = medias;
    }
}
