using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using UraniumUI.Icons.MaterialSymbols;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class MediaAttachmentViewModel : ObservableObject, IDisposable
{
    public byte[] Data { get; private set; }
    public MediaContent ServerContent { get; }
    public string KakaoServerPath { get; }

    public bool IsVideo { get; }
    public bool IsUpload => ServerContent == null && KakaoServerPath == null;
    public bool IsEditImageVisible => !IsVideo && IsUpload;
    public string FileName { get; }

    [ObservableProperty]
    public partial ImageSource ImageSource { get; private set; }

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

        // Set ImageSource
        ImageSource = ImageSource.FromUri(new(Utils.GenerateMediaUri(serverContent.ThumbnailMediaId)));
    }

    public MediaAttachmentViewModel(Medium media)
    {
        KakaoServerPath = media.media_path;
        IsVideo = media.content_type?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true;

        // Set ImageSource
        ImageSource = ImageSource.FromUri(new(media.thumbnail_url ?? media.origin_url ?? media.url));
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
            ImageSource = ImageSource.FromFile(FilePath);
        }
        else ImageSource = ImageSource.FromFile("video.png");
    }

    public void ApplyEdit(byte[] imageBytes)
    {
        if (IsVideo || !IsUpload) return;

        if (File.Exists(FilePath)) File.Delete(FilePath);

        Data = imageBytes;

        var fileExtension = Path.GetExtension(FilePath);
        var randomFileName = Path.GetRandomFileName().Replace(".", string.Empty) + fileExtension;
        FilePath = Path.Combine(Path.GetTempPath(), randomFileName);

        File.WriteAllBytes(FilePath, imageBytes);
        ImageSource = ImageSource.FromFile(FilePath);
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
