using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class DiscoveryOptionSelectUsersPage : ContentPage
{
    private bool _isInForeground;
    private readonly ObservableCollection<SelectUserViewModel> _selectedViewModels = [];
    private readonly TaskCompletionSource<List<string>> _taskCompletionSource = new();
    private readonly List<string> _originallySelectedUserIds;

    private List<SelectUserViewModel> _viewModels;
    public DiscoveryOptionSelectUsersPage(List<string> originallySelectedUserIds = null)
	{
        InitializeComponent();

        _originallySelectedUserIds = originallySelectedUserIds ?? [];
        SelectedUserCollectionView.ItemsSource = _selectedViewModels;
    }

    public Task<List<string>> GetResultAsync() => _taskCompletionSource.Task;

    private async Task RefreshAsync()
    {
        var friendsResult = await App.ExecuteRequestAsync(new GetFriends(Shared.UserId));
        if (friendsResult.IsSuccess)
        {
            Shared.Friends = friendsResult.Value;

            _viewModels = [.. friendsResult.Value.Select(x => new SelectUserViewModel(x, _originallySelectedUserIds.Contains(x.UserId)))];

            MainSearchBar.Text = string.Empty;
            EmptyLabel.IsVisible = !_viewModels.Any();
            MainCollectionView.ItemsSource = _viewModels.OrderBy(x => x.Nickname);

            foreach (var selectedViewModel in _viewModels.Where(x => x.IsSelected)) WeakReferenceMessenger.Default.Send(new SelectUserSelectionMessage(selectedViewModel));
        }
    }

    public static Dictionary<string, List<string>> GetPresets() => Configuration.GetValue<Dictionary<string, List<string>>>("DiscoveryOptionSelectUsersPresets") ?? [];

    public static Result SavePreset(string key, List<string> presets)
    {
        var presetsDict = GetPresets();
        if (presets.Count > 10) return (ErrorType.BadRequest, "프리셋은 최대 10개까지 저장할 수 있습니다.");
        else if (presetsDict.ContainsKey(key)) return (ErrorType.Conflict, $"{key}이라는 이름의 프리셋은 이미 존재합니다.");

        presetsDict[key] = presets;
        Configuration.SetValue("DiscoveryOptionSelectUsersPresets", presetsDict);

        return Result.Success();
    }

    public static Result CheckForSamePreset(List<string> userIds)
    {
        var presetsDict = GetPresets();
        foreach (var preset in presetsDict)
        {
            if (!preset.Value.Except(userIds).Any() && !userIds.Except(preset.Value).Any())
            {
                return (ErrorType.Conflict, $"{preset.Key}이라는 이름의 프리셋과 동일한 설정입니다.");
            }
        }
        return Result.Success();
    }

    public static Result DeletePreset(string key)
    {
        var presetsDict = GetPresets();
        if (!presetsDict.ContainsKey(key)) return (ErrorType.NotFound, $"{key}이라는 이름의 프리셋은 존재하지 않습니다.");

        presetsDict.Remove(key);
        Configuration.SetValue("DiscoveryOptionSelectUsersPresets", presetsDict);

        return Result.Success();
    }

    private void OnMainSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModels == null) return;

        var searchBar = sender as SearchBar;
        var query = searchBar.Text?.ToLowerInvariant() ?? string.Empty;
        query = query.Trim();

        if (string.IsNullOrEmpty(query))
        {
            MainCollectionView.ItemsSource = _viewModels;
            EmptyLabel.IsVisible = !_viewModels.Any();
        }
        else
        {
            var viewModels = _viewModels.Where(x => x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || x.User.Handle.Equals(query, StringComparison.OrdinalIgnoreCase));
            MainCollectionView.ItemsSource = viewModels;
            EmptyLabel.IsVisible = !viewModels.Any();
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private void OnSelectUserSelectionMessageReceived(object recipient, SelectUserSelectionMessage message)
    {
        var viewModel = message.Value;
        if (viewModel.IsSelected) _selectedViewModels.Add(viewModel);
        else _selectedViewModels.Remove(viewModel);

        SelectedUserPlaceholderVerticalStackLayout.IsVisible = _selectedViewModels.Count == 0;
    }

    private async void OnSelectButtonClicked(object sender, EventArgs e)
    {
        var userIds = _selectedViewModels.Select(x => x.User.UserId).ToList();

        if (userIds.Count == 0)
        {
            await DisplayAlert("오류", "최소 한 명 이상의 친구를 선택해주세요", Constants.PromptOk);
            return;
        }

        _taskCompletionSource.TrySetResult(userIds);
        await App.PopModalAsync();
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();

    private bool _isInitialized = false;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        if (!_isInitialized)
        {
            _isInitialized = true;

            await RefreshAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;

        if (!_taskCompletionSource.Task.IsCompleted)
        {
            _taskCompletionSource.TrySetResult(null);
        }
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        if (!_isInForeground) return;

        Dispatcher.Dispatch(() =>
        {
            var isLoading = message.Value;
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private void OnHandlerChanging(object sender, HandlerChangingEventArgs e)
    {
        if (e.NewHandler == null) WeakReferenceMessenger.Default.UnregisterAll(this);
        else
        {
            WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
            WeakReferenceMessenger.Default.Register<SelectUserSelectionMessage>(this, OnSelectUserSelectionMessageReceived);
        }
    }

    private async void OnPresetButtonClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet("프리셋", Constants.PromptCancel, null, "이 설정으로 프리셋 저장", "프리셋 불러오기", "프리셋 삭제");
        if (action == null || action == Constants.PromptCancel) return;

        if (action == "이 설정으로 프리셋 저장")
        {
            if (_selectedViewModels.Count == 0)
            {
                await DisplayAlert("오류", "프리셋을 저장하기 위해서는 최소 한 명 이상의 친구를 선택해야 합니다.", Constants.PromptOk);
                return;
            }

            var userIds = _selectedViewModels.Select(x => x.User.UserId).ToList();
            var key = await DisplayPromptAsync("프리셋 이름", "프리셋의 이름을 입력해주세요.", maxLength: 20, accept: "저장", cancel: Constants.PromptCancel, placeholder: "프리셋 이름");
            if (key == null || key == Constants.PromptCancel) return;

            if (key.Length < 1 || key.Length > 20)
            {
                await DisplayAlert("오류", "프리셋 이름은 1자 이상 20자 이하로 입력해야 합니다.", Constants.PromptOk);
                return;
            }

            var result = CheckForSamePreset(userIds);
            if (result.IsFailure)
            {
                await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
                return;
            }

            result = SavePreset(key, userIds);
            if (result.IsFailure)
            {
                await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
                return;
            }

            await Toast.Make($"{key} 프리셋이 저장되었습니다.").Show();
        }
        else if (action == "프리셋 불러오기")
        {
            var presets = GetPresets();
            if (presets.Count == 0)
            {
                await DisplayAlert("오류", "저장된 프리셋이 없습니다.", Constants.PromptOk);
                return;
            }
            var key = await DisplayActionSheet("프리셋 선택", Constants.PromptCancel, null, [.. presets.Keys]);
            if (key == null || key == Constants.PromptCancel) return;
            var userIds = presets[key];
            foreach (var userId in userIds)
            {
                var viewModel = _viewModels.FirstOrDefault(x => x.User.UserId == userId);
                if (viewModel != null) viewModel.IsSelected = true;
            }

            await Toast.Make($"{key} 프리셋을 불러왔습니다.").Show();
        }
        else if (action == "프리셋 삭제")
        {
            var presets = GetPresets();
            if (presets.Count == 0)
            {
                await DisplayAlert("오류", "저장된 프리셋이 없습니다.", Constants.PromptOk);
                return;
            }

            var key = await DisplayActionSheet("프리셋 삭제", Constants.PromptCancel, null, [.. presets.Keys]);
            if (key == null || key == Constants.PromptCancel) return;

            var result = DeletePreset(key);
            if (result.IsFailure)
            {
                await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
                return;
            }

            await Toast.Make("프리셋이 삭제되었습니다.").Show();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopModalAsync();
        return true;
    }
}