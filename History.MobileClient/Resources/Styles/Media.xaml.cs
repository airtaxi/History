using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using FFImageLoading.Maui;
using FFImageLoading.Maui.Platform;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace History.MobileClient.Resources.Styles;

public partial class Media : ResourceDictionary
{
    private static readonly ConcurrentDictionary<ContentView, IViewHandler> MediaElementHandlerMap = [];
    private static readonly ConcurrentDictionary<object, Size> ImageSizeMap = [];
    private static IMediaViewModel s_lastLoadedMediaViewModel;

    public Media() => InitializeComponent();

    private static void ResizeCarouselView(CarouselView carouselView, int width, int height)
    {
        var aspectRatio = (double)width / height;
        var parentWidth = carouselView.Width;
        var newHeight = parentWidth / aspectRatio;
        var previousHeight = carouselView.Height;
        carouselView.HeightRequest = newHeight;
    }

    private static CarouselView FindCarouselView(Element element)
    {
        var parent = element.Parent;
        while (parent != null && parent is not CarouselView) parent = parent.Parent;

        return parent as CarouselView;
    }

    private static void ResizeMediaElement(MediaElement mediaElement)
    {
        if (mediaElement.BindingContext is not VideoViewModel viewModel) return;

        if (viewModel.ResizeParentCarouselViewWhenSizeChanged)
        {
            var carouselView = FindCarouselView(mediaElement);
            if (carouselView != null)
            {
                var mediaElementWidth = mediaElement.MediaWidth;
                var mediaElementHeight = mediaElement.MediaHeight;
                if (mediaElementWidth == 0 || mediaElementHeight == 0) return;

                ResizeCarouselView(carouselView, mediaElementWidth, mediaElementHeight);
            }
        }
    }

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
        mediaElement.ShouldAutoPlay = viewModel.ShouldAutoPlay;
        mediaElement.ShouldLoopPlayback = viewModel.ShouldLoopPlayback;
        mediaElement.ShouldMute = viewModel.ShouldMute;
        mediaElement.ShouldShowPlaybackControls = viewModel.ShouldShowPlaybackControls;
        mediaElement.ShouldKeepScreenOn = viewModel.ShouldKeepScreenOn;

        if (viewModel.ResizeParentCarouselViewWhenSizeChanged)
        {
            if (mediaElement.MediaWidth > 0 && mediaElement.MediaHeight > 0) ResizeMediaElement(mediaElement);
            else mediaElement.StateChanged += OnMediaStateChanged;
        }
#if IOS
        mediaElement.StateChanged += OnAppleMediaStateChanged;
#endif

        mediaElement.Source = MediaSource.FromUri(viewModel.Uri);
        mediaElement.MediaFailed += OnMediaFailed;
    }

    private void OnAppleMediaStateChanged(object sender, MediaStateChangedEventArgs e)
    {
        if (e.NewState == MediaElementState.Playing)
        {
            if (sender is not MediaElement mediaElement) return;
            mediaElement.StateChanged -= OnMediaStateChanged;
            mediaElement.HeightRequest = mediaElement.MediaHeight;
        }
    }

    private void OnMediaFailed(object sender, MediaFailedEventArgs e) => Toast.Make(e.ErrorMessage).Show();

    private void OnVideoContentViewUnloaded(object sender, EventArgs e)
    {
        var contentView = sender as ContentView;
        var mediaElement = contentView.Content as MediaElement;
        if (mediaElement != null)
        {
#if IOS
            mediaElement.StateChanged -= OnAppleMediaStateChanged;
#endif
            mediaElement.StateChanged -= OnMediaStateChanged;
            mediaElement.MediaFailed -= OnMediaFailed;
        }

        if (MediaElementHandlerMap.TryGetValue(contentView, out var handler))
        {
            try { handler.DisconnectHandler(); }
            catch (ObjectDisposedException) { }
            finally { MediaElementHandlerMap.TryRemove(contentView, out var _); }
        }
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

    private void OnImageLoaded(object sender, EventArgs e)
    {
        if (sender is not CachedImage image) return;

#if IOS
        image.DownsampleToViewSize = false;
#endif

        var carouselView = FindCarouselView(image);
        if (carouselView == null) return;

        var viewModel = image.BindingContext as ImageViewModel;
        if (viewModel == null) return;

        Debug.WriteLine($"IMAGE Loaded: {viewModel.Uri}");

        WeakReferenceMessenger.Default.UnregisterAll(sender);
        WeakReferenceMessenger.Default.Register<ResizeMediaCarouselViewMessage>(sender, async (s, e) =>
        {
            if (e.Value == viewModel && s_lastLoadedMediaViewModel != viewModel)
            {
                Debug.WriteLine($"ResizeMediaCarouselViewMessage: {viewModel.Uri}");
                s_lastLoadedMediaViewModel = viewModel;
                await Task.Delay(100);
                ResizeImage(sender);
            }
        });
    }

    private void OnImageUnloaded(object sender, EventArgs e)
    {
        var image = sender as CachedImage;
        WeakReferenceMessenger.Default.UnregisterAll(image);
    }

    private async void OnImageFinished(object sender, CachedImageEvents.FinishEventArgs e)
    {
        if (sender is not CachedImage image) return;

        var carouselView = FindCarouselView(image);
        if (carouselView == null) return;

        var viewModel = image.BindingContext as ImageViewModel;
        if (viewModel == null) return;

        Debug.WriteLine($"IMAGE Finished: {viewModel.Uri} / {s_lastLoadedMediaViewModel == viewModel}");

        if (s_lastLoadedMediaViewModel == viewModel)
        {
            await Task.Delay(100);
            ResizeImage(sender);
        }
    }

    private static void ResizeImage(object sender)
    {
        var image = sender as CachedImage;
        image.Dispatcher.Dispatch(() =>
        {
            var nativeImageView = (image?.Handler as CachedImageHandler)?.PlatformView;
            int imageWidth = 0, imageHeight = 0;

#if ANDROID
            if (nativeImageView.Drawable is Android.Graphics.Drawables.BitmapDrawable bitmapDrawable)
            {
                var bitmap = bitmapDrawable.Bitmap;
                imageWidth = bitmap.Width;
                imageHeight = bitmap.Height;
            }
#elif IOS
            var uiImage = nativeImageView.Image;
            if (uiImage == null) return;

            imageWidth = (int)(uiImage.Size.Width * uiImage.CurrentScale);
            imageHeight = (int)(uiImage.Size.Height * uiImage.CurrentScale);
#endif

            if (imageWidth <= 0 || imageHeight <= 0) return;
            var aspectRatio = (double)imageWidth / imageHeight;

            var viewModel = image?.BindingContext as ImageViewModel;
            if (viewModel.ResizeParentCarouselViewWhenSizeChanged)
            {
                var carouselView = FindCarouselView(image);
                if (carouselView == null) return;

                ResizeCarouselView(carouselView, imageWidth, imageHeight);
                image.InvalidateMeasure();
            }
        });
    }
}