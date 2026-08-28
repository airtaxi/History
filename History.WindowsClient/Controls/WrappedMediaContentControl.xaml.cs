using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.WindowsClient.ViewModels.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.Controls;

public sealed partial class WrappedMediaContentControl : UserControl
{
    public static readonly DependencyProperty MediaContentsProperty = DependencyProperty.Register(nameof(MediaContents), typeof(List<MediaContent>), typeof(WrappedMediaContentControl), new PropertyMetadata(null, OnDataPropertyChanged));
    public static readonly DependencyProperty AllMediaContentsProperty = DependencyProperty.Register(nameof(AllMediaContents), typeof(List<MediaContent>), typeof(WrappedMediaContentControl), new PropertyMetadata(null, OnDataPropertyChanged));
    public static readonly DependencyProperty PostTypeProperty = DependencyProperty.Register(nameof(PostType), typeof(PostType), typeof(WrappedMediaContentControl), new PropertyMetadata(PostType.Timeline, OnDataPropertyChanged));
    public static readonly DependencyProperty IsParentPostProperty = DependencyProperty.Register(nameof(IsParentPost), typeof(bool), typeof(WrappedMediaContentControl), new PropertyMetadata(false, OnDataPropertyChanged));

    public WrappedMediaContentControl() => InitializeComponent();

    public WrappedMediaContentsViewModel ViewModel { get; } = new();

    public List<MediaContent> MediaContents
    {
        get => (List<MediaContent>)GetValue(MediaContentsProperty);
        set => SetValue(MediaContentsProperty, value);
    }

    public List<MediaContent> AllMediaContents
    {
        get => (List<MediaContent>)GetValue(AllMediaContentsProperty);
        set => SetValue(AllMediaContentsProperty, value);
    }

    public PostType PostType
    {
        get => (PostType)GetValue(PostTypeProperty);
        set => SetValue(PostTypeProperty, value);
    }

    public bool IsParentPost
    {
        get => (bool)GetValue(IsParentPostProperty);
        set => SetValue(IsParentPostProperty, value);
    }

    private static void OnDataPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((WrappedMediaContentControl)sender).Rebuild();

    private void Rebuild() => ViewModel.Update(MediaContents, AllMediaContents, PostType, IsParentPost);

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs e) => ViewModel.UpdateViewportWidth(e.NewSize.Width);

    private void OnMediaItemImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is Image image && image.Source is BitmapSource bitmapSource && image.DataContext is MediaContentViewModel viewModel)
        {
            viewModel.ReportImageSize(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
        }
    }
}