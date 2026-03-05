using History.Commons.DataTypes.Contents;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SuggestingBox.Maui;
using History.MobileClient.DataTypes;
using CommunityToolkit.Mvvm.Messaging;

namespace History.MobileClient.ContentViews.EditPost;

public partial class TextContentView : ContentView
{
    public MentionsViewModel MentionsViewModel => ViewModel;
    public SuggestingBox.Maui.SuggestingBox SuggestingBoxControl => MainSuggestingBox;

    public string Text
    {
        get => MainSuggestingBox.Text;
        set => MainSuggestingBox.Text = value;
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
        if (currentLineCount > previousLineCount)
            WeakReferenceMessenger.Default.Send(new MentionEditorNewLineMessage());
    }

    private void OnSuggestionRequested(SuggestingBox.Maui.SuggestingBox sender, SuggestionRequestedEventArgs args)
    {
        if (args.Prefix == "@")
        {
            var query = args.QueryText.Trim();
            List<MentionUserViewModel> viewModels;
            if (string.IsNullOrEmpty(query)) viewModels = [.. Shared.Friends.Select(friendUser => new MentionUserViewModel(friendUser))];
            else viewModels = [.. Shared.Friends
                    .Where(friendUser => friendUser.Handle.Contains(query, StringComparison.InvariantCultureIgnoreCase)
                        || friendUser.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || KoreanHelper.SplitToChosung(friendUser.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Select(friendUser => new MentionUserViewModel(friendUser))];
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

    private void OnImageInserted(SuggestingBox.Maui.SuggestingBox sender, ImageInsertedEventArgs args)
    {
        // Save the image data to a temporary file and raise the event
        var tempPath = Path.Combine(FileSystem.CacheDirectory, $"paste_{DateTime.Now:yyyyMMddHHmmss}.png");
        File.WriteAllBytes(tempPath, args.ImageData);
        ImageInputRequested?.Invoke(this, tempPath);
    }

    public List<BaseContent> GetContents() => MentionHelper.GetContents(MainSuggestingBox);

    public List<string> GetHashtags() => MentionHelper.GetHashtags(MainSuggestingBox);

    public void SetContents(List<BaseContent> contents)
    {
        var tokens = new List<SuggestingBoxTokenInfo>();
        string text = string.Empty;

        foreach (var content in contents)
        {
            if (content is ProfileContent profileContent)
            {
                string tokenText = "@" + profileContent.Nickname;
                tokens.Add(new SuggestingBoxTokenInfo(text.Length, "@", profileContent.Nickname,
                    new SuggestionFormat
                    {
                        ForegroundColor = Colors.White,
                        BackgroundColor = Application.Current.Resources["Primary"] as Color,
                        Bold = FormatEffect.On
                    }, profileContent));
                text += tokenText;
            }
            else if (content is StickerContent stickerContent)
            {
                string displayText = " * 스티커 * ";
                string tokenText = "@" + displayText;
                tokens.Add(new SuggestingBoxTokenInfo(text.Length, "@", displayText,
                    new SuggestionFormat
                    {
                        ForegroundColor = Colors.White,
                        BackgroundColor = Application.Current.Resources["Primary"] as Color,
                        Bold = FormatEffect.On
                    }, stickerContent));
                text += tokenText;
            }
            else if (content is HashtagContent hashtagContent)
            {
                string tokenText = "#" + hashtagContent.Tag;
                tokens.Add(new SuggestingBoxTokenInfo(text.Length, "#", hashtagContent.Tag,
                    new SuggestionFormat
                    {
                        BackgroundColor = Colors.Transparent,
                        ForegroundColor = Application.Current.Resources["Primary"] as Color,
                        Bold = FormatEffect.On
                    }, hashtagContent));
                text += tokenText;
            }
            else if (content is TextContent textContent)
                text += textContent.Text;
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

    public void InsertSticker(MentionStickerViewModel viewModel)
    {
        if (viewModel == null) return;
        MentionHelper.InsertSticker(MainSuggestingBox, viewModel.StickerId, viewModel.StickerContentId);

        // Send sticker usage record (process in background)
        _ = MentionsViewModel.RecordStickerUsageAsync(viewModel.StickerId, viewModel.StickerContentId);
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        ImageInputRequested = null;
    }
}