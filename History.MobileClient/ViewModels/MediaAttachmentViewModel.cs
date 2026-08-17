using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using UraniumUI.Icons.MaterialSymbols;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class MediaAttachmentViewModel : ObservableObject, IDisposable
{
    private byte[] _data;

    public byte[] Data
    {
        get
        {
            if (_data == null && IsUpload && FilePath != null && File.Exists(FilePath)) _data = File.ReadAllBytes(FilePath);
            return _data;
        }
        private set { _data = value; }
    }

    public MediaContent ServerContent { get; }
    public string KakaoServerPath { get; }

    public bool IsVideo { get; }
    public bool IsUpload => ServerContent == null && KakaoServerPath == null;
    public bool IsEditImageVisible => !IsVideo && IsUpload;
    public string FileName { get; }

    [ObservableProperty]
    public partial ImageSource Thumbnail { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionEmpty))]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpoilerGlyph))]
    public partial bool IsSpoiler { get; set; }

    public string SpoilerGlyph => IsSpoiler ? MaterialSharp.Visibility_off : MaterialSharp.Visibility;

    public bool IsDescriptionEmpty => string.IsNullOrEmpty(Description);

    public string FilePath { get; private set; }

    public MediaAttachmentViewModel(MediaContent serverContent)
    {
        ServerContent = serverContent;
        IsSpoiler = serverContent.IsSpoiler;

        // Set Description
        Description = serverContent.Description ?? string.Empty;

        // Set Thumbnail
        Thumbnail = ImageSource.FromUri(new(Utils.GenerateMediaUri(serverContent.ThumbnailMediaId)));
    }

    public MediaAttachmentViewModel(Medium media)
    {
        KakaoServerPath = media.media_path;
        IsVideo = media.content_type?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true;

        // Set Thumbnail
        Thumbnail = ImageSource.FromUri(new(media.thumbnail_url ?? media.origin_url ?? media.url));
    }

    public MediaAttachmentViewModel(string fileName, byte[] imageBytes, bool isVideo = false)
    {
        FilePath = Path.GetTempPath() + "_" + fileName;
        Data = imageBytes;
        IsVideo = isVideo;
        FileName = fileName;
        if (!isVideo)
        {
            File.WriteAllBytes(FilePath, imageBytes);
            _ = InitializeThumbnailAsync(imageBytes);
        }
        else Thumbnail = ImageSource.FromFile("video.png");
    }

    public MediaAttachmentViewModel(string fileName, string filePath, bool isVideo = false)
    {
        FileName = fileName;
        FilePath = filePath;
        IsVideo = isVideo;
        if (isVideo) Thumbnail = ImageSource.FromFile("video.png");
        else _ = InitializeThumbnailAsync(filePath);
    }

    private async Task InitializeThumbnailAsync(byte[] imageBytes)
    {
        var thumbnailBytes = await Utils.ResizeImageToThumbnailAsync(imageBytes);
        if (thumbnailBytes != null) Thumbnail = ImageSource.FromStream(() => new MemoryStream(thumbnailBytes));
    }

    private async Task InitializeThumbnailAsync(string filePath)
    {
        var thumbnailBytes = await Utils.ResizeImageToThumbnailAsync(filePath);
        if (thumbnailBytes != null) Thumbnail = ImageSource.FromStream(() => new MemoryStream(thumbnailBytes));
    }

    public async Task ApplyEditAsync(byte[] imageBytes)
    {
        if (IsVideo || !IsUpload) return;

        var fileExtension = Path.GetExtension(FilePath);
        var randomFileName = Path.GetRandomFileName().Replace(".", string.Empty) + fileExtension;
        var newFilePath = Path.Combine(Path.GetTempPath(), randomFileName);

        await Task.Run(() => File.WriteAllBytes(newFilePath, imageBytes));

        if (File.Exists(FilePath)) File.Delete(FilePath);
        Data = imageBytes;
        FilePath = newFilePath;
        var thumbnailBytes = await Utils.ResizeImageToThumbnailAsync(imageBytes);
        if (thumbnailBytes != null) Thumbnail = ImageSource.FromStream(() => new MemoryStream(thumbnailBytes));
    }

    public void Dispose()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    public void ToggleSpoiler()
    {
        IsSpoiler = !IsSpoiler;
    }
}
