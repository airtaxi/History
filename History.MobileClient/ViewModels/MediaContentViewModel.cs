using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class MediaContentViewModel : ObservableObject, IContentViewModel
{
    public IEnumerable<MediaContent> AllMediaContents { get; }

    public MediaContent MediaContent { get; }
    public bool IsWrapped { get; }
    public bool IsVideo { get; }
    public string Description { get; }
    public bool HasDescription { get; }

    [ObservableProperty]
    public partial bool IsOverlayVisible { get; private set; }

    [ObservableProperty]
    public partial IMediaViewModel Media { get; private set; }

    public MediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents, bool isWrapped)
    {
        AllMediaContents = allMediaContents;
        MediaContent = mediaContent;
        IsWrapped = isWrapped;
        IsVideo = mediaContent.IsVideo;
        Description = mediaContent.Description ?? string.Empty;
        HasDescription = !string.IsNullOrEmpty(Description);
        Media = new ImageViewModel(Utils.GenerateMediaUri(mediaContent.ThumbnailMediaId))
        {
            Aspect = isWrapped ? Aspect.AspectFill : Aspect.AspectFit,
            HorizontalContentOptions = isWrapped ? LayoutOptions.Fill : LayoutOptions.Start,
            VerticalContentOptions = isWrapped ? LayoutOptions.Fill : LayoutOptions.Start,
        };
        IsOverlayVisible = mediaContent.IsVideo;
    }

    [RelayCommand]
    public void HandleOverlayTap()
    {
        if (!MediaContent.IsVideo) throw new InvalidOperationException("MediaContent is not a video.");

        IsOverlayVisible = false;
        Media = new VideoViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
        {
            Aspect = IsWrapped ? Aspect.AspectFill : Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill
        };
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        IMediaViewModel viewModel = MediaContent.IsVideo ?
        new VideoViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            ShouldAutoPlay = true,
            ShouldLoopPlayback = true,
            ShouldMute = false,
            ShouldShowPlaybackControls = true
        }
        : new ImageViewModel(Utils.GenerateMediaUri(MediaContent.MediaId))
        {
            Aspect = Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill,
            IsFullScreen = true
        };

        var viewerPage = new FullScreenMediaViewerPage(viewModel);
        await App.PushModalAsync(viewerPage);
    }
}
