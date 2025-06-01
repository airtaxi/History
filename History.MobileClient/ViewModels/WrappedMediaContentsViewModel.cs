using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;
using System.Diagnostics;

namespace History.MobileClient.ViewModels;

public partial class WrappedMediaContentsViewModel : ObservableObject, IContentViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    public partial int CarouselPosition { get; set; }
    public string CarouselPositionText => $"{CarouselPosition + 1} / {_mediaContentsCount}";

    // Single media content won't be scrolled
    public bool CarouselSwipeEnabled { get; }

    public List<MediaContentViewModel> Medias { get; }
    public MediaContentViewModel FirstMedia { get; }

    private readonly int _mediaContentsCount;

    public WrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents, bool isTimeline, bool isParentPost = false)
    {
        _mediaContentsCount = mediaContents.Count();
        CarouselSwipeEnabled = _mediaContentsCount > 1;

        var medias = mediaContents.Select(m => new MediaContentViewModel(m, allMediaContents, isTimeline, isParentPost)).ToList();
        FirstMedia = medias.FirstOrDefault() ?? throw new InvalidOperationException("No media contents available.");
        Debug.WriteLine($"FIRST MEDIA: {FirstMedia.Media.Uri}");
        Medias = medias;
    }
}
