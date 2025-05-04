using FFImageLoading;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SpeakLink.Mention;
using SpeakLink.RichText;

namespace History.MobileClient.ContentViews.EditPost;

public partial class TextContentView : ContentView
{
    // MentionId of Android only supports integer value
    public static Dictionary<int, string> MentionIdMap = [];

    public string Text => MainMentionEditor.Text;

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

        if (!MentionIdMap.Any(x => x.Value == viewModel.UserId)) MentionIdMap[MentionIdMap.Count] = viewModel.UserId;

        MentionHelper.InsertMention(MainMentionEditor, MentionIdMap.FirstOrDefault(x => x.Value == viewModel.UserId).Key.ToString(), viewModel.Nickname + ' ');
    }

    public List<BaseContent> GetContents()
    {
        var result = new List<BaseContent>();
        foreach (var span in MainMentionEditor.FormattedText.Spans)
        {
            if (span is MentionSpan mentionSpan) result.Add(new ProfileContent() { UserId = MentionIdMap[int.Parse(mentionSpan.MentionId)] });
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