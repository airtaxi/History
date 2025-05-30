using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using FFImageLoading.Maui;
using FFImageLoading.Maui.Platform;
using History.MobileClient.ViewModels;
using System.Collections.Concurrent;

namespace History.MobileClient.Resources.Styles;

public partial class Media : ResourceDictionary
{
    private static readonly ConcurrentDictionary<ContentView, IViewHandler> MediaElementHandlerMap = [];
    private static readonly ConcurrentDictionary<object, object> ImageHandlerMap = [];

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

    private static async void ResizeImage(CachedImage image)
    {
        if (image.BindingContext is not ImageViewModel viewModel) return;

        if (viewModel.ResizeParentCarouselViewWhenSizeChanged)
        {
            if (ImageHandlerMap.ContainsKey(image)) return;

            var imageWidth = 0;
            var imageHeight = 0;

            var handler = image.Handler as CachedImageHandler;
            var nativeImageView = handler.PlatformView;
            ImageHandlerMap[image] = true;

#if ANDROID
            while (nativeImageView.Drawable == null && ImageHandlerMap.ContainsKey(image)) await Task.Delay(13);

            if (nativeImageView.Drawable is Android.Graphics.Drawables.BitmapDrawable bitmapDrawable)
            {
                var bitmap = bitmapDrawable.Bitmap;
                imageWidth = bitmap.Width;
                imageHeight = bitmap.Height;
            }
#elif IOS
        while (nativeImageView.Image == null && ImageHandlerMap.ContainsKey(image)) await Task.Delay(13);

        var uiImage = nativeImageView.Image;
        if (uiImage == null) return;

        imageWidth = (int)(uiImage.Size.Width * uiImage.CurrentScale);
        imageHeight = (int)(uiImage.Size.Height * uiImage.CurrentScale);
#endif

            if (imageWidth <= 0 || imageHeight <= 0) return;

            var carouselView = FindCarouselView(image);
            if (carouselView == null) return;

            ResizeCarouselView(carouselView, imageWidth, imageHeight);
            ImageHandlerMap.TryRemove(image, out var _);
        }
    }

    private void OnImageUnloaded(object sender, EventArgs e)
    {
        if (sender is not CachedImage image) return;

        var viewModel = image.BindingContext as ImageViewModel;
        if (viewModel.ResizeParentCarouselViewWhenSizeChanged) ImageHandlerMap.TryRemove(image, out var _);
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

    private void OnImageFinished(object sender, CachedImageEvents.FinishEventArgs e)
    {
        var image = sender as CachedImage;
        if (image.WidthRequest > 0) return;

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
        if (viewModel.IsFullScreen)
        {
            if (imageWidth < image.Width)
            {
                image.WidthRequest = image.Width;
                image.HeightRequest = image.Width / aspectRatio;
            }
        }
        else if(viewModel.ResizeParentCarouselViewWhenSizeChanged)
        {
            var carouselView = FindCarouselView(image);
            if (carouselView == null) return;

            ResizeCarouselView(carouselView, imageWidth, imageHeight);
        }
    }

    private void OnFullScreenImageSizeChanged(object sender, EventArgs e) => ResizeFullScreenImage(sender);

    private static void ResizeFullScreenImage(object sender)
    {
        var image = sender as CachedImage;

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

        if (imageWidth < image.Width)
        {
            image.WidthRequest = image.Width;
            image.HeightRequest = image.Width / aspectRatio;
        }

        return;
    }
}