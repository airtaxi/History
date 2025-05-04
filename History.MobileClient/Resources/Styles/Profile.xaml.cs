using Android.Print;
using History.MobileClient.ViewModels;
using System.Threading.Tasks;

namespace History.MobileClient.Resources.Styles;

public partial class Profile : ResourceDictionary
{
	public Profile()
	{
		InitializeComponent();
	}

    private void OnEditDescriptionImageTapped(object sender, TappedEventArgs e)
    {
		var image = sender as Image;
        var viewModel = image?.BindingContext as ProfileViewModel;
        viewModel.OnEditDescriptionImageTapped(sender, e);
    }

    private void OnEditNicknameImageTapped(object sender, TappedEventArgs e)
    {
		var image = sender as Image;
        var viewModel = image?.BindingContext as ProfileViewModel;
        viewModel.OnEditNicknameImageTapped(sender, e);
    }

    private async void OnProfileSettingsButtonClicked(object sender, EventArgs e)
    {
        var action = await App.Page.DisplayActionSheet("프로필 설정", "취소", null, "프로필 사진 변경", "배경 사진 변경", "프로필 공개 설정");

        if(action == null || action == "취소") return;

        if (action == "프로필 사진 변경")
        {
            var border = sender as Border;
            var viewModel = border?.BindingContext as ProfileViewModel;
            await viewModel.HandleChangeProfileMediaAsync();
        }
        else if (action == "배경 사진 변경")
        {
            var button = sender as Button;
            var viewModel = button?.BindingContext as ProfileViewModel;
            await viewModel.HandleChangeBackgroundMediaAsync();
        }
        else if (action == "프로필 공개 설정")
        {
            var button = sender as Button;
            var viewModel = button?.BindingContext as ProfileViewModel;
            await viewModel.HandleChangeProfileVisibilityAsync();
        }
    }

    private async void OnFriendshipButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var viewModel = button?.BindingContext as ProfileViewModel;
        await viewModel.HandleFriendshipActionAsync();
    }
}