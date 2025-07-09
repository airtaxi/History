using System.IO;
using History.Commons.Api.Message;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls;

namespace History.MobileClient.Pages;

public partial class MessagePage : ContentPage
{
    private readonly MessageViewModel _viewModel;
    public MessagePage(MessageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Dispatcher.Dispatch(MarkAsReadIfNeeded);
        ReplyButton.IsVisible = viewModel.Receiver?.UserId != Shared.UserId;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    private async void MarkAsReadIfNeeded()
    {
        if (_viewModel.Receiver?.UserId == Shared.UserId && _viewModel.ReadAt == null)
        {
            await App.ExecuteRequestAsync(new MarkMessageAsRead(_viewModel.Id));
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private async void OnReplyButtonClicked(object sender, EventArgs e)
    {
        var senderId = _viewModel.Sender?.UserId;
        if (senderId == null) return;

        var page = new WriteMessagePage(senderId, _viewModel.SenderName);
        await App.PushModalAsync(page);
    }
}
