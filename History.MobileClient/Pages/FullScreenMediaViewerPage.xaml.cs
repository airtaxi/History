using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.Behaviors;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using NativeMedia;
using UraniumUI.Extensions;

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
        WeakReferenceMessenger.Default.Register<FullScreenMediaTappedMessage>(this, OnFullScreenMediaTappedMessageReceived);

        if (_viewModel.FullScreenMedias.Count >= 2) DownloadAllImage.IsVisible = true;
#if IOS
        var carouselView = MainDataTemplatePresenter.FindInChildrenHierarchy<CarouselView>();
        if (carouselView != null) carouselView.Behaviors.Add(new SwipeToCloseBehavior());
        else MainDataTemplatePresenter.Behaviors.Add(new SwipeToCloseBehavior());
#endif
    }

    private void OnFullScreenMediaTappedMessageReceived(object recipient, FullScreenMediaTappedMessage message) => TopBarGrid.IsVisible = !TopBarGrid.IsVisible;

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnDownloadImageTapped(object sender, TappedEventArgs e)
    {
        var status = await Permissions.RequestAsync<SaveMediaPermission>();
        if (status != PermissionStatus.Granted) return;

        IsEnabled = false;
        MainActivityIndicator.IsRunning = true;

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
            await DisplayAlertAsync("오류", "미디어 파일 저장 중 오류가 발생하였습니다.", Constants.PromptOk);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            IsEnabled = true;
            MainActivityIndicator.IsRunning = false;
            await Toast.Make("미디어 파일이 저장되었습니다.").Show();
        }
    }

    private async void OnDownloadAllImageTapped(object sender, TappedEventArgs e)
    {
        var status = await Permissions.RequestAsync<SaveMediaPermission>();
        if (status != PermissionStatus.Granted) return;

        var allMedias = _viewModel.FullScreenMedias;
        var hasImages = allMedias.Any(x => x is ImageViewModel);
        var hasVideos = allMedias.Any(x => x is VideoViewModel);

        List<IMediaViewModel> targets;
        if (hasImages && hasVideos)
        {
            const string downloadAll = "전체 다운로드";
            const string downloadImagesOnly = "사진만 다운로드";
            const string downloadVideosOnly = "동영상만 다운로드";

            var action = await DisplayActionSheetAsync("다운로드 옵션", Constants.PromptCancel, null, downloadAll, downloadImagesOnly, downloadVideosOnly);
            if (action == null || action == Constants.PromptCancel) return;

            if (action == downloadImagesOnly) targets = [.. allMedias.Where(x => x is ImageViewModel)];
            else if (action == downloadVideosOnly) targets = [.. allMedias.Where(x => x is VideoViewModel)];
            else targets = [.. allMedias];
        }
        else targets = [.. allMedias];

        IsEnabled = false;
        MainActivityIndicator.IsRunning = true;

        var failedCount = 0;
        var tempPath = Path.GetTempPath();
        var downloadItems = targets.Select(media =>
        {
            var isImage = media is ImageViewModel;
            var fileName = isImage ? $"{media.Uri.GetHashCode()}.webp" : $"{media.Uri.GetHashCode()}.mp4";
            return (Media: media, IsImage: isImage, FilePath: Path.Combine(tempPath, fileName));
        }).ToList();

        try
        {
            await ParallelDownloader.DownloadFilesAsync(downloadItems.Select(item => (item.Media.Uri, item.FilePath)));

            foreach (var (media, isImage, filePath) in downloadItems.AsEnumerable().Reverse())
            {
                try { await MediaGallery.SaveAsync(isImage ? MediaFileType.Image : MediaFileType.Video, filePath); }
                catch { failedCount++; }
                finally
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }
        finally
        {
            IsEnabled = true;
            MainActivityIndicator.IsRunning = false;
        }

        if (failedCount > 0) await DisplayAlertAsync("오류", $"{targets.Count}개 중 {failedCount}개의 미디어 파일 저장에 실패하였습니다.", Constants.PromptOk);
        else await Toast.Make($"{targets.Count}개의 미디어 파일이 저장되었습니다.").Show();
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

        WeakReferenceMessenger.Default.Send(new FullScreenPageNavigationMessage(this, false));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;

        WeakReferenceMessenger.Default.Send(new FullScreenPageNavigationMessage(this, true));
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
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