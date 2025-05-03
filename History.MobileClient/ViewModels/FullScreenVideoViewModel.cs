using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class FullScreenVideoViewModel : VideoViewModel
{
    public FullScreenVideoViewModel(VideoViewModel source) : base(source.Uri, source.Description)
    {
        Aspect = Aspect.AspectFit;
        VideoShouldAutoPlay = true;
        VideoShouldLoopPlayback = true;
        ShouldMute = false;
        VideoShouldShowPlaybackControls = true;
    }
}

