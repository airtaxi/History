using System;
using System.Collections.Generic;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
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

    public MediaWindow(MediaWindowViewModel viewModel) : base(viewModel)
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
        MediaFlipView.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    private void OnCloseKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    protected override void OnWindowClosed(object sender, WindowEventArgs args)
    {
        base.OnWindowClosed(sender, args);

        foreach (var media in _viewModel.Medias) media.ResetForReuse();
    }

    private const double ZoomStep = 0.25;
    private const double MinZoomFactor = 0.1;
    private const double MaxZoomFactor = 10.0;

    private void OnZoomOutClicked(object sender, RoutedEventArgs e) => ZoomBy(-ZoomStep);

    private void OnZoomInClicked(object sender, RoutedEventArgs e) => ZoomBy(ZoomStep);

    private void OnZoomResetClicked(object sender, RoutedEventArgs e) => ResetZoomToFit(false);

    // Flipping to another media resets its zoom to the 100% fit level without animation; if the
    // new item's container is not realized yet, the template's SizeChanged/ImageOpened handlers
    // fit it.
    private void OnMediaFlipViewSelectionChanged(object sender, SelectionChangedEventArgs e) => ResetZoomToFit(true);

    // Re-applies the contain-fit zoom (100%), matching the open-state fit from Media.xaml.cs.
    private void ResetZoomToFit(bool disableAnimation)
    {
        if (GetCurrentItemScrollViewer() is not ScrollViewer scrollViewer) return;
        if (MediaViewportHelper.CalculateFitZoomFactor(scrollViewer) is not { } zoomFactor) return;

        scrollViewer.ChangeView(null, null, (float)zoomFactor, disableAnimation);
    }

    private void ZoomBy(double delta)
    {
        if (GetCurrentItemScrollViewer() is not ScrollViewer scrollViewer) return;

        double targetZoom = Math.Clamp(scrollViewer.ZoomFactor + delta, MinZoomFactor, MaxZoomFactor);
        if (Math.Abs(targetZoom - scrollViewer.ZoomFactor) < 0.01) return;

        // Keep the current viewport center fixed while zooming.
        double centerX = scrollViewer.HorizontalOffset + scrollViewer.ViewportWidth / 2;
        double centerY = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight / 2;
        double factor = targetZoom / scrollViewer.ZoomFactor;
        double targetHorizontalOffset = centerX * factor - scrollViewer.ViewportWidth / 2;
        double targetVerticalOffset = centerY * factor - scrollViewer.ViewportHeight / 2;

        scrollViewer.ChangeView(targetHorizontalOffset, targetVerticalOffset, (float)targetZoom, false);
    }

    // The zoomable ScrollViewer lives inside the current FlipView item's template, so it is
    // located by walking the realized container's visual tree.
    private ScrollViewer GetCurrentItemScrollViewer()
    {
        if (MediaFlipView.ContainerFromItem(MediaFlipView.SelectedItem) is not FrameworkElement container) return null;

        Queue<DependencyObject> pending = new();
        pending.Enqueue(container);

        while (pending.Count > 0)
        {
            DependencyObject current = pending.Dequeue();
            if (current is ScrollViewer scrollViewer) return scrollViewer;

            int childCount = VisualTreeHelper.GetChildrenCount(current);
            for (int i = 0; i < childCount; i++) pending.Enqueue(VisualTreeHelper.GetChild(current, i));
        }

        return null;
    }
}
