using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

internal class ContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate TextTypeContentsTemplate { get; set; }
    public DataTemplate StickerContentTemplate { get; set; }
    public DataTemplate MediaContentTemplate { get; set; }
    public DataTemplate ExternalUrlContentTemplate { get; set; }
    public DataTemplate PollContentTemplate { get; set; }
    public DataTemplate AndroidWrappedMediaContentsTemplate { get; set; }
    public DataTemplate AppleWrappedMediaContentsTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is TextTypeContentsViewModel) return TextTypeContentsTemplate;
        else if (item is StickerContentViewModel) return StickerContentTemplate;
        else if (item is MediaContentViewModel) return MediaContentTemplate;
        else if (item is ExternalUrlContentViewModel) return ExternalUrlContentTemplate;
        else if (item is PollContentViewModel) return PollContentTemplate;
#if IOS
        else if (item is WrappedMediaContentsViewModel) return AppleWrappedMediaContentsTemplate;
#else
        else if (item is WrappedMediaContentsViewModel) return AndroidWrappedMediaContentsTemplate;
#endif
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
