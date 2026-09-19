using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels.Message;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.MainPage;

// Shared message side bar surface: the message list, empty state and the
// write-message flow implemented by each account mode.
public abstract partial class BaseMainPageMessagesSideBarViewModel : BaseMainPageSideBarViewModel, IRecipient<MessageSentMessage>
{
    private bool _isFirstLoad;

    public BaseMainPageMessagesSideBarViewModel(MainPageViewModel baseViewModel)
    {
        BaseViewModel = baseViewModel;
        WeakReferenceMessenger.Default.Register((IRecipient<MessageSentMessage>)this);
    }

    public MainPageViewModel BaseViewModel { get; }

    // Whether this side bar lists the History or the Kakao Story mails; a sent-mail
    // notification is only acted on by the side bar of the matching platform.
    protected abstract bool IsKakaoStoryMode { get; }

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

    // A mail was sent from one of the compose dialogs; reload so the list reflects it.
    public void Receive(MessageSentMessage message)
    {
        if (message.IsKakaoStoryMode != IsKakaoStoryMode) return;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task WriteMessageAsync()
    {
        var dialogViewModel = CreateWriteMessageDialogViewModel();
        var dialog = new WriteMessageDialog(dialogViewModel);
        await BaseViewModel.ShowContentDialogAsync(dialog);
    }

    protected virtual HistoryWriteMessageDialogViewModel CreateWriteMessageDialogViewModel() => new(BaseViewModel);
}
