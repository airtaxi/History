using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Message;
using History.WindowsClient.Dialogs;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageMessagesSideBarViewModel(MainPageViewModel baseViewModel) : BaseMainPageSideBarViewModel
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _isFirstLoad;

    public MainPageViewModel BaseViewModel { get; } = baseViewModel;

    [ObservableProperty]
    public partial ObservableCollection<BaseMessageViewModel> Items { get; set; } = [];

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial string EmptyText { get; set; } = "쪽지가 없습니다.";

    public async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isFirstLoad) return;
        _isFirstLoad = true;

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        await _fetchSemaphore.WaitAsync();

        try
        {
            var receivedResult = await BaseViewModel.ExecuteRequestAsync(new GetReceivedMessages());
            var sentResult = await BaseViewModel.ExecuteRequestAsync(new GetSentMessages());

            if (receivedResult.IsSuccess && sentResult.IsSuccess)
            {
                var allMessages = receivedResult.Value
                    .Concat(sentResult.Value)
                    .OrderByDescending(message => message.CreatedAt);

                Items = new(allMessages.Select(message => new HistoryMessageViewModel(message, BaseViewModel)));
                IsEmpty = Items.Count == 0;
            }
            else if (receivedResult.IsSuccess)
            {
                var receivedMessages = receivedResult.Value.OrderByDescending(message => message.CreatedAt);
                Items = new(receivedMessages.Select(message => new HistoryMessageViewModel(message, BaseViewModel)));
                IsEmpty = Items.Count == 0;
            }
            else if (sentResult.IsSuccess)
            {
                var sentMessages = sentResult.Value.OrderByDescending(message => message.CreatedAt);
                Items = new(sentMessages.Select(message => new HistoryMessageViewModel(message, BaseViewModel)));
                IsEmpty = Items.Count == 0;
            }
            else
            {
                Items.Clear();
                IsEmpty = true;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    [RelayCommand]
    public async Task WriteMessageAsync()
    {
        var dialogViewModel = new WriteMessageDialogViewModel(BaseViewModel);
        var dialog = new WriteMessageDialog(dialogViewModel);
        await BaseViewModel.ShowContentDialogAsync(dialog);
        if (dialogViewModel.IsSent) await RefreshAsync();
    }
}

