namespace History.MobileClient.ViewModels;

internal class MediaTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }
    public DataTemplate ImageTemplate { get; set; }
    public DataTemplate FullScreenImageTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ImageViewModel imageViewModel) return imageViewModel.IsFullScreen ? FullScreenImageTemplate : ImageTemplate;
        else if (item is VideoViewModel) return VideoTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
