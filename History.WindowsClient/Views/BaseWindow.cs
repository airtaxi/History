using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace History.WindowsClient.Views;

public abstract class BaseWindow : WindowEx,
    IRecipient<LoadingStateRequestedMessage>,
    IRecipient<ShowLoadingMessage>,
    IRecipient<HideLoadingMessage>,
    IRecipient<NavigationRequestedMessage>,
    IRecipient<TryNavigateBackRequestedMessage>
{
    // Serializes this window's loading overlay sequences: concurrent loading requests from
    // different pages (for example MainPage and TimelinePage refreshing on startup) would
    // otherwise show and hide the overlay in an interleaved order, toggling the overlay
    // Visibility from inside the layout passes that are still settling during the initial load.
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

    protected readonly ApplicationThemeService _applicationThemeService = App.Services.GetRequiredService<ApplicationThemeService>();

    public BaseWindow()
    {
        _applicationThemeService.ApplyThemeToWindow(this);
        _applicationThemeService.ThemeChanged += OnApplicationThemeServiceThemeChanged;

        AppWindow.SetIcon("Assets/Icon.ico");

        WeakReferenceMessenger.Default.Register((IRecipient<LoadingStateRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<ShowLoadingMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<HideLoadingMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NavigationRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<TryNavigateBackRequestedMessage>)this);
    }

    // Runs loading requests that originated from this window's pages/controls: the
    // XamlRoot reference comparison routes messages from other windows away.
    public void Receive(LoadingStateRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        _ = RunLoadingAsync(message);
    }

    public void Receive(ShowLoadingMessage message) => ShowLoading(message.LoadingMessage);

    public void Receive(HideLoadingMessage message) => HideLoading();

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

    protected void UnregisterMessengerRecipients()
    {
        WeakReferenceMessenger.Default.Unregister<LoadingStateRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ShowLoadingMessage>(this);
        WeakReferenceMessenger.Default.Unregister<HideLoadingMessage>(this);
        WeakReferenceMessenger.Default.Unregister<NavigationRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TryNavigateBackRequestedMessage>(this);
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
