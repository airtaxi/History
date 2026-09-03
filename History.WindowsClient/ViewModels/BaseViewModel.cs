using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Enums;
using History.Commons.Interfaces;
using History.WindowsClient.Models;
using History.WindowsClient.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System.Net;

namespace History.WindowsClient.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    public event EventHandler<MessageDialogRequestedEventArgs> MessageDialogRequested;
    public event EventHandler<InputDialogRequestedEventArgs> InputDialogRequested;
    public event EventHandler<ContentDialogRequestedEventArgs> ContentDialogRequested;
    public event EventHandler<SelectionDialogRequestedEventArgs> SelectionDialogRequested;
    public event EventHandler<PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult>> FilePickRequested;
    public event EventHandler<PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>>> FilesPickRequested;
    public event EventHandler<PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult>> SaveFileRequested;
    public event EventHandler<PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult>> FolderPickRequested;
    public event EventHandler<LoadingStateRequestedEventArgs> LoadingStateRequested;
    public event EventHandler<ShowLoadingRequestedEventArgs> ShowLoadingRequested;
    public event EventHandler<HideLoadingRequestedEventArgs> HideLoadingRequested;
    public event EventHandler<NavigationRequestedEventArgs> NavigationRequested;
    public event EventHandler<TryNavigateBackRequestedEventArgs> TryNavigateBackRequested;

    public async Task<ContentDialogResult?> ShowMessageDialogAsync(MessageDialogParameters parameters)
    {
        var args = new MessageDialogRequestedEventArgs(parameters);
        MessageDialogRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    // Input dialog requests fulfilled by the host page (mirrors ShowMessageDialogAsync).
    public async Task<string> ShowInputDialogAsync(InputDialogParameters parameters)
    {
        var args = new InputDialogRequestedEventArgs(parameters);
        InputDialogRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    // Prebuilt ContentDialog requests fulfilled by the host page (mirrors ShowMessageDialogAsync).
    public async Task<ContentDialogResult?> ShowContentDialogAsync(ContentDialog dialog)
    {
        var args = new ContentDialogRequestedEventArgs(dialog);
        ContentDialogRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    // Selection dialog requests fulfilled by the host page (mirrors ShowInputDialogAsync).
    public async Task<string> ShowSelectionDialogAsync(string title, IReadOnlyList<string> options)
    {
        var args = new SelectionDialogRequestedEventArgs(title, options);
        SelectionDialogRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    // Picker requests fulfilled by the host page (mirrors ShowMessageDialogAsync).
    public async Task<PickFileResult> PickFileAsync(FileOpenPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult>(parameters);
        FilePickRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    public async Task<IReadOnlyList<PickFileResult>> PickFilesAsync(FileOpenPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>>(parameters);
        FilesPickRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    public async Task<PickFileResult> SaveFileAsync(FileSavePickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult>(parameters);
        SaveFileRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    public async Task<PickFolderResult> PickFolderAsync(FolderPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult>(parameters);
        FolderPickRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }

    // Loading requests are fulfilled by the host page or control, which forwards the
    // action to the owning window through the weak-reference messenger. Without a
    // subscriber (detached view model), the action still runs without the overlay.
    public async Task ExecuteWithLoadingAsync(Func<Task> action, string loadingMessage = null)
    {
        var args = new LoadingStateRequestedEventArgs(loadingMessage, action);
        LoadingStateRequested?.Invoke(this, args);
        if (args.ResultTask == null) await action();
        else await args.ResultTask;
    }

    public async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string loadingMessage = null)
    {
        var result = default(T);

        // The protocol action is Func<Task>, so wrap the typed action in a closure that
        // captures the result for the return value (the window never sees the T value).
        var args = new LoadingStateRequestedEventArgs(loadingMessage, async () => result = await action());
        LoadingStateRequested?.Invoke(this, args);
        if (args.ResultTask == null) await args.Action();
        else await args.ResultTask;

        return result;
    }

    // Requests the owning window to show the loading overlay with the optional message.
    public void ShowLoading(string loadingMessage = null)
    {
        var args = new ShowLoadingRequestedEventArgs(loadingMessage);
        ShowLoadingRequested?.Invoke(this, args);
    }

    // Requests the owning window to hide the loading overlay.
    public void HideLoading()
    {
        var args = new HideLoadingRequestedEventArgs();
        HideLoadingRequested?.Invoke(this, args);
    }

    // Requests the owning window to navigate its root frame to the given page type with
    // the given parameter (fulfilled by the host page or control).
    public void RequestNavigation(Type pageType, object parameter)
    {
        var args = new NavigationRequestedEventArgs(pageType, parameter);
        NavigationRequested?.Invoke(this, args);
    }

    // Requests the owning window to navigate its root frame back; returns whether the back
    // navigation actually happened (fulfilled by the host page or control).
    public async Task<bool> TryNavigateBackAsync()
    {
        var args = new TryNavigateBackRequestedEventArgs();
        TryNavigateBackRequested?.Invoke(this, args);
        if (args.ResultTask == null) return false;

        return await args.ResultTask;
    }

    public async Task<Result> ExecuteRequestAsync(IBaseRequest request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            await ExecuteWithLoadingAsync(() => CommonShared.ApiHandler.ExecuteRequestAsync(request));
            return Result.Success();
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await App.ShowErrorDialogAsync($"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}");
            return (errorType, exception.Message);
        }
    }

    public async Task<Result<T>> ExecuteRequestAsync<T>(IBaseRequest<T> request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try { return await ExecuteWithLoadingAsync(() => CommonShared.ApiHandler.ExecuteRequestAsync(request)); }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await App.ShowErrorDialogAsync($"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}");
            return (errorType, exception.Message);
        }
    }

    private static ErrorType StatusCodeToErrorType(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Forbidden => ErrorType.Forbidden,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        HttpStatusCode.BadRequest => ErrorType.BadRequest,
        HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
        _ => ErrorType.ProgramError,
    };
}