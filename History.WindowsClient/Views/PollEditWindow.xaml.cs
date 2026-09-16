using System.Collections.Specialized;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels.Poll;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using WinUIEx;

namespace History.WindowsClient.Views;

// Poll edit window hosting the PollEditWindowViewModel. Subclasses BaseWindow so the
// theme, icon, centering, and loading-message routing apply automatically; the view
// model's dialog events are fulfilled directly on this window's content.
public sealed partial class PollEditWindow : BaseWindow
{
    private int WindowWidth { get; } = 540;
    private readonly PollEditWindowViewModel _viewModel;

    public PollEditWindowViewModel ViewModel => _viewModel;

    public PollEditWindow(PollEditWindowViewModel viewModel) : base(viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();
    }

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
        LoadingTextBlock.Text = message;
    }

    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        _viewModel.Confirmed += OnViewModelConfirmed;
        _viewModel.Options.CollectionChanged += OnOptionsCollectionChanged;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        _viewModel.Confirmed -= OnViewModelConfirmed;
        _viewModel.Options.CollectionChanged -= OnOptionsCollectionChanged;

        base.UnsubscribeViewModelEvents();
    }

    // Fits the window to the content: measures the root grid's DesiredSize and resizes the
    // window's client area. Runs once on load and whenever the option rows change.
    private void UpdateWindowSize()
    {
        if (RootGrid.XamlRoot == null) return;

        RootGrid.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

        var dpiScale = RootGrid.XamlRoot.RasterizationScale;
        var desiredWidth = WindowWidth;

        // Re-measure at the final width so wrap-sensitive content reports its actual height.
        RootGrid.Measure(new Windows.Foundation.Size(desiredWidth, double.PositiveInfinity));

        var desiredHeight = RootGrid.DesiredSize.Height;
        if (ExtendsContentIntoTitleBar) desiredHeight -= 30;

        AppWindow.ResizeClient(new SizeInt32((int)Math.Ceiling(desiredWidth * dpiScale), (int)Math.Ceiling(desiredHeight * dpiScale)));
        this.CenterOnScreen();
    }

    // Fits the window whenever an option row is added or removed.
    private void OnOptionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => DispatcherQueue.TryEnqueue(UpdateWindowSize);

    // The poll definition was confirmed: report it and close the window.
    private void OnViewModelConfirmed(object sender, PollContent pollContent) => Close();

    protected override void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        base.OnWindowLoaded(sender, e);

        UpdateWindowSize();

        Activate();
    }

    private void OnCloseKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
