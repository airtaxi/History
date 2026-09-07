using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace History.WindowsClient.Helpers;

// Shared contain-fit zoom math for the media viewer: the full-screen media template
// (Resources/Media.xaml) and the viewer window zoom buttons (Views/MediaWindow.xaml)
// both fit the zoomed image exactly inside the viewport.
public static class MediaViewportHelper
{
    // Keeps the float-rounded zoomed extent strictly inside the viewport. Without this the
    // Auto scrollbars appear at the exact-fit boundary, shrink the viewport, and lock the
    // content into a small overflow that makes the ScrollViewer swallow the wheel (the
    // FlipView then never flips to the next media).
    public const double FitMargin = 2.0;

    // Returns the contain-fit zoom factor for the image inside the scroll viewer, or null
    // when no image is available or the computed factor is not usable.
    public static double? CalculateFitZoomFactor(ScrollViewer scrollViewer)
    {
        if (FindDescendantImage(scrollViewer) is not Image { Source: BitmapImage bitmap }) return null;

        double zoomFactor = Math.Min((scrollViewer.ActualWidth - FitMargin) / bitmap.PixelWidth, (scrollViewer.ActualHeight - FitMargin) / bitmap.PixelHeight);
        if (double.IsNaN(zoomFactor) || double.IsInfinity(zoomFactor) || zoomFactor <= 0) return null;

        return zoomFactor;
    }

    public static Image FindDescendantImage(DependencyObject root)
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
}
