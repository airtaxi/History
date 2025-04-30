using History.Commons;
using History.Commons.DataTypes.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public class MediaContentsViewModel(IEnumerable<MediaContent> mediaContents) : IContentViewModel
{
    public List<IMediaViewModel> Medias => [.. mediaContents.Select(mediaContent => (IMediaViewModel)(mediaContent.IsVideo
        ? new VideoViewModel(CommonsConstants.MediaBaseUrl + mediaContent.MediaId)
        {
            VideoShouldShowPlaybackControls = true,
            Aspect = Aspect.AspectFit,
            ShouldMute = true,
            HorizontalContentOptions = LayoutOptions.Fill,
            VideoShouldAutoPlay = true,
            VideoShouldLoopPlayback = false,
            VerticalContentOptions = LayoutOptions.Fill
        }
        : new ImageViewModel(CommonsConstants.MediaBaseUrl + mediaContent.MediaId)
        {
            Aspect = Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill
        }))];
}
