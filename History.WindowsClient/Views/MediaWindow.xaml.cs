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

    public MediaWindow(MediaWindowViewModel viewModel) : base()
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();

        SubscribeViewModelEvents();
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

    private void SubscribeViewModelEvents()
    {
        _viewModel.MessageDialogRequested += OnMessageDialogRequested;
        _viewModel.SelectionDialogRequested += OnSelectionDialogRequested;
        _viewModel.SaveFileRequested += OnSaveFileRequested;
        _viewModel.FolderPickRequested += OnFolderPickRequested;
        _viewModel.LoadingStateRequested += OnLoadingStateRequested;
    }

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = Content.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    private void OnSelectionDialogRequested(object sender, SelectionDialogRequestedEventArgs args)
    {
        var result = Content.ShowSelectionDialogAsync(args.Title, args.Options);
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
    // weak-reference messenger; BaseWindow routes them by XamlRoot.
    private void OnLoadingStateRequested(object sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(Content.XamlRoot, args);

    private void OnEscapeKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        UnregisterMessengerRecipients();

        foreach (var media in _viewModel.Medias)
        {
            media.ResetForReuse();
        }
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
        if (FindDescendantImage(scrollViewer) is not Image { Source: BitmapImage bitmap }) return;

        double zoomFactor = Math.Min(scrollViewer.ActualWidth / bitmap.PixelWidth, scrollViewer.ActualHeight / bitmap.PixelHeight);
        if (double.IsNaN(zoomFactor) || double.IsInfinity(zoomFactor) || zoomFactor <= 0) return;

        scrollViewer.ChangeView(null, null, (float)zoomFactor, disableAnimation);
    }

    private static Image FindDescendantImage(DependencyObject root)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is Image image) return image;
            if (FindDescendantImage(child) is Image found) return found;
        }
        return null;
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