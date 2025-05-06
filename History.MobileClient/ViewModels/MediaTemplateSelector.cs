namespace History.MobileClient.ViewModels;

internal class MediaTemplateSelector : DataTemplateSelector
{
    public DataTemplate FullScreenImageTemplate { get; set; }
    public DataTemplate FullScreenVideoTemplate { get; set; }

    public DataTemplate VideoTemplate { get; set; }
    // MAUI BUG: Cannot set ShouldShowPlaybackControls on xaml
    public DataTemplate ControllableVideoTemplate { get; set; }
    public DataTemplate ImageTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is FullScreenImageViewModel fullScreenImageViewModel) return FullScreenImageTemplate;
        else if (item is FullScreenVideoViewModel fullScreenVideoViewModel) return FullScreenVideoTemplate;
        else if (item is ImageViewModel) return ImageTemplate;
        else if (item is VideoViewModel videoViewModel)
        {
            if (videoViewModel.VideoShouldShowPlaybackControls) return ControllableVideoTemplate;
            else return VideoTemplate;
        }
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
