using History.Commons.DataTypes.Contents;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace History.WindowsClient.Controls;

public sealed partial class BodyContentControl : UserControl
{
    private const double StickerImageWidth = 80;

    public static readonly DependencyProperty ContentsProperty = DependencyProperty.Register(nameof(Contents), typeof(List<BaseContent>), typeof(BodyContentControl), new PropertyMetadata(null, OnContentsPropertyChanged));

    private readonly BodyContentViewModel _viewModel = new();

    public BodyContentControl() => InitializeComponent();

    public List<BaseContent> Contents
    {
        get => (List<BaseContent>)GetValue(ContentsProperty);
        set => SetValue(ContentsProperty, value);
    }

    private static Color AccentColor => (Color)Application.Current.Resources["SystemAccentColor"];

    private static void OnContentsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((BodyContentControl)sender).Rebuild();

    private void Rebuild()
    {
        MainRichTextBlock.Blocks.Clear();
        _viewModel.Update(Contents);
        if (_viewModel.Segments.Count == 0) return;

        var paragraph = new Paragraph();
        foreach (var segment in _viewModel.Segments) AppendInline(paragraph.Inlines, segment);
        MainRichTextBlock.Blocks.Add(paragraph);
    }

    private void AppendInline(InlineCollection inlines, BodyContentSegmentViewModel segment)
    {
        switch (segment)
        {
            case TextSegmentViewModel text: AppendTextInlines(inlines, text.Text); break;
            case UrlSegmentViewModel url: AppendUrlInline(inlines, url.Url); break;
            case HyperlinkSegmentViewModel hyperlink: AppendUrlInline(inlines, hyperlink.Url); break;
            case ProfileSegmentViewModel profile: AppendProfileInline(inlines, profile); break;
            case HashtagSegmentViewModel hashtag: AppendHashtagInline(inlines, hashtag); break;
            case StickerSegmentViewModel sticker: AppendStickerInline(inlines, sticker); break;
        }
    }

    private static void AppendTextInlines(InlineCollection inlines, string text)
    {
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        for (var index = 0; index < lines.Length; index++)
        {
            if (index > 0) inlines.Add(new LineBreak());
            if (lines[index].Length > 0) inlines.Add(new Run { Text = lines[index] });
        }
    }

    private static void AppendUrlInline(InlineCollection inlines, string url)
    {
        var hyperlink = CreateHyperlink(text: url, isBold: false);
        hyperlink.Click += async (_, _) => await OpenInBrowserAsync(url);
        inlines.Add(hyperlink);
    }

    private static void AppendProfileInline(InlineCollection inlines, ProfileSegmentViewModel segment)
    {
        var hyperlink = CreateHyperlink(text: segment.Nickname, isBold: true);
        hyperlink.Click += (_, _) =>
        {
            // TODO: Navigate to the user profile page once it is implemented.
        };
        inlines.Add(hyperlink);
    }

    private static void AppendHashtagInline(InlineCollection inlines, HashtagSegmentViewModel segment)
    {
        var hyperlink = CreateHyperlink(text: "#" + segment.Tag, isBold: true);
        hyperlink.Click += (_, _) =>
        {
            // TODO: Navigate to the hashtag page once it is implemented.
        };
        inlines.Add(hyperlink);
    }

    // GIF/WebP animation is not supported by BitmapImage; only the first frame is shown.
    private static void AppendStickerInline(InlineCollection inlines, StickerSegmentViewModel segment)
    {
        var image = new Image { Width = StickerImageWidth, Stretch = Stretch.Uniform, Source = new BitmapImage(new Uri(segment.ImageUri)) };
        image.Tapped += (_, _) =>
        {
            // TODO: Navigate to the sticker detail page once it is implemented.
        };
        inlines.Add(new InlineUIContainer { Child = image });
    }

    private static Hyperlink CreateHyperlink(string text, bool isBold)
    {
        var run = new Run { Text = text };
        if (isBold) run.FontWeight = FontWeights.Bold;
        return new Hyperlink { Foreground = new SolidColorBrush(AccentColor), Inlines = { run } };
    }

    private static async Task OpenInBrowserAsync(string url)
    {
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) return;
        await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
    }
}