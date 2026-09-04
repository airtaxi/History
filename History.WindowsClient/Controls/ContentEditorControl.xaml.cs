using System.Text;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace History.WindowsClient.Controls;

public sealed partial class ContentEditorControl : UserControl
{
    private const string ZeroWidthSpace = "\u200B";
    private const string ObjectReplacementCharacter = "\uFFFC";
    private const double StickerImageWidthRequest = 80;
    private const double StickerFallbackImageWidth = 160;
    private const double StickerFallbackImageHeight = 90;

    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(ContentEditorControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty AllowHashtagProperty = DependencyProperty.Register(nameof(AllowHashtag), typeof(bool), typeof(ContentEditorControl), new PropertyMetadata(true, OnAllowHashtagChanged));

    private ContentEditorViewModel _viewModel;

    public ContentEditorControl() => InitializeComponent();

    public void Initialize(BaseViewModel baseViewModel) => _viewModel = new(baseViewModel);

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    // When false, the '#' prefix is removed so hashtag suggestions are not offered (e.g. comments).
    public bool AllowHashtag
    {
        get => (bool)GetValue(AllowHashtagProperty);
        set => SetValue(AllowHashtagProperty, value);
    }

    private static void OnAllowHashtagChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not ContentEditorControl control) return;
        control.UpdatePrefixes();
    }

    private void UpdatePrefixes() => MainRichSuggestBox.Prefixes = AllowHashtag ? "@#" : "@";

    // When true, @-mention suggestions use the logged-in Kakao Story friends (CommonShared.KakaoFriends)
    // instead of the History friends (CommonShared.Friends).
    public bool IsKakaoMentionMode
    {
        get => _viewModel?.IsKakaoMentionMode ?? false;
        set => _viewModel?.IsKakaoMentionMode = value;
    }

    // Plain document text with token ZWSP padding and inline image objects removed.
    public string Text
    {
        get => GetPlainText();
        set => SetPlainText(value ?? string.Empty);
    }

    private RichEditTextDocument Document => MainRichSuggestBox.TextDocument;

    private static Color AccentColor => (Color)Application.Current.Resources["SystemAccentColor"];

    // Raised when the user pastes an image into the editor. The handler receives a
    // temporary file path containing the pasted image data.
    public event EventHandler<string> ImageInputRequested;

    // Raised when the user presses Ctrl+Enter in the editor. The handler can submit
    // the current editor contents (e.g. send a comment).
    public event EventHandler SubmitRequested;

    private void OnMainRichSuggestBoxPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter) return;
        if (!InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down)) return;

        e.Handled = true;
        SubmitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnMainRichSuggestBoxSuggestionRequested(RichSuggestBox2 sender, SuggestionRequestedEventArgs args)
    {
        if (args.Prefix == "@") sender.ItemsSource = _viewModel?.GetUserSuggestions(args.QueryText);
        else if (args.Prefix == "#") sender.ItemsSource = _viewModel?.GetHashtagSuggestions(args.QueryText);
    }

    private async void OnMainRichSuggestBoxPaste(RichSuggestBox2 sender, TextControlPasteEventArgs args)
    {
        var clipboard = Clipboard.GetContent();
        if (!clipboard.Contains(StandardDataFormats.Bitmap)) return;

        // Block the image from being inserted into the document as binary data.
        args.Handled = true;

        var streamReference = await clipboard.GetBitmapAsync();
        using var stream = await streamReference.OpenReadAsync();
        var imageData = new byte[stream.Size];
        using var reader = new DataReader(stream);
        await reader.LoadAsync((uint)stream.Size);
        reader.ReadBytes(imageData);

        var contentType = ImageContentTypeDetector.Detect(imageData);
        var tempPath = Path.Combine(Path.GetTempPath(), $"paste_{DateTime.Now:yyyyMMddHHmmssfff}{GetImageFileExtension(contentType)}");
        await File.WriteAllBytesAsync(tempPath, imageData);

        ImageInputRequested?.Invoke(this, tempPath);
    }

    private static string GetImageFileExtension(string contentType) =>
        contentType switch
        {
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

    private void OnMainRichSuggestBoxSuggestionChosen(RichSuggestBox2 sender, SuggestionChosenEventArgs args)
    {
        if (args.Prefix == "@")
        {
            if (args.SelectedItem is not BaseFriendshipViewModel userViewModel) return;

            args.DisplayText = userViewModel.Nickname;
            args.Format.BackgroundColor = AccentColor;
            args.Format.ForegroundColor = Colors.White;
            args.Format.Bold = FormatEffect.On;
        }
        else if (args.Prefix == "#")
        {
            args.DisplayText = args.SelectedItem as string;
            args.Format.BackgroundColor = Colors.Transparent;
            args.Format.ForegroundColor = AccentColor;
            args.Format.Bold = FormatEffect.On;
        }
    }

    private string GetPlainText()
    {
        var document = Document;
        if (document == null) return string.Empty;

        document.GetText(TextGetOptions.NoHidden, out var text);
        return text?.Replace(ZeroWidthSpace, string.Empty).Replace(ObjectReplacementCharacter, string.Empty) ?? string.Empty;
    }

    private void SetPlainText(string text)
    {
        MainRichSuggestBox.Clear();
        if (text.Length == 0) return;

        var document = Document;
        document.SetText(TextSetOptions.None, text);
        document.Selection.SetRange(0, 0);
    }

    public List<BaseContent> GetContents()
    {
        var result = new List<BaseContent>();
        var document = Document;
        document.GetText(TextGetOptions.None, out var rawText);
        var tokens = MainRichSuggestBox.Tokens
            .Where(token => token.RangeStart.HasValue && token.RangeEnd.HasValue)
            .OrderBy(token => token.RangeStart.Value)
            .ToList();

        var previousEndIndex = 0;
        foreach (var token in tokens)
        {
            var tokenStartIndex = Math.Clamp(token.RangeStart.Value, previousEndIndex, rawText.Length);
            var tokenEndIndex = Math.Clamp(token.RangeEnd.Value, previousEndIndex, rawText.Length);

            if (tokenStartIndex > previousEndIndex)
            {
                var plainText = CleanPlainText(rawText[previousEndIndex..tokenStartIndex]);
                if (!string.IsNullOrEmpty(plainText)) result.Add(new TextContent { Text = plainText });
            }

            // Sticker tokens hold the object replacement character in their range; the item decides the content type.
            if (token.Item is ProfileContent profileContent) result.Add(profileContent);
            else if (token.Item is StickerContent stickerContent) result.Add(stickerContent);
            else
            {
                var displayText = token.DisplayText ?? string.Empty;
                if (displayText.Length > 1 && displayText[0] == '#') result.Add(new HashtagContent { Tag = displayText[1..] });
                else if (displayText.Length > 0) result.Add(new TextContent { Text = displayText });
            }

            previousEndIndex = tokenEndIndex;
        }

        if (previousEndIndex < rawText.Length)
        {
            var remainingText = CleanPlainText(rawText[previousEndIndex..]);
            if (!string.IsNullOrEmpty(remainingText)) result.Add(new TextContent { Text = remainingText });
        }

        return result;
    }

    // Strips token artifacts (ZWSP padding and inline image placeholders) from raw document text.
    private static string CleanPlainText(string rawText) => rawText.Replace(ZeroWidthSpace, string.Empty).Replace(ObjectReplacementCharacter, string.Empty);

    public List<string> GetHashtags() =>
    [.. GetContents()
        .OfType<HashtagContent>()
        .Select(hashtagContent => hashtagContent.Tag)
    ];

    public async Task SetContentsAsync(List<BaseContent> contents)
    {
        MainRichSuggestBox.Clear();
        if (contents == null || contents.Count == 0) return;

        var document = Document;
        if (document == null) return;

        var textBuilder = new StringBuilder();
        var tokens = new List<(RichSuggestToken Token, int StartIndex)>();

        foreach (var content in contents)
        {
            if (content is ProfileContent profileContent)
            {
                tokens.Add((new RichSuggestToken(Guid.NewGuid(), "@" + profileContent.Nickname) { Item = profileContent }, textBuilder.Length));
                textBuilder.Append('@').Append(profileContent.Nickname);
            }
            else if (content is HashtagContent hashtagContent)
            {
                tokens.Add((new RichSuggestToken(Guid.NewGuid(), "#" + hashtagContent.Tag) { Item = hashtagContent }, textBuilder.Length));
                textBuilder.Append('#').Append(hashtagContent.Tag);
            }
            else if (content is TextContent textContent) textBuilder.Append(textContent.Text);
            else if (content is HyperlinkContent hyperlinkContent) textBuilder.Append(hyperlinkContent.Url);
            else if (content is StickerContent stickerContent)
            {
                // The object replacement character reserves the inline image position;
                // the image itself is inserted after the text layout is finalized.
                tokens.Add((new RichSuggestToken(Guid.NewGuid(), ObjectReplacementCharacter) { Item = stickerContent }, textBuilder.Length));
                textBuilder.Append(ObjectReplacementCharacter[0]);
            }
        }

        var text = textBuilder.ToString();
        document.SetText(TextSetOptions.Unhide, text);

        var rangePrototype = document.GetRange(0, 0);
        var formatMention = rangePrototype.CharacterFormat.GetClone();
        formatMention.BackgroundColor = AccentColor;
        formatMention.ForegroundColor = Colors.White;
        formatMention.Bold = FormatEffect.On;

        var formatHashtag = rangePrototype.CharacterFormat.GetClone();
        formatHashtag.BackgroundColor = Colors.Transparent;
        formatHashtag.ForegroundColor = AccentColor;
        formatHashtag.Bold = FormatEffect.On;

        foreach (var (token, startIndex) in tokens)
        {
            var tokenRange = document.GetRange(startIndex, startIndex + token.DisplayText.Length);
            if (token.Item is StickerContent stickerContent)
            {
                await InsertStickerImageAsync(tokenRange, stickerContent);
                PadStickerRange(tokenRange, formatMention);
            }
            else
            {
                // Padding mirrors the control's PadRange so token validation matches the link range text.
                var format = token.DisplayText[0] == '#' ? formatHashtag : formatMention;
                tokenRange.CharacterFormat.SetClone(format);
                PadStickerRange(tokenRange, format);
            }

            tokenRange.Link = $"\"{token.Id}\"";
            MainRichSuggestBox.RegisterTokenRange(token, tokenRange);
        }

        document.Selection.SetRange(text.Length, text.Length);
    }

    public async Task<bool> InsertStickerAsync(StickerContent stickerContent)
    {
        if (stickerContent == null || Document == null) return false;

        var imageData = await CommonUtils.GetStickerImageDataAsync(stickerContent.StickerMediaId);
        if (imageData.Length == 0) return false;

        var document = Document;
        var selection = document.Selection;

        // Insert on a new line when the document already has content.
        var text = GetPlainText();
        var insertPosition = selection.EndPosition;
        if (text.Length > 0 && !text.EndsWith('\n'))
        {
            document.GetText(TextGetOptions.NoHidden, out var documentText);
            var insertRange = document.GetRange(documentText.Length, documentText.Length);
            insertRange.SetText(TextSetOptions.Unhide, "\n");
            insertPosition = documentText.Length + 1;
        }

        var token = new RichSuggestToken(Guid.NewGuid(), ObjectReplacementCharacter) { Item = stickerContent, SkipValidation = true };
        var range = document.GetRange(insertPosition, insertPosition);
        // InsertImage replaces the range content, so the range must not be collapsed;
        // the ZWSP padding reserves a non-empty slot that the image replaces.
        PadStickerRange(range, null);
        await InsertStickerImageAsync(range, stickerContent, imageData);
        var imageRange = document.GetRange(insertPosition, insertPosition + 1);
        MainRichSuggestBox.RegisterTokenRange(token, imageRange);

        document.Selection.SetRange(imageRange.EndPosition, imageRange.EndPosition);
        return true;
    }

    // Pads the range with ZWSPs: gives InsertImage a non-empty slot to replace,
    // and mirrors the control's PadRange token layout (ZWSP + content + ZWSP inside a link).
    private static void PadStickerRange(ITextRange range, ITextCharacterFormat format)
    {
        var startPosition = range.StartPosition;
        var endPosition = range.EndPosition + 1;
        var clone = range.GetClone();
        clone.Collapse(true);
        clone.SetText(TextSetOptions.Unhide, ZeroWidthSpace);
        if (format != null) clone.CharacterFormat.SetClone(format);
        clone.SetRange(endPosition, endPosition);
        clone.SetText(TextSetOptions.Unhide, ZeroWidthSpace);
        if (format != null) clone.CharacterFormat.SetClone(format);
        range.SetRange(startPosition, endPosition + 1);
    }

    private static async Task InsertStickerImageAsync(ITextRange range, StickerContent stickerContent, byte[] imageData = null)
    {
        imageData ??= await CommonUtils.GetStickerImageDataAsync(stickerContent.StickerMediaId);
        if (imageData.Length == 0) return;

        using var imageStream = CreateImageStream(imageData);
        var (width, height) = GetImageSize(imageStream);
        imageStream.Seek(0);
        range.InsertImage(width, height, height, VerticalCharacterAlignment.Baseline, "스티커", imageStream);
    }

    private static InMemoryRandomAccessStream CreateImageStream(byte[] imageData)
    {
        var stream = new InMemoryRandomAccessStream();
        using var outputStream = stream.GetOutputStreamAt(0);
        using var dataWriter = new DataWriter(outputStream);
        dataWriter.WriteBytes(imageData);
        dataWriter.StoreAsync().AsTask().GetAwaiter().GetResult();
        dataWriter.FlushAsync().AsTask().GetAwaiter().GetResult();
        return stream;
    }

    private static (int Width, int Height) GetImageSize(InMemoryRandomAccessStream imageStream)
    {
        try
        {
            imageStream.Seek(0);
            var decoder = BitmapDecoder.CreateAsync(imageStream).AsTask().GetAwaiter().GetResult();
            var scale = StickerImageWidthRequest / decoder.PixelWidth;
            return ((int)Math.Round(decoder.PixelWidth * scale), (int)Math.Round(decoder.PixelHeight * scale));
        }
        catch (Exception) { return ((int)StickerFallbackImageWidth, (int)StickerFallbackImageHeight); }
    }

    public void Clear() => MainRichSuggestBox.Clear();

    public void FocusEditor() => MainRichSuggestBox.Focus(FocusState.Programmatic);
}