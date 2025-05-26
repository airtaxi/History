using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class FullScreenMediaContentViewModel(List<IMediaViewModel> medias, IMediaViewModel media) : ObservableObject
{
    public List<IMediaViewModel> FullScreenMedias { get; } = medias;

    [ObservableProperty]
    public partial IMediaViewModel CurrentMedia { get; set; } = media;

    public bool CarouselSwipeEnabled => FullScreenMedias.Count > 1;
}
