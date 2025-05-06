using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public class MediaContentViewModel(MediaContent mediaContent, IEnumerable<MediaContent> allMediaContents) : IContentViewModel
{
    public IEnumerable<MediaContent> AllMediaContents { get; } = allMediaContents;

    public IMediaViewModel Media
    {
        get
        {
            var viewModel = Utils.GenerateMediaViewModelFromMediaContent(mediaContent, false);
            if (viewModel is ImageViewModel imageViewModel)
            {
                imageViewModel.Aspect = Aspect.AspectFit;
                imageViewModel.HorizontalContentOptions = LayoutOptions.Start;
                imageViewModel.VerticalContentOptions = LayoutOptions.Start;
            }
            return viewModel;
        }
    }
}
