using History.MobileClient.ContentViews;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Resources.Styles;

public partial class Comment : ResourceDictionary
{
	public Comment()
	{
		InitializeComponent();
	}

    private void OnProfileGridLoaded(object sender, EventArgs e)
    {
        var grid = sender as Grid;
        var viewModel = grid.BindingContext as CommentViewModel;
        if (viewModel == null) return;

        var presenter = grid.Children.OfType<DataTemplatePresenter>().FirstOrDefault();
        presenter.ViewModel = viewModel.ProfileMedia;
    }

    private void OnTapped(object sender, TappedEventArgs e)
    {

    }
}