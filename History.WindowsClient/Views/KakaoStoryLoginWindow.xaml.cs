using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using WinUIEx;

namespace History.WindowsClient.Views;

// Kakao Story OAuth login window hosting the KakaoStoryLoginWindowViewModel. Subclasses
// BaseWindow so the theme, icon, centering, and loading-message routing apply automatically;
// the view model's dialog/loading events are fulfilled directly on this window's content.
public sealed partial class KakaoStoryLoginWindow : BaseWindow
{
    private static readonly TimeSpan LoginResultPollInterval = TimeSpan.FromMilliseconds(1250);

    private readonly KakaoStoryLoginWindowViewModel _viewModel;
    private DispatcherQueueTimer _timer;
    private string _currentUrl;

    public KakaoStoryLoginWindowViewModel ViewModel => _viewModel;

    public KakaoStoryLoginWindow(KakaoStoryLoginWindowViewModel viewModel) : base()
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();

        SubscribeViewModelEvents();
    }

    public Task<bool> GetResultAsync() => _viewModel.GetResultAsync();

    // no-op for this window
    protected override void Navigate(Type pageType, object parameter) { }

    // no-op for this window
    protected override bool TryNavigateBack() => false;

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
        BrowserWebView.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    private void SubscribeViewModelEvents() => _viewModel.MessageDialogRequested += OnMessageDialogRequested;

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = Content.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await BrowserWebView.EnsureCoreWebView2Async();
        BrowserWebView.CoreWebView2.Navigate(_viewModel.BuildAuthorizeUrl());

        // The kauth session silently issues an authorization code when the account
        // is still signed in; otherwise the user completes the login on the page.
        // Polling picks up the captured s/oauth callback URL once the code is available.
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = LoginResultPollInterval;
        _timer.Tick += (_, __) => _ = CheckLoginResultAsync();
        _timer.Start();

        Activate();
    }

    private void OnBrowserWebViewNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs e)
    {
        SetLoadingState(Visibility.Visible, null);

        // The s/oauth callback page runs the web client's oauth.min.js, which
        // exchanges (and thereby consumes) the authorization code itself. Cancel
        // the navigation so the code survives for the kauth token exchange.
        if (e.Uri?.StartsWith(KakaoStoryLoginWindowViewModel.OAuthRedirectUri) == true)
        {
            _currentUrl = e.Uri;
            e.Cancel = true;
        }
    }

    private async void OnBrowserWebViewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        SetLoadingState(Visibility.Collapsed, null);

        // A cancelled navigation reports the previous page's URL (the kauth page),
        // which would clobber the s/oauth URL captured in OnBrowserWebViewNavigationStarting.
        // Keep the pending code URL so CheckLoginResult completes.
        if (_currentUrl?.StartsWith(KakaoStoryLoginWindowViewModel.OAuthRedirectUri) != true) _currentUrl = e.Uri;

        await CheckLoginResultAsync();
        await UncheckSaveSignedInAsync();
        await TryAutoFillCredentialsAsync();
    }

    private async Task CheckLoginResultAsync()
    {
        if (await _viewModel.TryCompleteLoginAsync(_currentUrl))
        {
            Close();
        }
    }

    private async Task UncheckSaveSignedInAsync()
    {
        var script = @"
            (function() {
                var saveSignedIn = document.querySelector('input[name=""saveSignedIn""]');
                if (saveSignedIn && saveSignedIn.checked) saveSignedIn.click();
            })();
        ";
        try { await BrowserWebView.CoreWebView2.ExecuteScriptAsync(script); }
        catch { }
    }

    private async Task TryAutoFillCredentialsAsync()
    {
        var savedEmail = await KakaoStoryCredentialStore.GetEmailAsync();
        var savedPassword = await KakaoStoryCredentialStore.GetPasswordAsync();
        if (string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedPassword)) return;

        try
        {
            await Task.Delay(500);

            var escapedEmail = savedEmail.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var escapedPassword = savedPassword.Replace("\\", "\\\\").Replace("\"", "\\\"");

            var script = $@"
                (function tryFill(attempts) {{
                    var proto = Object.getOwnPropertyDescriptor(
                        window.HTMLInputElement.prototype, 'value');
                    var nativeSetter = proto && proto.set;

                    var emailInput = document.querySelector('input[name=""loginId""]');
                    var passInput = document.querySelector('input[name=""password""]');
                    var btn = document.querySelector('button.submit[type=""submit""]');

                    if (!nativeSetter || !emailInput || !passInput || !btn) {{
                        if (attempts > 0) setTimeout(function() {{ tryFill(attempts - 1); }}, 500);
                        return false;
                    }}

                    nativeSetter.call(emailInput, ""{escapedEmail}"");
                    emailInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    emailInput.dispatchEvent(new Event('change', {{ bubbles: true }}));

                    nativeSetter.call(passInput, ""{escapedPassword}"");
                    passInput.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    passInput.dispatchEvent(new Event('change', {{ bubbles: true }}));

                    setTimeout(function() {{ btn.click(); }}, 300);
                    return true;
                }})(5);
            ";

            await BrowserWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch { await _viewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "저장된 로그인 정보 자동 입력에 실패하였습니다. 수동으로 로그인해주세요.")); }
    }

    private void OnEscapeKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _timer?.Stop();
        UnregisterMessengerRecipients();
        _viewModel.CompleteAsCanceled();
    }
}