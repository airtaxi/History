using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.Messages;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class BulkPostManagePage : ContentPage
{
    private bool _areThereNoMorePostsToLoad;
    private bool _isSelectAll;
    private SelectablePostViewModel _lastViewModel;
    private readonly ObservableCollection<SelectablePostViewModel> _viewModels = [];
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public BulkPostManagePage()
    {
        InitializeComponent();
        MainCollectionView.ItemsSource = _viewModels;

        WeakReferenceMessenger.Default.Register<PostSelectionChangedMessage>(this, OnPostSelectionChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<PostResponseDto>>(this, OnPostDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnPostSelectionChangedMessageReceived(object recipient, PostSelectionChangedMessage message) => UpdateActionBar();

    private void OnPostDeletedMessageReceived(object recipient, ValueDeletedMessage<PostResponseDto> message)
    {
        var viewModels = _viewModels.Where(x => x.Post.Id == message.Value.Id).ToList();
        foreach (var viewModel in viewModels) _viewModels.Remove(viewModel);
        _lastViewModel = _viewModels.LastOrDefault();
        UpdateEmptyState();
        UpdateActionBar();
    }

    private async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        try
        {
            await _fetchSemaphore.WaitAsync();
            _areThereNoMorePostsToLoad = false;
            _isSelectAll = false;
            SelectAllLabel.Text = "전체 선택";
            _viewModels.Clear();
            _lastViewModel = null;

            var result = await App.ExecuteRequestAsync(new GetUserPosts(Shared.UserId, null, 50));
            if (result.IsSuccess)
            {
                var viewModels = result.Value.Select(x => new SelectablePostViewModel(x)).ToList();
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = viewModels.Count == 0;
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }

            UpdateEmptyState();
            UpdateActionBar();
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMorePostsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = _viewModels.LastOrDefault();
            if (lastViewModel == null) return;

            var lastPostId = lastViewModel.RepostId ?? lastViewModel.Post.Id;
            var result = await App.ExecuteRequestAsync(new GetUserPosts(Shared.UserId, lastPostId, 50));
            if (result.IsSuccess)
            {
                var viewModels = result.Value.Select(x => new SelectablePostViewModel(x)).ToList();
                _lastViewModel = viewModels.LastOrDefault();
                _areThereNoMorePostsToLoad = viewModels.Count == 0;
                foreach (var viewModel in viewModels) _viewModels.Add(viewModel);
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private void UpdateEmptyState() => EmptyStateLayout.IsVisible = _viewModels.Count == 0;

    private void UpdateActionBar()
    {
        var selectedCount = _viewModels.Count(x => x.IsSelected);
        ActionBarBorder.IsVisible = _viewModels.Count > 0;
        SelectedCountLabel.Text = $"{selectedCount}개 선택됨";
    }

    private async void OnChangeDiscoveryOptionButtonClicked(object sender, EventArgs e)
    {
        var selectedViewModels = _viewModels.Where(x => x.IsSelected).ToList();
        if (selectedViewModels.Count == 0)
        {
            await DisplayAlertAsync("안내", "게시글을 먼저 선택해주세요.", Constants.PromptOk);
            return;
        }

        var options = Enum.GetValues<DiscoveryOption>()
            .Where(x => x != DiscoveryOption.SelectedUsers && x != DiscoveryOption.UnselectedUsers)
            .Select(x => x.ToDisplayString())
            .ToArray();
        var rawTo = await DisplayActionSheetAsync("변경할 공개 범위 선택", Constants.PromptCancel, null, options);
        if (rawTo == null || rawTo == Constants.PromptCancel) return;

        var to = DiscoveryOptionExtensions.FromDisplayString(rawTo);

        var confirm = await DisplayAlertAsync("확인", $"선택한 {selectedViewModels.Count}개의 게시글 공개 범위를 '{to.ToDisplayString()}'로 변경하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new BulkChangeDiscoveryOptionByPostIds([.. selectedViewModels.Select(x => x.Post.Id)], to));
        if (result.IsSuccess)
        {
            foreach (var viewModel in selectedViewModels) viewModel.ApplyDiscoveryOption(to);
            UpdateActionBar();
            await DisplayAlertAsync("완료", "일괄 변경이 완료되었습니다.", Constants.PromptOk);
            InvalidatePostPages();
        }
    }

    private async void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        var selectedViewModels = _viewModels.Where(x => x.IsSelected).ToList();
        if (selectedViewModels.Count == 0)
        {
            await DisplayAlertAsync("안내", "게시글을 먼저 선택해주세요.", Constants.PromptOk);
            return;
        }

        var confirm = await DisplayAlertAsync("삭제", $"선택한 {selectedViewModels.Count}개의 게시글을 삭제하시겠습니까? 되돌릴 수 없습니다.", "삭제", Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new BulkDeletePostsByPostIds([.. selectedViewModels.Select(x => x.Post.Id)]));
        if (result.IsSuccess)
        {
            foreach (var viewModel in selectedViewModels) _viewModels.Remove(viewModel);
            _lastViewModel = _viewModels.LastOrDefault();
            UpdateEmptyState();
            UpdateActionBar();
            await DisplayAlertAsync("완료", "일괄 삭제가 완료되었습니다.", Constants.PromptOk);
            InvalidatePostPages();
        }
    }

    private void OnSelectAllLabelTapped(object sender, TappedEventArgs e)
    {
        var shouldSelectAll = !_isSelectAll;
        _isSelectAll = shouldSelectAll;
        SelectAllLabel.Text = shouldSelectAll ? "전체 해제" : "전체 선택";
        foreach (var viewModel in _viewModels) viewModel.SetSelected(shouldSelectAll);
        UpdateActionBar();
    }

    private static void InvalidatePostPages()
    {
        TimelinePage.ShouldRefresh = true;
        UserPage.ShouldRefresh = true;
        PublicPostsPage.ShouldRefresh = true;
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

        await RefreshAsync();
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnMainCollectionViewChildAdded(object sender, ElementEventArgs e)
    {
        var view = e.Element as View;
        if (view.BindingContext is not SelectablePostViewModel viewModel) return;

        if (_lastViewModel != null && viewModel.Post.Id == _lastViewModel.Post.Id)
        {
            _lastViewModel = null;
            await LoadMoreAsync();
        }
    }

    private async void OnMainCollectionViewRemainingItemsThresholdReached(object sender, EventArgs e) => await LoadMoreAsync();
}
