using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Message;
using History.Commons.Api.User;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using History.Commons;

namespace History.MobileClient.Pages;

public partial class MessagePage : ContentPage
{
    private readonly BaseMessageViewModel _viewModel;
    public MessagePage(BaseMessageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Dispatcher.Dispatch(MarkAsReadIfNeeded);
        ReplyButton.IsVisible = viewModel.IsReplyButtonVisible;
        DeleteButton.IsVisible = viewModel.IsDeleteButtonVisible;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _ = MarkMessageNotificationsAsReadAsync();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    private async void MarkAsReadIfNeeded()
    {
        if (_viewModel is HistoryMessageViewModel historyViewModel && historyViewModel.Receiver?.UserId == CommonShared.UserId && historyViewModel.ReadAt == null)
        {
            var result = await App.ExecuteRequestAsync(new MarkMessageAsRead(historyViewModel.Id));
            if (result.IsSuccess && Shared.HistoryUnreadMailCount > 0) Shared.HistoryUnreadMailCount--;
        }
        else if (_viewModel is KakaoMessageViewModel kakaoViewModel) kakaoViewModel.MarkAsReadLocally();
    }

    private async Task MarkMessageNotificationsAsReadAsync()
    {
        if (_viewModel is not HistoryMessageViewModel historyViewModel) return;

        var messageId = historyViewModel.Id;
        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByMessageId(messageId));
        if (success) WeakReferenceMessenger.Default.Send(new NotificationMessageReadMessage(messageId));
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();

    private async void OnDeleteButtonTapped(object sender, TappedEventArgs e) => await _viewModel.DeleteAsync(true);

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private async void OnReplyButtonClicked(object sender, EventArgs e)
    {
        if (_viewModel is KakaoMessageViewModel kakaoViewModel)
        {
            var kakaoSenderId = kakaoViewModel.SenderId;
            if (kakaoSenderId == null) return;

            var kakaoPage = new WriteMessagePage(kakaoSenderId, _viewModel.SenderName, true);
            await App.PushModalAsync(kakaoPage);
            return;
        }

        var senderId = (_viewModel as HistoryMessageViewModel)?.Sender?.UserId;
        if (senderId == null) return;

        var page = new WriteMessagePage(senderId, _viewModel.SenderName);
        await App.PushModalAsync(page);
    }
}
