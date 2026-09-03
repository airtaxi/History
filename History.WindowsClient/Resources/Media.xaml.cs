using History.WindowsClient.ViewModels.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

namespace History.WindowsClient.Resources;

public sealed partial class Media : ResourceDictionary
{
    public Media() => InitializeComponent();

    private void OnMediaItemImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is Image image && image.Source is BitmapSource bitmapSource && image.DataContext is MediaContentViewModel viewModel)
        {
            viewModel.ReportImageSize(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
        }
    }

    // Tapping the media opens the full-screen viewer and marks the gesture handled so the
    // enclosing post card cannot also navigate: the timeline card button's Click is
    // pointer-driven and already blocked by the pointer-press swallow on the carousel control,
    // while the shared post's Tapped handler is gesture-driven and would otherwise chain to the
    // wrapper post.
    // Taps that originate inside a button (video/spoiler overlays) keep their own behavior.
    private void OnInlineMediaTapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject originalSource) return;
        if (IsInsideInteractiveElement(originalSource, (FrameworkElement)sender)) return;

        e.Handled = true;

        if (sender is FrameworkElement { DataContext: MediaContentViewModel viewModel }) viewModel.HandleTapCommand.Execute(null);
    }

    // Taps that originate inside a button (video/spoiler overlays) keep their own behavior.
    private static bool IsInsideInteractiveElement(DependencyObject source, FrameworkElement root)
    {
        DependencyObject current = source;
        while (current != null && current != root)
        {
            if (current is ButtonBase) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    // Shift+wheel pans horizontally instead of scrolling vertically. The raw wheel delta is
    // applied with an inverted sign so wheel-up moves the view left, matching the native wheel
    // direction convention; ChangeView clamps the offset to the scrollable range.
    // The handler is attached to the template content rather than the ScrollViewer itself:
    // the ScrollViewer's internal wheel handling marks PointerWheelChanged handled before
    // XAML handlers on it would run, so the content source sees the event first.
    private void OnFullScreenMediaPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift)) return;
        if (sender is not FrameworkElement element) return;
        if (FindAncestorScrollViewer(element) is not ScrollViewer scrollViewer) return;

        var wheelDelta = e.GetCurrentPoint(scrollViewer).Properties.MouseWheelDelta;
        scrollViewer.ChangeView(scrollViewer.HorizontalOffset - wheelDelta, null, null);
        e.Handled = true;
    }

    // Tapping the media returns the zoom to the 100% fit level, like the reset button. Taps
    // that originate inside a button (e.g. the video transport controls) keep their own behavior.
    private void OnFullScreenMediaTapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject originalSource) return;
        if (IsInsideInteractiveElement(originalSource, (FrameworkElement)sender)) return;
        if (sender is not FrameworkElement element) return;
        if (FindAncestorScrollViewer(element) is ScrollViewer scrollViewer) FitImageToViewport(scrollViewer);
    }

    // The media keeps its natural size and the ScrollViewer zoom factor is set to the
    // contain-fit value, so the viewer opens at a 100% fit with the media centered (the
    // Image's own alignment centers content smaller than the viewport). Re-runs when the
    // image finishes decoding and when the viewport resizes.
    private void OnFullScreenScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            FitImageToViewport(scrollViewer);
        }
    }

    private void OnFullScreenMediaImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        if (FindAncestorScrollViewer(element) is ScrollViewer scrollViewer) FitImageToViewport(scrollViewer);
    }

    private static void FitImageToViewport(ScrollViewer scrollViewer)
    {
        if (FindDescendantImage(scrollViewer) is not Image { Source: BitmapImage bitmap }) return;

        double zoomFactor = Math.Min(scrollViewer.ActualWidth / bitmap.PixelWidth, scrollViewer.ActualHeight / bitmap.PixelHeight);
        if (double.IsNaN(zoomFactor) || double.IsInfinity(zoomFactor) || zoomFactor <= 0) return;

        scrollViewer.ChangeView(null, null, (float)zoomFactor, true);
    }

    private static ScrollViewer FindAncestorScrollViewer(DependencyObject source)
    {
        DependencyObject current = source;
        while (current != null)
        {
            if (current is ScrollViewer scrollViewer) return scrollViewer;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
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

    private void OnVideoMediaPlayerElementLoaded(object sender, RoutedEventArgs e)
    {
        var mediaPlayerElement = sender as MediaPlayerElement;
        if (mediaPlayerElement == null) return;

        mediaPlayerElement.MediaPlayer.IsMuted = true;
    }
}
