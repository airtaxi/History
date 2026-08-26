namespace History.MobileClient.ViewModels;

internal class MediaTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }
    public DataTemplate ImageTemplate { get; set; }
    public DataTemplate AppleImageTemplate { get; set; }
    public DataTemplate FullScreenImageTemplate { get; set; }
    public DataTemplate FullScreenVideoTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
#if IOS
        if (item is ImageViewModel imageViewModel) return imageViewModel.IsFullScreen ? FullScreenImageTemplate : ImageTemplate;
#else
        if (item is ImageViewModel imageViewModel) return imageViewModel.IsFullScreen ? FullScreenImageTemplate : ImageTemplate;
#endif
        else if (item is VideoViewModel videoViewModel) return videoViewModel.IsFullScreen ? FullScreenVideoTemplate : VideoTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
