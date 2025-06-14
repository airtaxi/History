using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;

namespace History.MobileClient.ViewModels;

public partial class WrappedMediaContentsViewModel : ObservableObject, IContentViewModel
{
    private double _lastCarouselViewHeight = 0;

    public double MaxCarouselViewHeight { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarouselPositionText))]
    [NotifyPropertyChangedFor(nameof(CarouselViewHeight))]
    public partial int CarouselPosition { get; set; }
    public string CarouselPositionText => $"{CarouselPosition + 1} / {_mediaContentsCount}";
    public double CarouselViewHeight
    {
        get
        {
            if (CurrentPositionMediaImageViewModel == null) return _lastCarouselViewHeight;

            var carouselView = CurrentPositionMediaImageViewModel.CarouselView;
            if (carouselView == null) return _lastCarouselViewHeight;

            if (_isTimeline)
            {
                var targetMaxAspectRatio = 1; // 1:1 aspect ratio for timeline
                var carouselViewWidth = carouselView.Width;
                MaxCarouselViewHeight = carouselViewWidth / targetMaxAspectRatio;
            }

            var width = CurrentPositionMediaImageViewModel.ImageWidth;
            var height = CurrentPositionMediaImageViewModel.ImageHeight;
            var aspectRatio = (double)width / height;

            var newHeight = carouselView.Width / aspectRatio;
            _lastCarouselViewHeight = newHeight;
            return newHeight;
        }
    }

    // Single media content won't be scrolled
    public bool CarouselSwipeEnabled { get; }

    public List<MediaContentViewModel> Medias { get; }
    public MediaContentViewModel FirstMedia { get; }

    private ImageViewModel CurrentPositionMediaImageViewModel => Medias[CarouselPosition].ImageMedia as ImageViewModel;

    private readonly int _mediaContentsCount;
    private readonly bool _isTimeline;

    public WrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents, bool isTimeline, bool isParentPost = false)
    {
        _isTimeline = isTimeline;
        _mediaContentsCount = mediaContents.Count();
        CarouselSwipeEnabled = _mediaContentsCount > 1;

        if (_isTimeline) MaxCarouselViewHeight = 400;
        else MaxCarouselViewHeight = double.PositiveInfinity;

        var medias = mediaContents.Select(m => new MediaContentViewModel(m, allMediaContents, isTimeline, isParentPost)).ToList();
        FirstMedia = medias.FirstOrDefault() ?? throw new InvalidOperationException("No media contents available.");
        Debug.WriteLine($"FIRST MEDIA: {FirstMedia.Media.Uri}");
        Medias = medias;

        WeakReferenceMessenger.Default.Register<ResizeCarouselViewMessage>(this, OnCarouselViewHeightChangedMessageReceived);
    }

    public void UpdateCarouselViewHeight() => OnPropertyChanged(nameof(CarouselViewHeight));

    private void OnCarouselViewHeightChangedMessageReceived(object recipient, ResizeCarouselViewMessage message)
    {
        if (CurrentPositionMediaImageViewModel == message.Value)
        {
            UpdateCarouselViewHeight();
        }
    }
}
