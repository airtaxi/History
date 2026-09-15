using History.WindowsClient.Messages;
using History.WindowsClient.Pages.Extras;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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
    private static ExtrasWindow s_instance;

    public static ExtrasWindow Instance => s_instance;

    private readonly ExtrasWindowViewModel _viewModel;

    public ExtrasWindowViewModel ViewModel => _viewModel;

    public ExtrasWindow(ExtrasWindowViewModel viewModel) : base(viewModel)
    {
        s_instance = this;

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

    // Keeps the title bar back button in sync with the frame's back stack, labels the window
    // after the page it is currently hosting, and shows the page-specific actions for pages
    // that support them.
    private void OnExtrasFrameNavigated(object sender, NavigationEventArgs e)
    {
        var isInviteCodesPage = e.SourcePageType == typeof(InviteCodesPage);
        var isInviteCodeRequestsPage = e.SourcePageType == typeof(InviteCodeRequestsPage);
        var isStickersPage = e.SourcePageType == typeof(StickersPage);
        var isModerationRecordsPage = e.SourcePageType == typeof(ModerationRecordsPage);

        AppTitleBar.IsBackButtonVisible = ExtrasFrame.CanGoBack;
        AppTitleBar.Title = e.SourcePageType switch
        {
            _ when e.SourcePageType == typeof(StickersPage) => "스티커",
            _ when e.SourcePageType == typeof(InviteCodesPage) => "초대 코드",
            _ when e.SourcePageType == typeof(InviteCodeRequestsPage) => "초대 코드 요청 관리",
            _ when e.SourcePageType == typeof(ModerationRecordsPage) => "제재 내역",
            _ when e.SourcePageType == typeof(BulkPostManagePage) => "게시글 일괄 관리",
            _ => "부가메뉴",
        };
        AddButton.Visibility = isStickersPage || isInviteCodesPage ? Visibility.Visible : Visibility.Collapsed;
        RefreshButton.Visibility = isStickersPage || isInviteCodesPage || isInviteCodeRequestsPage || isModerationRecordsPage ? Visibility.Visible : Visibility.Collapsed;

        // The add button starts the create flow of the hosted page, so its label follows it.
        var addButtonText = isStickersPage ? "스티커 만들기" : "초대 코드 요청";
        ToolTipService.SetToolTip(AddButton, addButtonText);
        AutomationProperties.SetName(AddButton, addButtonText);
    }

    private void OnAppTitleBarBackRequested(TitleBar sender, object args)
    {
        if (ExtrasFrame.CanGoBack)
        {
            ExtrasFrame.GoBack();
        }
    }

    // Refreshes the hosted page through the messenger so the request stays inside this window.
    private void OnRefreshButtonClicked(object sender, RoutedEventArgs e) => RefreshRequestedMessage.Send(Content.XamlRoot);

    // Runs the hosted page's add flow through the messenger so the request stays inside this
    // window: sticker pages open the sticker create flow, invite code pages the request flow.
    private void OnAddButtonClicked(object sender, RoutedEventArgs e)
    {
        if (ExtrasFrame.CurrentSourcePageType == typeof(StickersPage)) StickerCreateRequestedMessage.Send(Content.XamlRoot);
        else InviteCodeRequestRequestedMessage.Send(Content.XamlRoot);
    }

    // Ctrl+R and F5 refresh the hosted page the same way the title bar refresh button does,
    // but only while that button is part of the current page's toolbar.
    private void OnRefreshKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (RefreshButton.Visibility != Visibility.Visible) return;

        args.Handled = true;
        RefreshRequestedMessage.Send(Content.XamlRoot);
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

    // Clears the static instance on close so the closed window and its content tree are not
    // kept alive for the rest of the app session (hosted flows use the instance as their modal
    // owner).
    protected override void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (ReferenceEquals(s_instance, this)) s_instance = null;

        base.OnWindowClosed(sender, args);
    }
}
