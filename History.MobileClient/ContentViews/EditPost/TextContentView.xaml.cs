using History.Commons.DataTypes.Contents;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SpeakLink.Mention;

#if ANDROID
using Android.Views;
using AndroidX.AppCompat.Widget;
#elif IOS
using UIKit;
#endif

namespace History.MobileClient.ContentViews.EditPost;

public partial class TextContentView : ContentView
{
    public MentionsViewModel MentionsViewModel => MainMentionEditor;
    public MentionEditor MentionEditor => MainMentionEditor;

    public string Text => MainMentionEditor.Text;

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

    private void OnUserGridTapped(object sender, TappedEventArgs e)
    {
		var element = sender as Element;
		var viewModel = element.BindingContext as MentionViewModel;

        MentionHelper.InsertMention(MainMentionEditor, viewModel.UserId, viewModel.Nickname);
    }

    public List<BaseContent> GetContents()
    {
        var result = new List<BaseContent>();
        if (MainMentionEditor?.FormattedText?.Spans != null)
        {
            foreach (var span in MainMentionEditor.FormattedText.Spans)
            {
                if (span is MentionSpan mentionSpan) result.Add(new ProfileContent() { UserId = MentionHelper.MentionIdMap[int.Parse(mentionSpan.MentionId)] });
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
            if (content is ProfileContent profileContent) MentionHelper.AppendMention(MainMentionEditor, profileContent.UserId, profileContent.Nickname);
            else if (content is TextContent textContent) MentionHelper.AppendText(MainMentionEditor, textContent.Text);
        }
    }

    public void FocusEditor()
    {
        MainMentionEditor.Focus();
        MainMentionEditor.CursorPosition = MainMentionEditor.Text?.Length ?? 0;
    }

    public void InsertMention(MentionViewModel viewModel)
    {
        if (viewModel == null) return;

        MentionHelper.InsertMention(MainMentionEditor, viewModel.UserId, viewModel.Nickname);
    }

    public void InsertMention(string userId, string nickname) => MentionHelper.InsertMention(MainMentionEditor, userId, nickname);

    private void OnUnloaded(object sender, EventArgs e)
    {
        ViewModel.ImageInputRequested -= OnImageInputRequested;
        ImageInputRequested = null;
    }
}