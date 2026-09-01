using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.WindowsClient.ViewModels.Media;

namespace History.WindowsClient.ViewModels;

// Wraps a batch of consecutive media contents for the WrappedMediaContentControl.
// Owns the carousel state view model so the carousel position and decoded media sources
// survive ItemsRepeater element recycling: the control binds to this view model instead of
// recreating its own on every realization, which would reset the position and reload images.
// The carousel view model is created lazily so off-screen posts do not build media view models.
public sealed partial class WrappedMediaContentItemViewModel(List<MediaContent> mediaContents, List<MediaContent> allMediaContents, PostType postType, bool isParentPost) : IContentViewModel
{
    private WrappedMediaContentsViewModel _carouselViewModel;

    public WrappedMediaContentsViewModel CarouselViewModel => _carouselViewModel ??= CreateCarouselViewModel();

    public List<MediaContent> MediaContents { get; } = mediaContents;
    public List<MediaContent> AllMediaContents { get; } = allMediaContents;
    public PostType PostType { get; } = postType;
    public bool IsParentPost { get; } = isParentPost;

    private WrappedMediaContentsViewModel CreateCarouselViewModel()
    {
        var carouselViewModel = new WrappedMediaContentsViewModel();
        carouselViewModel.Update(MediaContents, AllMediaContents, PostType, IsParentPost);
        return carouselViewModel;
    }
}