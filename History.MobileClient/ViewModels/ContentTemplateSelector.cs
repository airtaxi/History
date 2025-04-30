namespace History.MobileClient.ViewModels;

internal class ContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate TextAndProfileContentsTemplate { get; set; }
    public DataTemplate StickerContentTemplate { get; set; }
    public DataTemplate MediaContentTemplate { get; set; }
    public DataTemplate MediaContentsTemplate { get; set; }
    public DataTemplate MediasContentTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is TextAndProfileContentsViewModel) return TextAndProfileContentsTemplate;
        else if (item is StickerContentViewModel) return StickerContentTemplate;
        else if (item is MediaContentViewModel) return MediaContentTemplate;
        else if (item is MediaContentsViewModel) return MediaContentsTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
