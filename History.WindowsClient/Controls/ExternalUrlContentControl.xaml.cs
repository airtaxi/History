using History.Commons.DataTypes.Contents;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.TimeLineData;

namespace History.WindowsClient.Controls;

public sealed partial class ExternalUrlContentControl : UserControl
{
    public static readonly DependencyProperty ExternalUrlContentProperty = DependencyProperty.Register(nameof(ExternalUrlContent), typeof(ExternalUrlContent), typeof(ExternalUrlContentControl), new PropertyMetadata(null, OnDataPropertyChanged));
    public static readonly DependencyProperty ScrapProperty = DependencyProperty.Register(nameof(Scrap), typeof(Scrap), typeof(ExternalUrlContentControl), new PropertyMetadata(null, OnDataPropertyChanged));

    public ExternalUrlContentControl() => InitializeComponent();

    public ExternalUrlContentViewModel ViewModel { get; } = new();

    public ExternalUrlContent ExternalUrlContent
    {
        get => (ExternalUrlContent)GetValue(ExternalUrlContentProperty);
        set => SetValue(ExternalUrlContentProperty, value);
    }

    public Scrap Scrap
    {
        get => (Scrap)GetValue(ScrapProperty);
        set => SetValue(ScrapProperty, value);
    }

    private static void OnDataPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((ExternalUrlContentControl)sender).Rebuild();

    private void Rebuild()
    {
        if (Scrap != null) ViewModel.Update(Scrap);
        else ViewModel.Update(ExternalUrlContent);
    }

    private void OnOpenMenuItemClicked(object sender, RoutedEventArgs e) => ViewModel.OpenLinkCommand.Execute(null);

    private void OnCopyMenuItemClicked(object sender, RoutedEventArgs e) => ViewModel.CopyLinkCommand.Execute(null);
}