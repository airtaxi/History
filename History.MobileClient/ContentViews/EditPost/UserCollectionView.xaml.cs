using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SpeakLink.Mention;

namespace History.MobileClient.ContentViews.EditPost;

public partial class UserCollectionView : ContentView
{
    private MentionEditor _mentionEditor;

    public UserCollectionView() => InitializeComponent();

    public void SetMentionEditor(MentionEditor mentionEditor)
    {
        _mentionEditor = mentionEditor;
        UserCollectionView.BindingContext = mentionEditor.BindingContext as MentionsViewModel;
    }

    private void OnUserGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MentionViewModel;
        if (viewModel == null) return;

        MentionHelper.InsertMention(_mentionEditor, viewModel.UserId, viewModel.Nickname);
    }
}