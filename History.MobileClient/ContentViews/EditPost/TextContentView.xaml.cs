using History.Commons.DataTypes.Contents;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SuggestingBox.Maui;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.ContentViews.EditPost;

public partial class TextContentView : ContentView
{
    public MentionsViewModel MentionsViewModel => ViewModel;
    public SuggestingBox.Maui.SuggestingBox SuggestingBoxControl => MainSuggestingBox;

    // When true, @-mention suggestions use the logged-in Kakao Story friends (Shared.KakaoFriends)
    // instead of the History friends (Shared.Friends).
    public bool IsKakaoMentionMode { get; set; }

    public string Text
    {
        get => MainSuggestingBox.Text;
        set => MainSuggestingBox.SetContent(value ?? string.Empty, []);
    }

    public string Placeholder
    {
        get => MainSuggestingBox.Placeholder;
        set => MainSuggestingBox.Placeholder = value;
    }

    public event EventHandler<string> ImageInputRequested;

    public TextContentView()
    {
        InitializeComponent();
        MainSuggestingBox.TextChanged += OnSuggestingBoxTextChanged;
    }

    private void OnSuggestingBoxTextChanged(object sender, TextChangedEventArgs eventArgs)
    {
        var previousLineCount = (eventArgs.OldTextValue ?? string.Empty).Split('\n').Length;
        var currentLineCount = (eventArgs.NewTextValue ?? string.Empty).Split('\n').Length;
        if (currentLineCount > previousLineCount) WeakReferenceMessenger.Default.Send(new MentionEditorNewLineMessage());
    }

    private void OnSuggestionRequested(SuggestingBox.Maui.SuggestingBox sender, SuggestionRequestedEventArgs args)
    {
        if (args.Prefix == "@")
        {
            var query = args.QueryText.Trim();
            var viewModels = IsKakaoMentionMode ? BuildKakaoMentionViewModels(query) : BuildHistoryMentionViewModels(query);
            ViewModel.UserViewModels = viewModels;

            sender.ItemTemplate = (DataTemplate)Resources["UserMentionTemplate"];
            sender.ItemsSource = viewModels;
        }
        else if (args.Prefix == "#")
        {
            var query = args.QueryText.Trim();
            sender.ItemTemplate = (DataTemplate)Resources["HashtagTemplate"];
            if (!string.IsNullOrEmpty(query)) sender.ItemsSource = new List<string> { query };
            else sender.ItemsSource = null;
        }
    }

