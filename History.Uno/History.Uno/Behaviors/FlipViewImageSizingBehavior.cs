using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Xaml.Interactivity;
using Windows.Graphics.Display;

namespace History.Uno.Behaviors;

/// <summary>
/// Fits an Image inside a FlipView item to Math.Min(naturalHeight, flipViewHeight) once the
/// web image has been decoded (ImageOpened), so short images are not stretched up to the
/// carousel viewport and tall images do not exceed it. No-op when there is no FlipView ancestor.
/// </summary>
public sealed partial class FlipViewImageSizingBehavior : Behavior<Image>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.ImageOpened += OnImageOpenedOrLoaded;
        AssociatedObject.Loaded += OnImageOpenedOrLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.ImageOpened -= OnImageOpenedOrLoaded;
        AssociatedObject.Loaded -= OnImageOpenedOrLoaded;
    }

    private void OnImageOpenedOrLoaded(object sender, RoutedEventArgs args)
    {
        var flipView = AssociatedObject.FindParent<FlipView>();
        FitFlipView(flipView);

        try
        {
            WeakReferenceMessenger.Default.Register<SpanChangedMessage>(flipView, OnSpanChangedMessage);
            WeakReferenceMessenger.Default.Register<CarouselPositionChangedMessage>(flipView, OnCarouselPositionChangedMessage);
        }
        catch { } // Suppress duplicate registration exception
    }

    private void OnCarouselPositionChangedMessage(object recipient, CarouselPositionChangedMessage message)
    {
        var flipView = recipient as FlipView;

        if (message.Value == flipView.SelectedItem) FitFlipView(flipView);
    }

    private void OnSpanChangedMessage(object recipient, SpanChangedMessage message) => FitFlipView(recipient as FlipView);

    private void FitFlipView(FlipView flipView)
    {
        //if (flipView == null) return;

        //var parent = flipView.Parent as FrameworkElement;
        //flipView.Width = parent.ActualWidth;

        //if (AssociatedObject.Source is not BitmapSource bitmap || bitmap.PixelHeight <= 0) return;
        //flipView.Height = bitmap.PixelHeight;
    }
}
