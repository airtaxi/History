using History.Commons.DataTypes.Contents;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SpeakLink.Mention;
using History.MobileClient.DataTypes;
using CommunityToolkit.Mvvm.Messaging;



#if ANDROID
using Android.Views;
using AndroidX.AppCompat.Widget;
#elif IOS
using UIKit;
#endif

namespace History.MobileClient.ContentViews.EditPost;

public partial class TextContentView : ContentView
{
    public MentionsViewModel MentionsViewModel => ViewModel;
    public MentionEditor MentionEditor => MainMentionEditor;

    public string Text
    {
        get => MainMentionEditor.Text;
        set => MainMentionEditor.Text = value;
    }

    public string Placeholder
    {
        get => MainMentionEditor.Placeholder;
        set => MainMentionEditor.Placeholder = value;
    }

    public event EventHandler<string> ImageInputRequested;
    public TextContentView()
	{
		InitializeComponent();
        ViewModel.ImageInputRequested += OnImageInputRequested;
    }

    private void OnImageInputRequested(object sender, string path) => ImageInputRequested?.Invoke(this, path);

    public List<BaseContent> GetContents()
    {
        var result = new List<BaseContent>();
        if (MainMentionEditor?.FormattedText?.Spans != null)
        {
            foreach (var span in MainMentionEditor.FormattedText.Spans)
            {
                if (span is MentionSpan mentionSpan)
                {
                    var index = int.Parse(mentionSpan.MentionId);
                    var isUser = MentionHelper.IsUser(index);

                    if (isUser)
                    {
                        var profileContent = MentionHelper.GetProfileContent(index);
                        if (profileContent == null) continue;

                        result.Add(profileContent);
                    }
                    else
                    {
                        var stickerContent = MentionHelper.GetStickerContent(index);
                        if (stickerContent == null) continue;

                        result.Add(stickerContent);
                    }
                }
                else result.Add(new TextContent() { Text = span.Text });
            }
        }
        return result;
    }

    public void SetContents(List<BaseContent> contents)
    {
        MainMentionEditor.Text = "";

        foreach (var content in contents)
        {
            if (content is ProfileContent profileContent) MentionHelper.AppendUser(MainMentionEditor, profileContent.UserId, profileContent.Nickname);
            if (content is StickerContent stickerContent) MentionHelper.AppendSticker(MainMentionEditor, stickerContent.StickerId, stickerContent.StickerContentId);
            else if (content is TextContent textContent) MentionHelper.AppendText(MainMentionEditor, textContent.Text);
        }
    }

    public void FocusEditor()
    {
        MainMentionEditor.Focus();
        MainMentionEditor.CursorPosition = MainMentionEditor.Text?.Length ?? 0;
    }

    public void UnfocusEditor() => MainMentionEditor.Unfocus();

    public void InsertUser(MentionUserViewModel viewModel)
    {
        if (viewModel == null) return;

        MentionHelper.InsertUser(MainMentionEditor, viewModel.UserId, viewModel.Nickname);
    }

    public void InsertSticker(MentionStickerViewModel viewModel)
    {
        if (viewModel == null) return;

        MentionHelper.InsertSticker(MainMentionEditor, viewModel.StickerId, viewModel.StickerContentId);

        // Send sticker usage record (process in background)
        _ = MentionsViewModel.RecordStickerUsageAsync(viewModel.StickerId, viewModel.StickerContentId);
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        ViewModel.ImageInputRequested -= OnImageInputRequested;
        ImageInputRequested = null;
    }

    private string _lastTextValue;
    private void OnMainMentionEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        var newTextValue = e.NewTextValue;
        if (_lastTextValue != null && newTextValue.Length > 0 &&  newTextValue.Last() == '\n' && newTextValue[..(newTextValue.Length - 1)] == _lastTextValue)
            WeakReferenceMessenger.Default.Send(new MentionEditorNewLineMessage());
        _lastTextValue = e.NewTextValue;
    }
}