using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.InviteCode;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class InviteCodeRequestsPage : ContentPage
{
    private readonly ObservableCollection<InviteCodeRequestViewModel> _viewModels = [];
    private bool _areThereNoMoreRequestsToLoad;
    private bool _isInForeground;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public InviteCodeRequestsPage()
    {
        InitializeComponent();
        RequestsCollectionView.ItemsSource = _viewModels;
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChanged);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;
        _ = RefreshAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChanged(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        try
        {
            await _fetchSemaphore.WaitAsync();
            _areThereNoMoreRequestsToLoad = false;
            _viewModels.Clear();
            var result = await App.ExecuteRequestAsync(new GetInviteCodeRequests());
            if (result.IsSuccess)
                foreach (var request in result.Value) _viewModels.Add(new InviteCodeRequestViewModel(request));
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnRemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (_fetchSemaphore.CurrentCount == 0 || _areThereNoMoreRequestsToLoad) return;
        try
        {
            await _fetchSemaphore.WaitAsync();
            var lastViewModel = _viewModels.LastOrDefault();
            if (lastViewModel == null) return;
            var result = await App.ExecuteRequestAsync(new GetInviteCodeRequests(lastViewModel.Id));
            if (result.IsSuccess)
            {
                _areThereNoMoreRequestsToLoad = result.Value.Count == 0;
                foreach (var request in result.Value) _viewModels.Add(new InviteCodeRequestViewModel(request));
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    private async void OnAcceptButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.BindingContext is not InviteCodeRequestViewModel viewModel) return;

        var message = await App.Page.DisplayPromptAsync(
            "수락",
            "추가 메시지를 입력하세요 (선택사항)",
            maxLength: 500);

        var confirm = await App.Page.DisplayAlertAsync(
            "확인",
            $"요청을 수락하시겠습니까? {viewModel.RequestedCount}개의 초대 코드가 자동 발급됩니다.",
            Constants.PromptOk,
            Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new AcceptInviteCodeRequest(viewModel.Id, message), [ErrorType.Conflict]);
        if (result.IsSuccess)
        {
            await App.Page.DisplayAlertAsync("완료", "초대 코드 요청을 수락했습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
        else
        {
            await App.Page.DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
        }
    }

    private async void OnRejectButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.BindingContext is not InviteCodeRequestViewModel viewModel) return;

        var message = await App.Page.DisplayPromptAsync(
            "거부",
            "추가 메시지를 입력하세요 (선택사항)",
            maxLength: 500);

        var confirm = await App.Page.DisplayAlertAsync(
            "확인",
            "요청을 거부하시겠습니까?",
            Constants.PromptOk,
            Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new RejectInviteCodeRequest(viewModel.Id, message), [ErrorType.Conflict]);
        if (result.IsSuccess)
        {
            await App.Page.DisplayAlertAsync("완료", "초대 코드 요청을 거부했습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
        else
        {
            await App.Page.DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
        }
    }

    private async void OnBackTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnRefreshViewRefreshing(object sender, EventArgs e)
    {
        await RefreshAsync();
        RefreshView.IsRefreshing = false;
    }
}