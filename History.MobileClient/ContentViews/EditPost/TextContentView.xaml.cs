using History.Commons.DataTypes.Contents;
using History.MobileClient.ViewModels;
using SpeakLink.Mention;
using SpeakLink.RichText;

namespace History.MobileClient.ContentViews.EditPost;

public partial class TextContentView : ContentView
{
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

        MainMentionEditor.InsertMention(viewModel.UserId, viewModel.Nickname);
    }

    public List<BaseContent> GetContents()
    {
        var result = new List<BaseContent>();
        foreach (var span in MainMentionEditor.FormattedText.Spans)
        {
            if (span is MentionSpan mentionSpan) result.Add(new ProfileContent() { UserId = mentionSpan.MentionId });
            else result.Add(new TextContent() { Text = span.Text });
        }
        return result;
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        ViewModel.ImageInputRequested -= OnImageInputRequested;
        ImageInputRequested = null;
    }
}