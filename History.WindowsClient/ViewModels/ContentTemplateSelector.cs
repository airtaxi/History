using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

// Selects the content-slot template for each wrapped content item kind.
// The templates themselves live in Resources/Post.xaml and host the existing
// content UserControls (media/text/poll/external URL) or a plain sticker image.
public partial class ContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate WrappedMediaTemplate { get; set; }

    public DataTemplate BodyTemplate { get; set; }

    public DataTemplate StickerTemplate { get; set; }

    public DataTemplate PollTemplate { get; set; }

    public DataTemplate ExternalUrlTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) => item switch
    {
        WrappedMediaContentItemViewModel => WrappedMediaTemplate,
        BodyContentItemViewModel => BodyTemplate,
        StickerContentItemViewModel => StickerTemplate,
        PollContentItemViewModel => PollTemplate,
        ExternalUrlContentItemViewModel => ExternalUrlTemplate,
        _ => null,
    };

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}