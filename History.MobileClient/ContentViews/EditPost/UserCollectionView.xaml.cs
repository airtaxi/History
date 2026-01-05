using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SpeakLink.Mention;

namespace History.MobileClient.ContentViews.EditPost;

public partial class UserCollectionView : ContentView
{
    private TextContentView _textContentView;

    public UserCollectionView() => InitializeComponent();

    public void SetTextContentView(TextContentView textContentView)
    {
        _textContentView = textContentView;
        CollectionView.BindingContext = _textContentView.MentionsViewModel;
    }

    private void OnUserGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MentionUserViewModel;
        if (viewModel == null) return;

        _textContentView.InsertUser(viewModel);
    }
}