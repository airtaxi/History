using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace History.WindowsClient.Views;

public abstract class BaseWindow : Window,
    IRecipient<LoadingStateRequestedMessage>,
    IRecipient<ShowLoadingMessage>,
    IRecipient<HideLoadingMessage>
{
    protected readonly ApplicationThemeService _applicationThemeService = App.Services.GetRequiredService<ApplicationThemeService>();

    public BaseWindow()
    {
        _applicationThemeService.ApplyThemeToWindow(this);
        _applicationThemeService.ThemeChanged += OnApplicationThemeServiceThemeChanged;

        this.CenterOnScreen();

        AppWindow.SetIcon("Assets/Icon.ico");

        WeakReferenceMessenger.Default.Register((IRecipient<LoadingStateRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<ShowLoadingMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<HideLoadingMessage>)this);
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

    private async Task RunLoadingAsync(LoadingStateRequestedMessage message)
    {
        try
        {
            ShowLoading(message.LoadingMessage);
            await message.Action();
            message.Complete();
        }
        catch (Exception exception) { message.Fail(exception); }
        finally { HideLoading(); }
    }

    protected abstract void ShowLoading(string message = null);
    protected abstract void HideLoading();

    private void OnApplicationThemeServiceThemeChanged(ElementTheme theme) => _applicationThemeService.ApplyThemeToWindow(this);
}
