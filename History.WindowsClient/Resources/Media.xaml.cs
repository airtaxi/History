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
}
