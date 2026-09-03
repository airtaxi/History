using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels.Media;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.ViewModels;

// Full-screen media viewer state for MediaWindow, mirroring the MAUI
// FullScreenMediaContentViewModel and the FullScreenMediaViewerPage download flows.
// Owns the full-screen media items (original resolution, Uniform stretch) and drives
// the single/all download flows through the BaseViewModel dialog and picker events,
// which the window code-behind fulfills.
public sealed partial class MediaWindowViewModel : BaseViewModel
{
    private const string DownloadAllText = "전체 다운로드";
    private const string DownloadImagesOnlyText = "사진만 다운로드";
    private const string DownloadVideosOnlyText = "동영상만 다운로드";

    private int _previousIndex = -1;

    public MediaWindowViewModel(List<MediaContent> mediaContents, PostType postType, bool isParentPost, int initialIndex)
    {
        Medias = [.. mediaContents.Select(mediaContent => new MediaContentViewModel(mediaContent, mediaContents, postType, isParentPost, true))];
        SelectedIndex = Math.Clamp(initialIndex, 0, Math.Max(Medias.Count - 1, 0));
    }

    public List<MediaContentViewModel> Medias { get; }

    public bool HasMultipleMedias => Medias.Count > 1;

    public Visibility PositionBadgeVisiability => HasMultipleMedias ? Visibility.Visible : Visibility.Collapsed;

    public string PositionText => $"{SelectedIndex + 1} / {Medias.Count}";

    // Zoom controls only apply to images; videos display fit-to-viewport without zoom.
    public bool IsZoomVisible => Medias.Count > 0 && !Medias[Math.Clamp(SelectedIndex, 0, Medias.Count - 1)].IsVideo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText))]
    [NotifyPropertyChangedFor(nameof(IsZoomVisible))]
    public partial int SelectedIndex { get; set; }

    // Stops the previously selected media (video playback, spoiler overlays) when the
    // viewer moves to another item, mirroring WrappedMediaContentsViewModel.
    partial void OnSelectedIndexChanged(int value)
    {
        if (Medias.Count == 0) return;

        if (_previousIndex >= 0 && _previousIndex < Medias.Count && _previousIndex != value) Medias[_previousIndex].ResetForReuse();
        _previousIndex = value;
    }

    // Saves the currently selected media through the file save picker. The picker runs
    // first so a cancelled save never downloads the file.
    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (Medias.Count == 0) return;

        var media = Medias[Math.Clamp(SelectedIndex, 0, Medias.Count - 1)];
        var extension = media.IsVideo ? ".mp4" : ".webp";
        var mediaId = media.MediaContent.MediaId;

        var saveResult = await SaveFileAsync(new FileSavePickerParameters(new Dictionary<string, IReadOnlyList<string>> { [extension.TrimStart('.').ToUpperInvariant()] = [extension] }, $"{mediaId}{extension}", extension, PickerLocationId.PicturesLibrary));
        if (saveResult == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
            try
            {
                await DownloadFileAsync(CommonUtils.GenerateMediaUri(mediaId), tempPath);
                File.Copy(tempPath, saveResult.Path, true);
            }
            catch { await ShowMessageDialogAsync(new MessageDialogParameters("오류", "미디어 파일 저장 중 오류가 발생하였습니다.")); }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        });
    }

    // Saves all media (or only images/videos when the post mixes both) into a folder the
    // user picks once. Files are named 1.webp, 2.mp4... in post order; failures are counted
    // and reported instead of aborting the batch.
    [RelayCommand]
    private async Task DownloadAllAsync()
    {
        if (Medias.Count == 0) return;

        var targets = Medias;
        var hasImages = Medias.Any(x => !x.IsVideo);
        var hasVideos = Medias.Any(x => x.IsVideo);
        if (hasImages && hasVideos)
        {
            var selection = await ShowSelectionDialogAsync("다운로드 옵션", [DownloadAllText, DownloadImagesOnlyText, DownloadVideosOnlyText]);
            if (selection == null) return;

            if (selection == DownloadImagesOnlyText) targets = Medias.Where(x => !x.IsVideo).ToList();
            else if (selection == DownloadVideosOnlyText) targets = Medias.Where(x => x.IsVideo).ToList();
        }

        var folderResult = await PickFolderAsync(new FolderPickerParameters(PickerLocationId.PicturesLibrary));
        if (folderResult == null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var failedCount = 0;
            var tempPath = Path.GetTempPath();
            var downloadItems = targets.Select((media, index) =>
            {
                var extension = media.IsVideo ? ".mp4" : ".webp";
                return (Uri: CommonUtils.GenerateMediaUri(media.MediaContent.MediaId), TempFilePath: Path.Combine(tempPath, $"{Guid.NewGuid():N}{extension}"), TargetFilePath: Path.Combine(folderResult.Path, $"{index + 1}{extension}"));
            }).ToList();

            try
            {
                await DownloadFilesAsync(downloadItems.Select(item => (item.Uri, item.TempFilePath)));
                foreach (var item in downloadItems)
                {
                    try { File.Copy(item.TempFilePath, item.TargetFilePath, true); }
                    catch { failedCount++; }
                    finally
                    {
                        if (File.Exists(item.TempFilePath))
                        {
                            File.Delete(item.TempFilePath);
                        }
                    }
                }
            }
            catch { failedCount = targets.Count; }
            finally
            {
                foreach (var item in downloadItems)
                {
                    if (File.Exists(item.TempFilePath))
                    {
                        File.Delete(item.TempFilePath);
                    }
                }
            }

            if (failedCount > 0) await ShowMessageDialogAsync(new MessageDialogParameters("오류", $"{targets.Count}개 중 {failedCount}개의 미디어 파일 저장에 실패하였습니다."));
        });
    }

    private static async Task DownloadFileAsync(string requestUri, string destinationPath)
    {
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var fileStream = File.Create(destinationPath);
        await response.Content.CopyToAsync(fileStream);
    }

    private static async Task DownloadFilesAsync(IEnumerable<(string Uri, string FilePath)> items) => await Task.WhenAll(items.Select(item => DownloadFileAsync(item.Uri, item.FilePath)));
}