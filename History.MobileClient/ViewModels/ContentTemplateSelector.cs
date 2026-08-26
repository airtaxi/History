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
        else if (item is BaseMediaContentViewModel) return MediaContentTemplate;
        else if (item is ExternalUrlContentViewModel) return ExternalUrlContentTemplate;
        else if (item is PollContentViewModel) return PollContentTemplate;
#if IOS
        else if (item is BaseWrappedMediaContentsViewModel) return AppleWrappedMediaContentsTemplate;
#else
        else if (item is BaseWrappedMediaContentsViewModel) return AndroidWrappedMediaContentsTemplate;
#endif
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
