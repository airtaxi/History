using History.WindowsClient.Pages.Extras;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;
using TitleBar = Microsoft.UI.Xaml.Controls.TitleBar;

namespace History.WindowsClient.Views;

// Extras window shell hosting the extras pages in its own frame. Subclasses BaseWindow so the
// theme, icon, centering, and loading-message routing apply automatically; the window owns the
// frame navigation and the title bar back button, and fulfills the hosted pages' view model
// dialog/loading requests on this window's content.
public sealed partial class ExtrasWindow : BaseWindow
{
    private readonly ExtrasWindowViewModel _viewModel;

    public ExtrasWindowViewModel ViewModel => _viewModel;

    public ExtrasWindow(ExtrasWindowViewModel viewModel) : base(viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();

        ExtrasFrame.Navigate(typeof(ExtrasPage));
    }

    protected override void Navigate(Type pageType, object parameter) => ExtrasFrame.Navigate(pageType, parameter);

    protected override bool TryNavigateBack()
    {
        if (!ExtrasFrame.CanGoBack) return false;

        ExtrasFrame.GoBack();
        return true;
    }

    // Keeps the title bar back button in sync with the frame's back stack.
    private void OnExtrasFrameNavigated(object sender, NavigationEventArgs e) => AppTitleBar.IsBackButtonVisible = ExtrasFrame.CanGoBack;

    private void OnAppTitleBarBackRequested(TitleBar sender, object args)
    {
        if (ExtrasFrame.CanGoBack)
        {
            ExtrasFrame.GoBack();
        }
    }

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
        ExtrasFrame.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    protected override void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        base.OnWindowLoaded(sender, e);

        Activate();
    }

    private void OnCloseKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
