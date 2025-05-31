using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
