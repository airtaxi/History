namespace History.MobileClient.ViewModels;

internal class MediaTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }
    public DataTemplate ImageTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ImageViewModel) return ImageTemplate;
        else if (item is VideoViewModel) return VideoTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
