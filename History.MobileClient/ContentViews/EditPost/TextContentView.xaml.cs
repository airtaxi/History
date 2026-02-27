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
            ViewModel.IsDisplayingMentions = viewModels.Count > 0;
            ViewModel.IsDisplayingUserMentions = ViewModel.IsDisplayingMentions;
            ViewModel.IsDisplayingStickerMentions = false;

            // Set as display names for the suggestion popup
            sender.ItemsSource = viewModels.Select(viewModel => viewModel.Nickname).ToList();
        }
        else if (args.Prefix == "#")
        {
            // For hashtags, offer the typed text as a suggestion
            var query = args.QueryText.Trim();
            if (!string.IsNullOrEmpty(query)) sender.ItemsSource = new List<string> { query };
            else sender.ItemsSource = null;
        }
        else
        {
            ViewModel.IsDisplayingMentions = false;
            ViewModel.IsDisplayingUserMentions = false;
            ViewModel.IsDisplayingStickerMentions = false;
        }
    }

    private void OnSuggestionChosen(SuggestingBox.Maui.SuggestingBox sender, SuggestionChosenEventArgs args)
    {
        if (args.Prefix == "@")
        {
            // Find the matching user viewmodel by nickname
            var nickname = args.SelectedItem as string;
            var userViewModel = ViewModel.UserViewModels?.FirstOrDefault(viewModel => viewModel.Nickname == nickname);
            if (userViewModel != null)
            {
                args.DisplayText = userViewModel.Nickname;
                args.Format.ForegroundColor = Color.FromArgb("#6750A4");
                args.Format.Bold = FormatEffect.On;
            }

            ViewModel.IsDisplayingMentions = false;
            ViewModel.IsDisplayingUserMentions = false;
        }
        else if (args.Prefix == "#")
        {
            args.DisplayText = args.SelectedItem as string;
            args.Format.BackgroundColor = Colors.LightSlateGray;
            args.Format.ForegroundColor = Colors.White;
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
                        ForegroundColor = Color.FromArgb("#6750A4"),
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
                        BackgroundColor = Colors.LightGray,
                        Bold = FormatEffect.On
                    }, stickerContent));
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