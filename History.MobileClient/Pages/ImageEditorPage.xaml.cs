using FFImageLoading;
using History.MobileClient.Helpers;

namespace History.MobileClient.Pages;

public partial class ImageEditorPage : ContentPage
{
    private readonly TaskCompletionSource<byte[]> _taskCompletionSource = new();

    public ImageEditorPage(ImageSource imageSource)
    {
        InitializeComponent();

        ImageEditor.Source = imageSource;
    }

    public Task<byte[]> GetResultAsync() => _taskCompletionSource.Task;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (!_taskCompletionSource.Task.IsCompleted) _taskCompletionSource.TrySetResult(null);
    }

    private async void OnApplyImageTapped(object sender, TappedEventArgs e)
    {
        ImageEditor.SaveEdits();
        var stream = await ImageEditor.GetImageStream();
        var bytes = stream.ToByteArray();
        _taskCompletionSource.TrySetResult(bytes);
        await App.PopModalAsync();
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}