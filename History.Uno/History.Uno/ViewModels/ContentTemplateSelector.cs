using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.Uno.ViewModels;

public class ContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate TextTypeContentsTemplate { get; set; }
    public DataTemplate StickerContentTemplate { get; set; }
    public DataTemplate MediaContentTemplate { get; set; }
    public DataTemplate ExternalUrlContentTemplate { get; set; }
    public DataTemplate PollContentTemplate { get; set; }
    public DataTemplate WrappedMediaContentsTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is TextTypeContentsViewModel) return TextTypeContentsTemplate;
        else if (item is StickerContentViewModel) return StickerContentTemplate;
        else if (item is MediaContentViewModel) return MediaContentTemplate;
        else if (item is ExternalUrlContentViewModel) return ExternalUrlContentTemplate;
        else if (item is PollContentViewModel) return PollContentTemplate;
        else if (item is WrappedMediaContentsViewModel) return WrappedMediaContentsTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
