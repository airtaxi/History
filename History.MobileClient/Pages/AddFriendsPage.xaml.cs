using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class AddFriendsPage : ContentPage
{
	public AddFriendsPage()
	{
		InitializeComponent();
	}

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        await MainSearchBar.HideSoftInputAsync(CancellationToken.None);

        var searchBar = sender as SearchBar;
        var query = searchBar.Text;
        var results = new List<UserResponseDto>();

        // Add handle results
        var handleResult = await App.ExecuteRequestAsync(new GetUserByHandle(query), [ErrorType.NotFound]);
        if (handleResult.IsSuccess) results.Add(handleResult);

        // Add nickname results
        var nicknameResults = await App.ExecuteRequestAsync(new FindUsersByNickname(query));
        if (nicknameResults.IsSuccess) results.AddRange(nicknameResults.Value);

        // Remove myself from results
        results.RemoveAll(x => x.UserId == Shared.UserId);

        var viewModels = results.Select(x => new FriendshipViewModel(x));

        MainCollectionView.ItemsSource = viewModels;
        EmptyLabel.IsVisible = !viewModels.Any();
    }

    private void OnFriendCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection == null) return;

        var collectionView = sender as CollectionView;
        collectionView.SelectedItem = null;

        var viewModel = e.CurrentSelection as FriendshipViewModel;
        Application.Current.Windows[0].Page.Navigation.PushModalAsync(new UserPage(viewModel.User.UserId));
    }
}