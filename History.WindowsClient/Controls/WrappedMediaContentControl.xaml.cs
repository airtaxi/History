using History.WindowsClient.ViewModels.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    private void OnMediaItemImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is Image image && image.Source is BitmapSource bitmapSource && image.DataContext is MediaContentViewModel viewModel)
        {
            viewModel.ReportImageSize(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
        }
    }
}