using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.Pages;

public partial class BasePage : Page
{
    // The view model instance the shared handlers are attached to, so navigation detaches the
    // same instance even when a page switches its active view model in between.
    private BaseViewModel _subscribedViewModel;

    private bool _isFirstLoad;

    protected virtual BaseViewModel ViewModel { get; }

    // Whether this page is the active page in its frame; maintained by the navigation lifecycle.
    protected bool IsInForeground { get; private set; }

    // XAML-declared first-load hook: derived page roots declare Loaded="OnPageLoaded". The base
    // runs the derived first-load work exactly once per page instance and ignores later passes.
    protected void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_isFirstLoad) return;
        _isFirstLoad = true;

        OnFirstPageLoad();
    }

    // Runs once per page instance, after the page's XamlRoot is available; start initial data
    // loading and other XamlRoot-dependent work here.
    protected virtual void OnFirstPageLoad() { }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        SubscribeViewModelEvents();
        IsInForeground = true;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        IsInForeground = false;

        // Leaving the page aborts any running flow it guarded, so a close block it requested
        // must not outlive the page and lock the window shut.
        WindowCloseBlockRequestedMessage.Send(XamlRoot, false);

        UnsubscribeViewModelEvents();
        base.OnNavigatedFrom(e);
    }

    // Subscribes the shared view model events every page fulfills on its own content: dialogs,
    // pickers, loading overlay, and navigation requests. Derived pages override this and call
    // base to add the events that are specific to their view model.
    protected virtual void SubscribeViewModelEvents()
    {
        if (ViewModel == null) return;

        _subscribedViewModel = ViewModel;

        _subscribedViewModel.MessageDialogRequested += OnMessageDialogRequested;
        _subscribedViewModel.InputDialogRequested += OnInputDialogRequested;
        _subscribedViewModel.ContentDialogRequested += OnContentDialogRequested;
        _subscribedViewModel.SelectionDialogRequested += OnSelectionDialogRequested;
        _subscribedViewModel.FilePickRequested += OnFilePickRequested;
        _subscribedViewModel.FilesPickRequested += OnFilesPickRequested;
        _subscribedViewModel.SaveFileRequested += OnSaveFileRequested;
        _subscribedViewModel.FolderPickRequested += OnFolderPickRequested;
        _subscribedViewModel.LoadingStateRequested += OnLoadingStateRequested;
        _subscribedViewModel.ShowLoadingRequested += OnShowLoadingRequested;
        _subscribedViewModel.HideLoadingRequested += OnHideLoadingRequested;
        _subscribedViewModel.NavigationRequested += OnNavigationRequested;
        _subscribedViewModel.TryNavigateBackRequested += OnTryNavigateBackRequested;
        _subscribedViewModel.WindowCloseBlockRequested += OnWindowCloseBlockRequested;
    }

    protected virtual void UnsubscribeViewModelEvents()
    {
        if (_subscribedViewModel == null) return;

        _subscribedViewModel.MessageDialogRequested -= OnMessageDialogRequested;
        _subscribedViewModel.InputDialogRequested -= OnInputDialogRequested;
        _subscribedViewModel.ContentDialogRequested -= OnContentDialogRequested;
        _subscribedViewModel.SelectionDialogRequested -= OnSelectionDialogRequested;
        _subscribedViewModel.FilePickRequested -= OnFilePickRequested;
        _subscribedViewModel.FilesPickRequested -= OnFilesPickRequested;
        _subscribedViewModel.SaveFileRequested -= OnSaveFileRequested;
        _subscribedViewModel.FolderPickRequested -= OnFolderPickRequested;
        _subscribedViewModel.LoadingStateRequested -= OnLoadingStateRequested;
        _subscribedViewModel.ShowLoadingRequested -= OnShowLoadingRequested;
        _subscribedViewModel.HideLoadingRequested -= OnHideLoadingRequested;
        _subscribedViewModel.NavigationRequested -= OnNavigationRequested;
        _subscribedViewModel.TryNavigateBackRequested -= OnTryNavigateBackRequested;
        _subscribedViewModel.WindowCloseBlockRequested -= OnWindowCloseBlockRequested;

        _subscribedViewModel = null;
    }

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = this.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Fulfills the view model's input dialog requests with the page-bound dialog.
    private void OnInputDialogRequested(object sender, InputDialogRequestedEventArgs args)
    {
        var result = this.ShowInputDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Fulfills the view model's prebuilt dialog requests with the page-bound dialog.
    private void OnContentDialogRequested(object sender, ContentDialogRequestedEventArgs args)
    {
        var result = this.ShowContentDialogAsync(args.Dialog);
        args.ResultTask = result;
    }

    // Fulfills the view model's selection dialog requests with the page-bound dialog.
    private void OnSelectionDialogRequested(object sender, SelectionDialogRequestedEventArgs args)
    {
        var result = this.ShowSelectionDialogAsync(args.Title, args.Options);
        args.ResultTask = result;
    }

    // Fulfills the view model's picker requests with the page-bound pickers.
    private void OnFilePickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult> args)
    {
        var result = this.PickFileAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnFilesPickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>> args)
    {
        var result = this.PickFilesAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnSaveFileRequested(object sender, PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult> args)
    {
        var result = this.SaveFileAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnFolderPickRequested(object sender, PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult> args)
    {
        var result = this.PickFolderAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Forwards the view model's loading requests to the owning window through the
    // weak-reference messenger (the window matches the page's XamlRoot).
    private void OnLoadingStateRequested(object sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(XamlRoot, args);

    private void OnShowLoadingRequested(object sender, ShowLoadingRequestedEventArgs args) => ShowLoadingMessage.Send(args);

    private void OnHideLoadingRequested(object sender, HideLoadingRequestedEventArgs args) => HideLoadingMessage.Send();

    // Forwards the view model's navigation requests to the owning window through the
    // weak-reference messenger (the window matches the page's XamlRoot).
    private void OnNavigationRequested(object sender, NavigationRequestedEventArgs args) => NavigationRequestedMessage.Send(XamlRoot, args.PageType, args.Parameter);

    // Forwards the view model's back navigation requests to the owning window through the
    // weak-reference messenger (the window matches the page's XamlRoot).
    private void OnTryNavigateBackRequested(object sender, TryNavigateBackRequestedEventArgs args) => TryNavigateBackRequestedMessage.Send(XamlRoot, args);

    // Forwards the view model's close-block requests to the owning window through the
    // weak-reference messenger (the window matches the page's XamlRoot).
    private void OnWindowCloseBlockRequested(object sender, WindowCloseBlockRequestedEventArgs args) => WindowCloseBlockRequestedMessage.Send(XamlRoot, args.IsCloseBlocked, args.Reason);
}
