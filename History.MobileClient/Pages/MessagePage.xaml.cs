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
        SenderLabel.Text = $"보낸 사람: {_viewModel.SenderName}";
        ReceiverLabel.Text = $"받는 사람: {_viewModel.ReceiverName}";
        TimestampLabel.Text = _viewModel.TimestampText;
        MessageLabel.Text = _viewModel.MainText;
        if (_viewModel.HasImage)
        {
            BackgroundImage.Source = _viewModel.ImageUrl;
            BackgroundImage.IsVisible = true;
            BackgroundBox.IsVisible = false;
        }
        else
        {
            BackgroundImage.IsVisible = false;
            BackgroundBox.IsVisible = true;
        }
        MarkAsReadIfNeeded();
    }

    private async void MarkAsReadIfNeeded()
    {
        if (_viewModel.Receiver?.UserId == Shared.UserId && _viewModel.Message.ReadAt == null)
        {
            await App.ExecuteRequestAsync(new MarkMessageAsRead(_viewModel.Id));
        }
    }
}
