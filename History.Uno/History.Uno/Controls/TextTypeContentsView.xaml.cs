using History.Uno.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI;

namespace History.Uno.Controls;

/// <summary>
/// Builds a TextBlock from a TextTypeContentsViewModel's runs (plain text, links,
/// profile mentions, and hashtags). Links open in the system browser, profile mentions
/// navigate to the user page, and hashtags are not yet implemented.
/// Uses TextBlock with Inlines instead of RichTextBlock, because RichTextBlock has no
/// platform implementation in Uno (it renders nothing on Android/iOS).
/// </summary>
public sealed partial class TextTypeContentsView : UserControl
{
    private static readonly SolidColorBrush PrimaryBrush = new(Color.FromArgb(0xFF, 0xED, 0x66, 0x4D));

    // Uno does not mute the Tapped gesture when a Hyperlink inside a TextBlock is clicked
    // (CompleteGesture has no effect for the bubbling Tapped event), so the card-level tap
    // would fire along with the hyperlink click. This flag consumes the subsequent Tapped.
    private long _lastHyperlinkClickTicks;

    public TextTypeContentsView()
    {
        InitializeComponent();
        MainTextBlock.Tapped += OnMainTextBlockTapped;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnMainTextBlockTapped(object sender, TappedRoutedEventArgs args)
    {
        if (Environment.TickCount64 - _lastHyperlinkClickTicks > 300) return;

        // The tap was already consumed by a hyperlink click, so it must not bubble to the card.
        args.Handled = true;
        _lastHyperlinkClickTicks = 0;
    }

    private void MarkHyperlinkClick() => _lastHyperlinkClickTicks = Environment.TickCount64;

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not TextTypeContentsViewModel viewModel) return;

        RebuildContent(viewModel);
    }

    private void RebuildContent(TextTypeContentsViewModel viewModel)
    {
        MainTextBlock.Inlines.Clear();
        foreach (var run in viewModel.Runs)
        {
            var inline = CreateInline(run);
            if (inline != null) MainTextBlock.Inlines.Add(inline);
        }
    }

    private Inline CreateInline(TextContentRun run)
    {
        switch (run.Kind)
        {
            case TextContentRunKind.Link:
                var linkHyperlink = CreateHyperlink(run.Text, PrimaryBrush);
                linkHyperlink.Click += (_, _) => OnLinkHyperlinkClicked(run.Target);
                return linkHyperlink;

            case TextContentRunKind.Profile:
                var profileHyperlink = CreateHyperlink(run.Text, PrimaryBrush, isBold: true);
                profileHyperlink.Click += (_, _) => OnProfileHyperlinkClicked(run.Target);
                return profileHyperlink;

            case TextContentRunKind.Hashtag:
                var hashtagHyperlink = CreateHyperlink(run.Text, PrimaryBrush, isBold: true);
                hashtagHyperlink.Click += OnHashtagHyperlinkClicked;
                return hashtagHyperlink;

            default:
                var runText = new Run { Text = run.Text, FontWeight = run.IsBold ? FontWeights.Bold : FontWeights.Normal };
                if (run.ColorHex != null) runText.Foreground = ParseColorBrush(run.ColorHex);
                return runText;
        }
    }

    private void OnLinkHyperlinkClicked(string target)
    {
        MarkHyperlinkClick();
        _ = Launcher.LaunchUriAsync(new Uri(target));
    }

    private void OnProfileHyperlinkClicked(string target)
    {
        MarkHyperlinkClick();
        _ = App.PushAsync(typeof(Pages.UserPage), target);
    }

    private async void OnHashtagHyperlinkClicked(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        MarkHyperlinkClick();
        // TODO: Navigate to EditPostPage pre-filled with the hashtag (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "해시태그 검색은 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    private static Hyperlink CreateHyperlink(string text, SolidColorBrush brush, bool isBold = false)
    {
        var hyperlink = new Hyperlink { Foreground = brush };
        hyperlink.Inlines.Add(new Run { Text = text, FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal });
        return hyperlink;
    }

    private static SolidColorBrush ParseColorBrush(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6 && hex.Length != 8) return new SolidColorBrush(Color.FromArgb(0xFF, 0x80, 0x80, 0x80)); // Gray

        var red = Convert.ToByte(hex[..2], 16);
        var green = Convert.ToByte(hex.Substring(2, 2), 16);
        var blue = Convert.ToByte(hex.Substring(4, 2), 16);
        var alpha = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)0xFF;
        return new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
    }
}
