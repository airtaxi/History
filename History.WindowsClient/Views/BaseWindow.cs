using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Services;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using WinUIEx;

namespace History.WindowsClient.Views;

public abstract class BaseWindow : WindowEx,
    IRecipient<LoadingStateRequestedMessage>,
    IRecipient<ShowLoadingMessage>,
    IRecipient<HideLoadingMessage>,
    IRecipient<NavigationRequestedMessage>,
    IRecipient<TryNavigateBackRequestedMessage>,
    IRecipient<WindowCloseBlockRequestedMessage>
{
    // Serializes this window's loading overlay sequences: concurrent loading requests from
    // different pages (for example MainPage and TimelinePage refreshing on startup) would
    // otherwise show and hide the overlay in an interleaved order, toggling the overlay
    // Visibility from inside the layout passes that are still settling during the initial load.
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

    private readonly BaseViewModel _viewModel;

    // Close guard: while a long-running flow owns the window, close attempts are cancelled and
    // the notice with that flow's reason is shown instead.
    private const string DefaultCloseBlockedReason = "작업이 진행 중입니다. 중단한 뒤 닫아주세요.";

    private bool _isCloseBlocked;
    private string _closeBlockedReason;
    private bool _isCloseBlockedNoticeOpen;

    protected readonly ApplicationThemeService _applicationThemeService = App.Services.GetRequiredService<ApplicationThemeService>();

    protected BaseWindow() : this(null) { }

    protected BaseWindow(BaseViewModel viewModel)
    {
        _viewModel = viewModel;

        _applicationThemeService.ApplyThemeToWindow(this);
        _applicationThemeService.ThemeChanged += OnApplicationThemeServiceThemeChanged;
        Closed += OnBaseWindowClosed;

        // Wired once here so the close-block flag alone decides whether a close is cancelled.
        AppWindow.Closing += (_, args) =>
        {
            if (!_isCloseBlocked) return;

            args.Cancel = true;
            ShowCloseBlockedNotice();
        };

        AppWindow.SetIcon("Assets/Icon.ico");

        WeakReferenceMessenger.Default.Register((IRecipient<LoadingStateRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<ShowLoadingMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<HideLoadingMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NavigationRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<TryNavigateBackRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<WindowCloseBlockRequestedMessage>)this);
    }

    // XAML-declared lifecycle hooks: derived roots declare Loaded="OnWindowLoaded" and windows
    // declare Closed="OnWindowClosed". The wiring persists for the window's lifetime, so the
    // view model events stay subscribed while the window content is alive.
    protected virtual void OnWindowLoaded(object sender, RoutedEventArgs e) => SubscribeViewModelEvents();

    protected virtual void OnWindowClosed(object sender, WindowEventArgs args) => UnsubscribeViewModelEvents();

    // Subscribes the shared view model events every window fulfills on its own content: dialogs,
    // pickers, loading overlay, and navigation requests. Derived windows override this and call
    // base to add the events that are specific to their view model.
    protected virtual void SubscribeViewModelEvents()
    {
        if (_viewModel == null) return;

        _viewModel.MessageDialogRequested += OnMessageDialogRequested;
        _viewModel.InputDialogRequested += OnInputDialogRequested;
        _viewModel.ContentDialogRequested += OnContentDialogRequested;
        _viewModel.SelectionDialogRequested += OnSelectionDialogRequested;
        _viewModel.FilePickRequested += OnFilePickRequested;
        _viewModel.FilesPickRequested += OnFilesPickRequested;
        _viewModel.SaveFileRequested += OnSaveFileRequested;
        _viewModel.FolderPickRequested += OnFolderPickRequested;
        _viewModel.LoadingStateRequested += OnLoadingStateRequested;
        _viewModel.ShowLoadingRequested += OnShowLoadingRequested;
        _viewModel.HideLoadingRequested += OnHideLoadingRequested;
        _viewModel.NavigationRequested += OnNavigationRequested;
        _viewModel.TryNavigateBackRequested += OnTryNavigateBackRequested;
    }

    protected virtual void UnsubscribeViewModelEvents()
    {
        if (_viewModel == null) return;

        _viewModel.MessageDialogRequested -= OnMessageDialogRequested;
        _viewModel.InputDialogRequested -= OnInputDialogRequested;
        _viewModel.ContentDialogRequested -= OnContentDialogRequested;
        _viewModel.SelectionDialogRequested -= OnSelectionDialogRequested;
        _viewModel.FilePickRequested -= OnFilePickRequested;
        _viewModel.FilesPickRequested -= OnFilesPickRequested;
        _viewModel.SaveFileRequested -= OnSaveFileRequested;
        _viewModel.FolderPickRequested -= OnFolderPickRequested;
        _viewModel.LoadingStateRequested -= OnLoadingStateRequested;
        _viewModel.ShowLoadingRequested -= OnShowLoadingRequested;
        _viewModel.HideLoadingRequested -= OnHideLoadingRequested;
        _viewModel.NavigationRequested -= OnNavigationRequested;
        _viewModel.TryNavigateBackRequested -= OnTryNavigateBackRequested;
    }

    // Fulfills the view model's dialog requests with this window's content.
    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = Content.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnInputDialogRequested(object sender, InputDialogRequestedEventArgs args)
    {
        var result = Content.ShowInputDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnContentDialogRequested(object sender, ContentDialogRequestedEventArgs args)
    {
        var result = Content.ShowContentDialogAsync(args.Dialog);
        args.ResultTask = result;
    }

    private void OnSelectionDialogRequested(object sender, SelectionDialogRequestedEventArgs args)
    {
        var result = Content.ShowSelectionDialogAsync(args.Title, args.Options);
        args.ResultTask = result;
    }

    // Fulfills the view model's picker requests with this window's content.
    private void OnFilePickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult> args)
    {
        var result = Content.PickFileAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnFilesPickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>> args)
    {
        var result = Content.PickFilesAsync(args.Parameters);
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
    // weak-reference messenger (the window matches its own XamlRoot).
    private void OnLoadingStateRequested(object sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(Content.XamlRoot, args);

    private void OnShowLoadingRequested(object sender, ShowLoadingRequestedEventArgs args) => ShowLoadingMessage.Send(Content.XamlRoot, args);

    private void OnHideLoadingRequested(object sender, HideLoadingRequestedEventArgs args) => HideLoadingMessage.Send(Content.XamlRoot);

    // Forwards the view model's navigation requests to this window through the
    // weak-reference messenger (the window matches its own XamlRoot).
    private void OnNavigationRequested(object sender, NavigationRequestedEventArgs args) => NavigationRequestedMessage.Send(Content.XamlRoot, args.PageType, args.Parameter);

    // Forwards the view model's back navigation requests to this window through the
    // weak-reference messenger (the window matches its own XamlRoot).
    private void OnTryNavigateBackRequested(object sender, TryNavigateBackRequestedEventArgs args) => TryNavigateBackRequestedMessage.Send(Content.XamlRoot, args);

    // Runs loading requests that originated from this window's pages/controls: the
    // XamlRoot reference comparison routes messages from other windows away.
    public void Receive(LoadingStateRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        _ = RunLoadingAsync(message);
    }

    // Runs explicit show/hide requests that originated from this window: the XamlRoot
    // reference comparison routes messages from other windows away.
    public void Receive(ShowLoadingMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        ShowLoading(message.LoadingMessage);
    }

    public void Receive(HideLoadingMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        HideLoading();
    }

    // Runs navigation requests that originated from this window's pages/controls: the
    // XamlRoot reference comparison routes messages from other windows away.
    public void Receive(NavigationRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        Navigate(message.PageType, message.Parameter);
    }

    // Runs back navigation requests that originated from this window's pages/controls: the
    // XamlRoot reference comparison routes messages from other windows away.
    public void Receive(TryNavigateBackRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        message.Complete(TryNavigateBack());
    }

    // Applies close-block requests that originated from this window's pages/controls: the
    // XamlRoot reference comparison routes requests from other windows away.
    public void Receive(WindowCloseBlockRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        SetCloseBlocked(message.IsCloseBlocked, message.Reason);
    }

    // Blocks or unblocks closing the window. While blocked, every close attempt (the title bar
    // close button, Alt+F4, the system menu, or a programmatic Close) is cancelled and the
    // notice with the optional reason is shown.
    public void SetCloseBlocked(bool isCloseBlocked, string reason = null)
    {
        _isCloseBlocked = isCloseBlocked;
        _closeBlockedReason = reason;
    }

    // Whether closing the window is currently blocked.
    public bool IsCloseBlocked => _isCloseBlocked;

    public void ShowCloseBlockedNotice()
    {
        if (_isCloseBlockedNoticeOpen) return;

        _isCloseBlockedNoticeOpen = true;
        _ = ShowCloseBlockedNoticeAsync();
    }

    private async Task ShowCloseBlockedNoticeAsync()
    {
        try { await Content.ShowMessageDialogAsync(new MessageDialogParameters("안내", _closeBlockedReason ?? DefaultCloseBlockedReason)); }
        finally { _isCloseBlockedNoticeOpen = false; }
    }

    // Detaches the theme service subscription and the messenger registrations on close so the
    // application-lifetime theme service does not keep closed windows and their content trees alive.
    private void OnBaseWindowClosed(object _, WindowEventArgs __)
    {
        _applicationThemeService.ThemeChanged -= OnApplicationThemeServiceThemeChanged;
        UnregisterMessengerRecipients();
    }

    private void UnregisterMessengerRecipients()
    {
        WeakReferenceMessenger.Default.Unregister<LoadingStateRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ShowLoadingMessage>(this);
        WeakReferenceMessenger.Default.Unregister<HideLoadingMessage>(this);
        WeakReferenceMessenger.Default.Unregister<NavigationRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TryNavigateBackRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<WindowCloseBlockRequestedMessage>(this);
    }

    protected abstract void Navigate(Type pageType, object parameter);

    protected abstract bool TryNavigateBack();

    private async Task RunLoadingAsync(LoadingStateRequestedMessage message)
    {
        await _loadingSemaphore.WaitAsync();
        try
        {
            ShowLoading(message.LoadingMessage);
            await message.Action();
            message.Complete();
        }
        catch (Exception exception) { message.Fail(exception); }
        finally
        {
            HideLoading();
            _loadingSemaphore.Release();
        }
    }

    protected abstract void ShowLoading(string message = null);
    protected abstract void HideLoading();

    private void OnApplicationThemeServiceThemeChanged(ElementTheme theme) => _applicationThemeService.ApplyThemeToWindow(this);
}
