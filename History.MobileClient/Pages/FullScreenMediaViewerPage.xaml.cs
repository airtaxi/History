using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using History.MobileClient.ViewModels;
using NativeMedia;

namespace History.MobileClient.Pages;

public partial class FullScreenMediaViewerPage : ContentPage
{
    private IMediaViewModel _viewModel;
	public FullScreenMediaViewerPage(IMediaViewModel viewModel)
	{
        _viewModel = viewModel;

        InitializeComponent();
        MainDataTemplatePresenter.ViewModel = _viewModel;
    }

    private async void OnBackImageTouchGestureCompleted(object sender, TouchGestureCompletedEventArgs e) => await App.PopModalAsync();

    private async void OnDownloadImageTouchGestureCompleted(object sender, CommunityToolkit.Maui.Core.TouchGestureCompletedEventArgs e)
    {
        var status = await Permissions.RequestAsync<SaveMediaPermission>();
        if (status != PermissionStatus.Granted) return;

        IsEnabled = false;
        IsBusy = true;
        var tempPath = Path.GetTempPath();
        var isImage = _viewModel is ImageViewModel;
        var fileName = isImage ? $"{_viewModel.Uri.GetHashCode()}.webp" : $"{_viewModel.Uri.GetHashCode()}.mp4";
        var filePath = Path.Combine(tempPath, fileName);
        try
        {
            await Downloader.DownloadFileAsync(_viewModel.Uri, filePath);
            await MediaGallery.SaveAsync(_viewModel is ImageViewModel ? MediaFileType.Image : MediaFileType.Video, filePath);
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
}