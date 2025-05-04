using AndroidX.Lifecycle;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.User;
using History.MobileClient.ContentViews;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using System.Threading.Tasks;

namespace History.MobileClient.Resources.Styles;

public partial class Post : ResourceDictionary
{
	public Post()
	{
		InitializeComponent();
	}

    private async void OnProfileImageTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as PostViewModel;

        var userId = viewModel.Post.User?.UserId;
        if (userId == null)
        {
            await App.Page.DisplayAlert("오류", "사용자 정보를 가져올 수 없습니다", Constants.PromptOk);
            return;
        }

        var profilePage = new UserPage(viewModel.Post.User.UserId);
        await App.PushModalAsync(profilePage);
    }

    private async void OnMoreImageTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as PostViewModel;

        await viewModel.DisplayActionSheet(false);
    }

    private void OnProfileGridLoaded(object sender, EventArgs e)
    {
        var grid = sender as Grid;
        var viewModel = grid.BindingContext as PostViewModel;
        if (viewModel == null) return;

        var presenter = grid.Children.OfType<DataTemplatePresenter>().FirstOrDefault();
        presenter.ViewModel = viewModel.ProfileMedia;
    }

    private void OnCommentGridLoaded(object sender, EventArgs e)
    {
        var grid = sender as Grid;
        var viewModel = grid.BindingContext as PostViewModel;
        if (viewModel == null) return;

        var presenter = grid.Children.OfType<DataTemplatePresenter>().FirstOrDefault();
        presenter.ViewModel = viewModel.FirstComment;
        viewModel.FirstCommentPresenter = new(presenter);
    }
}