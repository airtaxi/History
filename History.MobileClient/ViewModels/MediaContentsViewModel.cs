using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public partial class MediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents) : ObservableObject, IContentViewModel
{
    public IEnumerable<MediaContent> AllMediaContents { get; } = allMediaContents;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    public partial int CarouselPosition { get; set; }

    public string CarouselPositionText => $"{CarouselPosition + 1} / {mediaContents.Count()}";

    public List<IMediaViewModel> Medias => [.. mediaContents.Select(m => Utils.GenerateMediaViewModelFromMediaContent(m, true))];
}
