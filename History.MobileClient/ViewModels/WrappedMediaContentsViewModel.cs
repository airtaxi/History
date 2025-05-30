using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public partial class WrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents, bool isTimeline, bool isParentPost = false) : ObservableObject, IContentViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    public partial int CarouselPosition { get; set; }

    // Single media content won't be scrolled
    public bool CarouselSwipeEnabled => mediaContents.Count() > 1;
    public string CarouselPositionText => $"{CarouselPosition + 1} / {mediaContents.Count()}";

    public List<MediaContentViewModel> Medias { get; } = [.. mediaContents.Select(m => new MediaContentViewModel(m, allMediaContents, isTimeline, isParentPost))];
    public MediaContentViewModel FirstMedia => Medias.FirstOrDefault() ?? throw new InvalidOperationException("No media contents available.");
}