    private static List<MentionUserViewModel> BuildHistoryMentionViewModels(string query)
    {
        if (string.IsNullOrEmpty(query)) return [.. Shared.Friends.Select(friendUser => new MentionUserViewModel(friendUser))];

        return [.. Shared.Friends
            .Where(friendUser => friendUser.Handle.Contains(query, StringComparison.InvariantCultureIgnoreCase)
                || friendUser.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase)
                || KoreanHelper.SplitToChosung(friendUser.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(friendUser => new MentionUserViewModel(friendUser))];
    }

    private static List<MentionUserViewModel> BuildKakaoMentionViewModels(string query)
    {
        if (string.IsNullOrEmpty(query)) return [.. Shared.KakaoFriends.Select(profile => new MentionUserViewModel(profile))];

        return [.. Shared.KakaoFriends
            .Where(profile => profile.display_name != null && (profile.display_name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || KoreanHelper.SplitToChosung(profile.display_name).Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Select(profile => new MentionUserViewModel(profile))];
    }

    private void OnSuggestionChosen(SuggestingBox.Maui.SuggestingBox sender, SuggestionChosenEventArgs args)
    {
        if (args.Prefix == "@")
        {
            var userViewModel = args.SelectedItem as MentionUserViewModel;
            if (userViewModel != null)
            {
                args.DisplayText = userViewModel.Nickname;
                args.Item = new ProfileContent { UserId = userViewModel.UserId, Nickname = userViewModel.Nickname };
                args.Format.ForegroundColor = Colors.White;
                args.Format.BackgroundColor = Application.Current.Resources["Primary"] as Color;
                args.Format.Bold = FormatEffect.On;
            }
        }
        else if (args.Prefix == "#")
        {
            args.DisplayText = args.SelectedItem as string;
            args.Format.BackgroundColor = Colors.Transparent;
            args.Format.ForegroundColor = Application.Current.Resources["Primary"] as Color;
            args.Format.Bold = FormatEffect.On;
        }
    }

    private void OnImagePasteRequested(SuggestingBox.Maui.SuggestingBox sender, ImagePasteRequestedEventArgs args)
    {
        var tempPath = Path.Combine(FileSystem.CacheDirectory, $"paste_{DateTime.Now:yyyyMMddHHmmssfff}{GetImageFileExtension(args.ContentType)}");
        File.WriteAllBytes(tempPath, args.ImageData);
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

    public List<BaseContent> GetContents() => MentionHelper.GetContents(MainSuggestingBox);

    public List<string> GetHashtags() => MentionHelper.GetHashtags(MainSuggestingBox);

    public string GetTextWithImageTokenReplacement(string replacementText) => (MainSuggestingBox.Text ?? string.Empty).Replace(SuggestingBoxText.ImagePlaceholderString, replacementText ?? string.Empty);

    public async Task SetContentsAsync(List<BaseContent> contents)
    {
        var tokens = new List<SuggestingBoxTokenInfo>();
        string text = string.Empty;

        foreach (var content in contents)
        {
            if (content is ProfileContent profileContent)
            {
                string tokenText = "@" + profileContent.Nickname;
                var mentionFormat = new SuggestionFormat
                {
                    ForegroundColor = Colors.White,
                    BackgroundColor = Application.Current.Resources["Primary"] as Color,
                    Bold = FormatEffect.On
                };
                tokens.Add(new SuggestingBoxTokenInfo(text.Length, "@", profileContent.Nickname, mentionFormat, profileContent));
                text += tokenText;
            }
            else if (content is StickerContent stickerContent)
            {
                var stickerToken = await MentionHelper.CreateStickerImageTokenAsync(text.Length, stickerContent);
                if (stickerToken != null)
                {
                    tokens.Add(stickerToken);
                    text += SuggestingBoxText.ImagePlaceholderString;
                }
                else
                {
                    var fallbackToken = MentionHelper.CreateStickerFallbackToken(text.Length, stickerContent);
                    tokens.Add(fallbackToken);
                    text += fallbackToken.FullText;
                }
            }
            else if (content is HashtagContent hashtagContent)
            {
                string tokenText = "#" + hashtagContent.Tag;
                var hashtagFormat = new SuggestionFormat
                {
                    BackgroundColor = Colors.Transparent,
                    ForegroundColor = Application.Current.Resources["Primary"] as Color,
                    Bold = FormatEffect.On
                };
                tokens.Add(new SuggestingBoxTokenInfo(text.Length, "#", hashtagContent.Tag, hashtagFormat, hashtagContent));
                text += tokenText;
            }
            else if (content is TextContent textContent) text += textContent.Text;
            else if (content is HyperlinkContent hyperlinkContent) text += hyperlinkContent.Url;
        }

        MainSuggestingBox.SetContent(text, tokens);
    }

    public void FocusEditor()
    {
        MainSuggestingBox.Focus();
    }

    public void UnfocusEditor() => MainSuggestingBox.Unfocus();

    public void InsertUser(MentionUserViewModel viewModel)
    {
        if (viewModel == null) return;
        MentionHelper.InsertUser(MainSuggestingBox, viewModel.UserId, viewModel.Nickname);
    }

    public async Task<bool> InsertStickerAsync(MentionStickerViewModel viewModel)
    {
        if (viewModel == null) return false;

        var inserted = await MentionHelper.InsertStickerAsync(MainSuggestingBox, viewModel.StickerContent);
        if (!inserted) return false;

        // Send sticker usage record (process in background)
        _ = MentionsViewModel.RecordStickerUsageAsync(viewModel.StickerId, viewModel.StickerContentId);
        return true;
    }

    private void OnUnloaded(object sender, EventArgs e) => ImageInputRequested = null;
}
