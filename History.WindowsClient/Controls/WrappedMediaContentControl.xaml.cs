using History.WindowsClient.ViewModels.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.Controls;

public sealed partial class WrappedMediaContentControl : UserControl
{
    public static readonly DependencyProperty CarouselViewModelProperty = DependencyProperty.Register(nameof(CarouselViewModel), typeof(WrappedMediaContentsViewModel), typeof(WrappedMediaContentControl), new PropertyMetadata(null));

    public WrappedMediaContentControl() => InitializeComponent();

    public WrappedMediaContentsViewModel CarouselViewModel
    {
        get => (WrappedMediaContentsViewModel)GetValue(CarouselViewModelProperty);
        set => SetValue(CarouselViewModelProperty, value);
    }

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs e) => CarouselViewModel?.UpdateViewportWidth(e.NewSize.Width);

    // Swallows the pointer press so the enclosing post card's button cannot also raise its own
    // Click (Button Click is pointer-driven, so marking Tapped handled alone would not stop the
    // chained navigation to the wrapper post). The media templates still raise their own Tapped
    // gesture, which opens the full-screen viewer.
    private void OnRootGridPointerPressed(object sender, PointerRoutedEventArgs e) => e.Handled = true;
}