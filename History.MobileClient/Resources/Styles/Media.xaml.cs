using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls;
using System.Collections.Concurrent;

namespace History.MobileClient.Resources.Styles;

public partial class Media : ResourceDictionary
{
    private static readonly ConcurrentDictionary<ContentView, IViewHandler> MediaElementHandlerMap = [];

    public Media() => InitializeComponent();

    private void OnVideoContentViewLoaded(object sender, EventArgs e)
    {
        var contentView = sender as ContentView;
        var mediaElement = new MediaElement();
        contentView.Content = mediaElement;
        var handler = mediaElement.Handler;
        MediaElementHandlerMap[contentView] = handler;

        var viewModel = contentView.BindingContext as VideoViewModel;
        mediaElement.Aspect = viewModel.Aspect;
        mediaElement.HorizontalOptions = viewModel.HorizontalContentOptions;
        mediaElement.VerticalOptions = viewModel.VerticalContentOptions;
        mediaElement.ShouldAutoPlay = viewModel.VideoShouldAutoPlay;
        mediaElement.ShouldLoopPlayback = viewModel.VideoShouldLoopPlayback;
        mediaElement.ShouldMute = contentView.BindingContext is not FullScreenVideoViewModel;
        mediaElement.ShouldShowPlaybackControls = viewModel.VideoShouldShowPlaybackControls;
        mediaElement.ShouldKeepScreenOn = false;
        mediaElement.Source = MediaSource.FromUri(viewModel.Uri);
    }

    private void OnVideoContentViewUnloaded(object sender, EventArgs e)
    {
        var contentView = sender as ContentView;
        if (MediaElementHandlerMap.TryGetValue(contentView, out var handler))
        {
            try { handler.DisconnectHandler(); }
            catch (ObjectDisposedException) { }
            finally { MediaElementHandlerMap.TryRemove(contentView, out var _); }
        }
    }
}