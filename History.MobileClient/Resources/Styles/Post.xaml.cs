using History.MobileClient.ContentViews;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Resources.Styles;

public partial class Post : ResourceDictionary
{
	public Post()
	{
		InitializeComponent();
	}

    //private void OnProfileGridLoaded(object sender, EventArgs e)
    //{
    //    var grid = sender as Grid;
    //    var viewModel = grid.BindingContext as PostViewModel;
    //    if (viewModel == null) return;

    //    var presenter = grid.Children.OfType<DataTemplatePresenter>().FirstOrDefault();
    //    presenter.ViewModel = viewModel.ProfileMedia;
    //}

    //private void OnCommentGridLoaded(object sender, EventArgs e)
    //{
    //    var grid = sender as Grid;
    //    var viewModel = grid.BindingContext as PostViewModel;
    //    if (viewModel == null) return;

    //    var presenter = grid.Children.OfType<DataTemplatePresenter>().FirstOrDefault();
    //    presenter.ViewModel = viewModel.FirstComment;
    //    viewModel.FirstCommentPresenter = new(presenter);
    //}
}