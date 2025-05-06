namespace History.MobileClient.ViewModels;

public sealed class FullScreenImageViewModel : ImageViewModel
{
    public FullScreenImageViewModel(ImageViewModel source) : base(source.Uri, source.Description)
    {
        Aspect = Aspect.AspectFit;
    }
}