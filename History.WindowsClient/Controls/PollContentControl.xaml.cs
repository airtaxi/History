using History.Commons.DataTypes.Contents;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace History.WindowsClient.Controls;

public sealed partial class PollContentControl : BaseControl
{
    public static readonly DependencyProperty PollContentProperty = DependencyProperty.Register(nameof(PollContent), typeof(PollContent), typeof(PollContentControl), new PropertyMetadata(null, OnDataPropertyChanged));
    public static readonly DependencyProperty PostIdProperty = DependencyProperty.Register(nameof(PostId), typeof(string), typeof(PollContentControl), new PropertyMetadata(null, OnDataPropertyChanged));

    public PollContentControl() => InitializeComponent();

    public override PollContentViewModel ViewModel { get; } = new();

    public PollContent PollContent
    {
        get => (PollContent)GetValue(PollContentProperty);
        set => SetValue(PollContentProperty, value);
    }

    public string PostId
    {
        get => (string)GetValue(PostIdProperty);
        set => SetValue(PostIdProperty, value);
    }

    private static void OnDataPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((PollContentControl)sender).Rebuild();

    private void Rebuild() => ViewModel.Update(PollContent, PostId);

    // Prevent event bubbling to the parent control when the user clicks on the content, so that it doesn't trigger any unintended actions.
    private void OnPointerPressed(object sender, PointerRoutedEventArgs e) => e.Handled = true;
}