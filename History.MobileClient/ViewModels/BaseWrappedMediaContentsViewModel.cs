using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.Messages;
using History.Commons.Enums;

namespace History.MobileClient.ViewModels;

// Base wrapped media (carousel) view model shared by History and (future) Kakao Story.
// Holds the carousel geometry/state logic and messenger wiring; derived types build the
// media list from their own data models.
public partial class BaseWrappedMediaContentsViewModel : ObservableObject, IContentViewModel
{
    private double _lastCarouselViewHeight = 0;

    [ObservableProperty]
    public partial double MaxCarouselViewHeight { get; private set; }

    [ObservableProperty]
    public partial double MinCarouselViewHeight { get; private set; }

    public int CarouselPosition
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(CarouselPositionText));
#if ANDROID
            OnPropertyChanged(nameof(CarouselViewHeight));
#else
            OnPropertyChanged(nameof(CarouselViewHeight));
#endif
        }
    }
    public string CarouselPositionText => $"{CarouselPosition + 1} / {_mediaContentsCount}";

    [ObservableProperty]
    public partial LayoutOptions CarouselViewHorizontalOptions { get; set; } = LayoutOptions.Fill;

    [ObservableProperty]
    public partial double CarouselViewWidth { get; set; } = -1;

    public double CarouselViewHeight
    {
        get
        {
            var newHeight = CalculateNewHeight();
            try
            {

                var carouselViewWidth = CurrentPositionMediaImageViewModel.CarouselView?.Width ?? -2;
                Debug.WriteLine($"CarouselViewHeight: {newHeight} (W: {carouselViewWidth})");
                if (newHeight == 0) return _lastCarouselViewHeight;

                App.Page.Dispatcher.Dispatch(() =>
                {
                    var maxWidth = CurrentPositionMediaImageViewModel.MaxWidth;
                    if (CarouselViewWidth == -1 && carouselViewWidth > maxWidth)
                    {
                        CarouselViewWidth = maxWidth;
                        CarouselViewHorizontalOptions = LayoutOptions.Start;
                        CurrentPositionMediaImageViewModel.CarouselView.WidthRequest = maxWidth;
                        CurrentPositionMediaImageViewModel.CarouselView.HorizontalOptions = LayoutOptions.Start;
                    }
                    else if (CarouselViewWidth != -1 && carouselViewWidth < maxWidth)
                    {
                        CarouselViewWidth = -1;
                        CarouselViewHorizontalOptions = LayoutOptions.Fill;
                    }
                });

                _lastCarouselViewHeight = newHeight;
                return newHeight;
            }
            finally
            {
#if IOS
                var image = CurrentPositionMediaImageViewModel?.Image;
                var carouselView = CurrentPositionMediaImageViewModel?.CarouselView;
                if (image != null && carouselView != null && newHeight > 0)
                {
                    image.WidthRequest = carouselView.Width;
                    image.HeightRequest = Math.Min(newHeight, MaxCarouselViewHeight);
                    image.ReloadImage();
                }
#endif
            }
        }
    }

    // Single media content won't be scrolled
    public bool CarouselSwipeEnabled { get; }

    public List<BaseMediaContentViewModel> Medias { get; }
    public BaseMediaContentViewModel FirstMedia { get; }

    private ImageViewModel CurrentPositionMediaImageViewModel => Medias[CarouselPosition].ImageMedia as ImageViewModel;

    private readonly int _mediaContentsCount;
    private readonly PostType _postType;

    public BaseWrappedMediaContentsViewModel(IEnumerable<BaseMediaContentViewModel> medias, PostType postType)
    {
        _postType = postType;
        _mediaContentsCount = medias.Count();
        CarouselSwipeEnabled = _mediaContentsCount > 1;

        if (_postType == PostType.Unwrapped)
        {
            MinCarouselViewHeight = 10;
            MaxCarouselViewHeight = double.PositiveInfinity;
        }
        else
        {
            MinCarouselViewHeight = 400;
            MaxCarouselViewHeight = 400;
        }

        var mediaList = medias.ToList();
        FirstMedia = mediaList.FirstOrDefault() ?? throw new InvalidOperationException("No media contents available.");
        Debug.WriteLine($"FIRST MEDIA: {FirstMedia.Media.Uri}");
        Medias = mediaList;

        WeakReferenceMessenger.Default.Register<ResizeCarouselViewMessage>(this, OnCarouselViewHeightChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<SpanChangedMessage>(this, OnSpanChangedMessageReceived);
    }

    public void UpdateCarouselViewSize()
    {
        var newHeight = CalculateNewHeight();
        if (newHeight != _lastCarouselViewHeight && newHeight > 0)
        {
            Debug.WriteLine($"UpdateCarouselViewHeight {_lastCarouselViewHeight}");
            OnPropertyChanged(nameof(CarouselViewHeight));
        }
    }

    private void OnSpanChangedMessageReceived(object _, SpanChangedMessage __)
    {
        _lastCarouselViewHeight = 0;
        CarouselViewWidth = -1;
        CarouselViewHorizontalOptions = LayoutOptions.Fill;
        OnPropertyChanged(nameof(CarouselViewHeight));
    }

    private double CalculateNewHeight()
    {
        if (CurrentPositionMediaImageViewModel == null) return _lastCarouselViewHeight;

        var carouselView = CurrentPositionMediaImageViewModel.CarouselView;
        if (carouselView == null) return _lastCarouselViewHeight;


        var width = CurrentPositionMediaImageViewModel.ImageWidth;
        var height = CurrentPositionMediaImageViewModel.ImageHeight;
        var aspectRatio = (double)width / height;

        var carouselViewWidth = Math.Min(carouselView.Width, CurrentPositionMediaImageViewModel.MaxWidth);
        var newHeight = carouselViewWidth / aspectRatio;

        if (_postType != PostType.Unwrapped)
        {
            var targetMaxAspectRatio = 1; // 1:1 aspect ratio for timeline
            var newMaxHeight = carouselViewWidth / targetMaxAspectRatio;
            if (newMaxHeight > 0)
            {
                if (newHeight > 0) MinCarouselViewHeight = Math.Min(newHeight, newMaxHeight);
                MaxCarouselViewHeight = newMaxHeight;
            }
        }

        return newHeight;
    }

    private void OnCarouselViewHeightChangedMessageReceived(object _, ResizeCarouselViewMessage message)
    {
        if (CurrentPositionMediaImageViewModel == message.Value)
        {
            UpdateCarouselViewSize();
        }
    }
}
