using System.Diagnostics;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ZstdSharp.Unsafe;

namespace History.MobileClient.ViewModels;

public partial class WrappedMediaContentsViewModel : ObservableObject, IContentViewModel
{
    private double _lastCarouselViewHeight = 0;

    [ObservableProperty]
    public partial double MaxCarouselViewHeight { get; private set; }

    [ObservableProperty]
    public partial double MinCarouselViewHeight { get; private set; }

    public int CarouselPosition
    {
        get
        {
            return field;
        }
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
    public double CarouselViewHeight
    {
        get
        {
            var newHeight = CalculateNewHeight();
            try
            {
                Debug.WriteLine($"CarouselViewHeight: {newHeight} (W: {CurrentPositionMediaImageViewModel.CarouselView?.Width ?? -2})");
                if (newHeight == 0) return _lastCarouselViewHeight;

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

    public List<MediaContentViewModel> Medias { get; }
    public MediaContentViewModel FirstMedia { get; }

    private ImageViewModel CurrentPositionMediaImageViewModel => Medias[CarouselPosition].ImageMedia as ImageViewModel;

    private readonly int _mediaContentsCount;
    private readonly PostType _postType;

    public WrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost = false)
    {
        _postType = postType;
        _mediaContentsCount = mediaContents.Count();
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

        var medias = mediaContents.Select(m => new MediaContentViewModel(m, allMediaContents, postType, isParentPost)).ToList();
        FirstMedia = medias.FirstOrDefault() ?? throw new InvalidOperationException("No media contents available.");
        Debug.WriteLine($"FIRST MEDIA: {FirstMedia.Media.Uri}");
        Medias = medias;

        WeakReferenceMessenger.Default.Register<ResizeCarouselViewMessage>(this, OnCarouselViewHeightChangedMessageReceived);
    }

    public void UpdateCarouselViewHeight()
    {
        var newHeight = CalculateNewHeight();
        if (newHeight != _lastCarouselViewHeight && newHeight > 0)
        {
            Debug.WriteLine($"UpdateCarouselViewHeight {_lastCarouselViewHeight}");
            OnPropertyChanged(nameof(CarouselViewHeight));
        }
    }

    private double CalculateNewHeight()
    {
        if (CurrentPositionMediaImageViewModel == null) return _lastCarouselViewHeight;

        var carouselView = CurrentPositionMediaImageViewModel.CarouselView;
        if (carouselView == null) return _lastCarouselViewHeight;


        var width = CurrentPositionMediaImageViewModel.ImageWidth;
        var height = CurrentPositionMediaImageViewModel.ImageHeight;
        var aspectRatio = (double)width / height;

        var newHeight = carouselView.Width / aspectRatio;

        if (_postType != PostType.Unwrapped)
        {
            var targetMaxAspectRatio = 1; // 1:1 aspect ratio for timeline
            var carouselViewWidth = carouselView.Width;
            var newMaxHeight = carouselViewWidth / targetMaxAspectRatio;
            if (newMaxHeight > 0)
            {
                if (newHeight > 0) MinCarouselViewHeight = Math.Min(newHeight, newMaxHeight);
                MaxCarouselViewHeight = newMaxHeight;
            }
        }

        return newHeight;
    }

    private void OnCarouselViewHeightChangedMessageReceived(object recipient, ResizeCarouselViewMessage message)
    {
        if (CurrentPositionMediaImageViewModel == message.Value)
        {
            UpdateCarouselViewHeight();
        }
    }
}
