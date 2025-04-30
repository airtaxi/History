using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

internal class MediaTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }
    // MAUI BUG: Cannot set ShouldShowPlaybackControls on xaml
    public DataTemplate ControllableVideoTemplate { get; set; }
    public DataTemplate ImageTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ImageViewModel) return ImageTemplate;
        else if (item is VideoViewModel videoViewModel)
        {
            if (videoViewModel.VideoShouldShowPlaybackControls) return ControllableVideoTemplate;
            else return VideoTemplate;
        }
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
