using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public partial class MediaAttachmentViewModel : ObservableObject, IDisposable
{
    public byte[] Data { get; private set; }
    public MediaContent ServerContent { get; }

    public bool IsVideo { get; }
    public bool IsUpload => ServerContent == null;
    public bool IsEditImageVisible => !IsVideo && IsUpload;
    public string FileName { get; }

    [ObservableProperty]
    public partial ImageSource ImageSource { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionEmpty))]
    public partial string Description { get; set; } = string.Empty;

    public bool IsDescriptionEmpty => string.IsNullOrEmpty(Description);

    public string FilePath { get; private set; }

    public MediaAttachmentViewModel(MediaContent serverContent)
    {
        ServerContent = serverContent;

        // Set Description
        Description = serverContent.Description ?? string.Empty;

        // Set ImageSource
        ImageSource = ImageSource.FromUri(new(Utils.GenerateMediaUri(serverContent.ThumbnailMediaId)));
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
}
