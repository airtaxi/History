using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using WinUIEx;

namespace History.WindowsClient.Views;

// Full-screen media viewer window hosting the MediaWindowViewModel. Subclasses BaseWindow
// so the theme, icon, centering, and loading-message routing apply automatically; the view
// model's dialog/picker/loading events are fulfilled directly on this window's content.
public sealed partial class MediaWindow : BaseWindow
{
    private readonly MediaWindowViewModel _viewModel;

    public MediaWindowViewModel ViewModel => _viewModel;

    public MediaWindow(MediaWindowViewModel viewModel) : base()
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();

        SubscribeViewModelEvents();
    }

    // no-op for this window
    protected override void Navigate(Type pageType, object parameter) { }

    protected override void ShowLoading(string message = null)
    {
        if (DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Visible, message);
        else DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Visible, message));
    }

    protected override void HideLoading()
    {
        if (DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Collapsed, null);
        else DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Collapsed, null));
    }

    private void SetLoadingState(Visibility visibility, string message)
    {
        LoadingGrid.Visibility = visibility;
        AppTitleBar.IsEnabled = visibility == Visibility.Collapsed;
        MediaFlipView.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    private void SubscribeViewModelEvents()
    {
        _viewModel.MessageDialogRequested += OnMessageDialogRequested;
        _viewModel.SelectionDialogRequested += OnSelectionDialogRequested;
        _viewModel.SaveFileRequested += OnSaveFileRequested;
        _viewModel.FolderPickRequested += OnFolderPickRequested;
        _viewModel.LoadingStateRequested += OnLoadingStateRequested;
    }

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = Content.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnSelectionDialogRequested(object sender, SelectionDialogRequestedEventArgs args)
    {
        var result = Content.ShowSelectionDialogAsync(args.Title, args.Options);
        args.ResultTask = result;
    }

    private void OnSaveFileRequested(object sender, PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult> args)
    {
        var result = Content.SaveFileAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnFolderPickRequested(object sender, PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult> args)
    {
        var result = Content.PickFolderAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Forwards the view model's loading requests to this window's overlay through the
    // weak-reference messenger; BaseWindow routes them by XamlRoot.
    private void OnLoadingStateRequested(object sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(Content.XamlRoot, args);

    private void OnEscapeKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        UnregisterMessengerRecipients();

        foreach (var media in _viewModel.Medias)
        {
            media.ResetForReuse();
        }
    }
}