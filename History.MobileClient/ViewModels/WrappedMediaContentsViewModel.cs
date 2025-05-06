using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public partial class WrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents) : ObservableObject, IContentViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    public partial int CarouselPosition { get; set; }

    public string CarouselPositionText => $"{CarouselPosition + 1} / {mediaContents.Count()}";

    public List<MediaContentViewModel> Medias => [.. mediaContents.Select(m => new MediaContentViewModel(m, allMediaContents, true))];
}
