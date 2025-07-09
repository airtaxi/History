using History.Commons.Api.Message;
using History.MobileClient.ViewModels;
using System.IO;
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
    }

    private async void MarkAsReadIfNeeded()
    {
        if (_viewModel.Receiver?.UserId == Shared.UserId && _viewModel.ReadAt == null)
        {
            await App.ExecuteRequestAsync(new MarkMessageAsRead(_viewModel.Id));
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}
