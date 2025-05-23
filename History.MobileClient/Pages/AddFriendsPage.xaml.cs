using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class AddFriendsPage : ContentPage
{
	public AddFriendsPage()
	{
		InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
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

    private async void OnFriendCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection == null) return;

        var collectionView = sender as CollectionView;
        collectionView.SelectedItem = null;

        var viewModel = e.CurrentSelection as FriendshipViewModel;
        await App.PushModalAsync(new UserPage(viewModel.User.UserId));
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        MainActivityIndicator.IsRunning = isLoading;
        IsEnabled = !isLoading;
    }
}