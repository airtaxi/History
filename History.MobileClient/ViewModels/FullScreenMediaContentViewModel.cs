using CommunityToolkit.Mvvm.ComponentModel;

namespace History.MobileClient.ViewModels;

public partial class FullScreenMediaContentViewModel(List<IMediaViewModel> medias, IMediaViewModel media) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    public partial int CarouselPosition { get; set; }

#if IOS
    public List<IMediaViewModel> FullScreenMedias { get; } = medias.OfType<ImageViewModel>().Select(x => (IMediaViewModel)x).ToList();
#else
    public List<IMediaViewModel> FullScreenMedias { get; } = medias;
#endif

    [ObservableProperty]
    public partial IMediaViewModel CurrentMedia { get; set; } = media;

    public string CarouselPositionText => $"{CarouselPosition + 1} / {FullScreenMedias.Count}";
}
