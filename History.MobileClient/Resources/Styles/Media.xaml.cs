#if ANDROID
using AndroidX.AppCompat.Widget;
using AndroidX.Lifecycle;
#endif

using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;

namespace History.MobileClient.Resources.Styles;

public partial class Media : ResourceDictionary
{
    private static readonly ConcurrentDictionary<ContentView, IViewHandler> MediaElementHandlerMap = [];
    private static readonly ConcurrentDictionary<object, object> ImageHandlerMap = [];

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
        mediaElement.BindingContext = viewModel;
        mediaElement.HorizontalOptions = viewModel.HorizontalContentOptions;
        mediaElement.VerticalOptions = viewModel.VerticalContentOptions;
        mediaElement.ShouldAutoPlay = viewModel.VideoShouldAutoPlay;
        mediaElement.ShouldLoopPlayback = viewModel.VideoShouldLoopPlayback;
        mediaElement.ShouldMute = contentView.BindingContext is not FullScreenVideoViewModel;
        mediaElement.ShouldShowPlaybackControls = viewModel.VideoShouldShowPlaybackControls;
        mediaElement.ShouldKeepScreenOn = false;

        if (mediaElement.MediaWidth > 0 && mediaElement.MediaHeight > 0) ResizeMediaElement(mediaElement);
        else mediaElement.StateChanged += OnMediaStateChanged;

        mediaElement.Source = MediaSource.FromUri(viewModel.Uri);
    }

    private void OnMediaStateChanged(object sender, MediaStateChangedEventArgs e)
    {
        if (e.NewState == MediaElementState.Playing)
        {
            if (sender is not MediaElement mediaElement) return;
            mediaElement.StateChanged -= OnMediaStateChanged;
            ResizeMediaElement(mediaElement);
        }
    }

    private static void ResizeMediaElement(MediaElement mediaElement)
    {
        if (mediaElement.BindingContext is not VideoViewModel viewModel) return;

        if (viewModel.ResizeParentCarouselViewWhenSizeChanged)
        {
            var parent = mediaElement.Parent;
            while (parent != null && parent is not CarouselView) parent = parent.Parent;

            if (parent != null && parent is CarouselView carouselView)
            {
                var mediaElementWidth = mediaElement.MediaWidth;
                var mediaElementHeight = mediaElement.MediaHeight;
                if (mediaElementWidth == 0 || mediaElementHeight == 0) return;

                var aspectRatio = (double)mediaElementWidth / mediaElementHeight;
                var parentWidth = carouselView.Width;
                var newHeight = parentWidth / aspectRatio;
                carouselView.HeightRequest = newHeight;
            }
        }
    }

    private void OnVideoContentViewUnloaded(object sender, EventArgs e)
    {
        var contentView = sender as ContentView;
        (contentView.Content as MediaElement)?.StateChanged -= OnMediaStateChanged;

        if (MediaElementHandlerMap.TryGetValue(contentView, out var handler))
        {
            try { handler.DisconnectHandler(); }
            catch (ObjectDisposedException) { }
            finally { MediaElementHandlerMap.TryRemove(contentView, out var _); }
        }
    }

    private static void OnAndroidImageLoaded(object sender, EventArgs e)
    {
        if (!ImageHandlerMap.TryGetValue(sender, out var rawImage) || rawImage is not Image image) return;
        if (image.BindingContext is not ImageViewModel viewModel) return;

        if (viewModel.ResizeParentCarouselViewWhenSizeChanged)
        {
            var imageWidth = 0;
            var imageHeight = 0;
#if ANDROID
            var handler = image.Handler as ImageHandler;
            var nativeImageView = handler.PlatformView as AppCompatImageView;

            if (nativeImageView.Drawable is Android.Graphics.Drawables.BitmapDrawable bitmapDrawable)
            {
                var bitmap = bitmapDrawable.Bitmap;
                imageWidth = bitmap.Width;
                imageHeight = bitmap.Height;
            }
#endif
            if (imageWidth <= 0 || imageHeight <= 0) return;
#if ANDROID
            nativeImageView.ViewTreeObserver.GlobalLayout -= OnAndroidImageLoaded;
            ImageHandlerMap.TryRemove(nativeImageView.ViewTreeObserver, out var _);
#endif
            ResizeCarouselView(image, imageWidth, imageHeight);
        }
    }

    private static void ResizeCarouselView(Image image, int imageWidth, int imageHeight)
    {
        var parent = image.Parent;
        while (parent != null && parent is not CarouselView) parent = parent.Parent;

        if (parent != null && parent is CarouselView carouselView)
        {
            var aspectRatio = (double)imageWidth / imageHeight;
            var parentWidth = carouselView.Width;
            var newHeight = parentWidth / aspectRatio;
            carouselView.HeightRequest = newHeight;
        }
    }

    private void OnImageLoaded(object sender, EventArgs e)
    {
        if (sender is not Image image) return;

#if ANDROID
        var handler = image.Handler as ImageHandler;
        var imageView = handler.PlatformView as AppCompatImageView;
        ImageHandlerMap[imageView.ViewTreeObserver] = image;
        imageView.ViewTreeObserver.GlobalLayout += OnAndroidImageLoaded;
#elif IOS
        var handler = image.Handler as ImageHandler;
        var nativeImageView = handler.PlatformView;
        ImageHandlerMap[image] = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            while (nativeImageView.Image == null && ImageHandlerMap.ContainsKey(image)) await Task.Delay(50);

            var uiImage = nativeImageView.Image;
            if (uiImage == null) return;

            var pixelWidth = (int)(uiImage.Size.Width * uiImage.CurrentScale);
            var pixelHeight = (int)(uiImage.Size.Height * uiImage.CurrentScale);
            ResizeCarouselView(image, pixelWidth, pixelHeight);
        });
#endif
    }

    private void OnImageUnloaded(object sender, EventArgs e)
    {
        if (sender is not Image image) return;

#if ANDROID
        if (ImageHandlerMap.TryGetValue(image, out var rawImage) && rawImage is AppCompatImageView imageView)
        {
            imageView.ViewTreeObserver.GlobalLayout -= OnAndroidImageLoaded;
            ImageHandlerMap.TryRemove(imageView.ViewTreeObserver, out var _);
        }
#elif IOS
        if (ImageHandlerMap.TryGetValue(image, out var rawImage)) ImageHandlerMap.TryRemove(image, out var _);
#endif
    }
}