using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using NativeMedia;

namespace History.MobileClient.Pages;

public partial class FullScreenMediaViewerPage : ContentPage
{
    private bool _isInForeground;
    private readonly FullScreenMediaContentViewModel _viewModel;

	public FullScreenMediaViewerPage(FullScreenMediaContentViewModel viewModel)
	{
        _viewModel = viewModel;

        InitializeComponent();
        MainDataTemplatePresenter.ViewModel = _viewModel;

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnDownloadImageTapped(object sender, TappedEventArgs e)
    {
        var status = await Permissions.RequestAsync<SaveMediaPermission>();
        if (status != PermissionStatus.Granted) return;

        IsEnabled = false;
        IsBusy = true;

        var viewModel = _viewModel.CurrentMedia;
        var isImage = viewModel is ImageViewModel;
        var fileName = isImage ? $"{viewModel.Uri.GetHashCode()}.webp" : $"{viewModel.Uri.GetHashCode()}.mp4";

        var tempPath = Path.GetTempPath();
        var filePath = Path.Combine(tempPath, fileName);

        try
        {
            await Downloader.DownloadFileAsync(viewModel.Uri, filePath);
            await MediaGallery.SaveAsync(viewModel is ImageViewModel ? MediaFileType.Image : MediaFileType.Video, filePath);
        }
        catch
        {
            await DisplayAlert("오류", "미디어 파일 저장 중 오류가 발생하였습니다.", Constants.PromptOk);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            IsEnabled = true;
            IsBusy = false;
            await Toast.Make("미디어 파일이 저장되었습니다.").Show();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

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
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopAsync();
        return true;
    }
}