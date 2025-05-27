using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class AddFriendsPage : ContentPage
{
    private bool _isInForeground;

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

        // Delete duplicated records
        results = [.. results.DistinctBy(x => x.UserId)];

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
        await App.PushAsync(new UserPage(viewModel.User.UserId));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }
}