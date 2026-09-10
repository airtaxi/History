using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.WindowsClient.Dialogs;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.MainPage;

// Shared message side bar surface: the message list, empty state and the
// write-message flow implemented by each account mode.
public abstract partial class BaseMainPageMessagesSideBarViewModel(MainPageViewModel baseViewModel) : BaseMainPageSideBarViewModel
{
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

    public abstract Task RefreshAsync();

    [RelayCommand]
    private async Task WriteMessageAsync()
    {
        var dialogViewModel = CreateWriteMessageDialogViewModel();
        var dialog = new WriteMessageDialog(dialogViewModel);
        await BaseViewModel.ShowContentDialogAsync(dialog);
        if (dialogViewModel.IsSent) await RefreshAsync();
    }

    protected virtual HistoryWriteMessageDialogViewModel CreateWriteMessageDialogViewModel() => new(BaseViewModel);
}
