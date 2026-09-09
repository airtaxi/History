using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels;

// Local media attachment for the post composer: owns the temp file that will be uploaded,
// the preview thumbnail, the optional description, and the spoiler flag. Videos show a
// thumbnail frame (or the video placeholder image) while the upload payload stays in the
// temp file. Media already stored on the server (post editing) is kept as a server media
// attachment without a local file and is sent back unchanged on submit. Dialog requests
// (description edit) and the removal from the list are fulfilled by the owning composer.
public sealed partial class MediaAttachmentViewModel : ObservableObject, IDisposable
{
    private readonly ComposePostWindowViewModel _parent;

    private readonly MediaContent _serverContent;

    private byte[] _data;

    public string FileName { get; }
    public string FilePath { get; }
    public bool IsVideo { get; }
    public BitmapImage ThumbnailImageSource { get; }

    // The original server media kept during editing; null for local uploads.
    public MediaContent ServerContent => _serverContent;

    public bool IsServerMedia => _serverContent != null;

    // Upload payload: read lazily from the temp file so large images are not buffered in
    // memory for the whole editing session. Server media has no local file to upload.
    public byte[] Data
    {
        get
        {
            if (IsServerMedia || _data != null) return _data;
            if (File.Exists(FilePath)) _data = File.ReadAllBytes(FilePath);
            return _data;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayDescription))]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpoilerGlyph))]
    [NotifyPropertyChangedFor(nameof(SpoilerToolTip))]
    public partial bool IsSpoiler { get; set; }

    // Keeps the kept server media in sync so it is submitted with the edited values.
    partial void OnDescriptionChanged(string value)
    {
        if (_serverContent != null)
        {
            _serverContent.Description = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    partial void OnIsSpoilerChanged(bool value)
    {
        if (_serverContent != null)
        {
            _serverContent.IsSpoiler = value;
        }
    }

    public string DisplayDescription => string.IsNullOrWhiteSpace(Description) ? "설명 추가" : Description;

    public string SpoilerGlyph => IsSpoiler ? "\uED1A" : "\uE7B3";

    public string SpoilerToolTip => IsSpoiler ? "스포일러 해제" : "스포일러로 표시";

    private MediaAttachmentViewModel(ComposePostWindowViewModel parent, string fileName, string filePath, BitmapImage thumbnailImageSource, bool isVideo = false)
    {
        _parent = parent;
        FileName = fileName;
        FilePath = filePath;
        IsVideo = isVideo;
        ThumbnailImageSource = thumbnailImageSource;
    }

    private MediaAttachmentViewModel(ComposePostWindowViewModel parent, MediaContent serverContent, BitmapImage thumbnailImageSource)
    {
        _parent = parent;
        _serverContent = serverContent;
        IsVideo = serverContent.IsVideo;
        ThumbnailImageSource = thumbnailImageSource;
        Description = serverContent.Description ?? string.Empty;
        IsSpoiler = serverContent.IsSpoiler;
    }

    // Builds the attachment for a media item already stored on the server: the thumbnail
    // comes from its server thumbnail and there is no local file, so the content is sent
    // back unchanged on submit.
    public static MediaAttachmentViewModel CreateFromServer(ComposePostWindowViewModel parent, MediaContent serverContent)
    {
        var thumbnailImageSource = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(serverContent.ThumbnailMediaId)));
        return new MediaAttachmentViewModel(parent, serverContent, thumbnailImageSource);
    }

    // Builds the attachment and its preview thumbnail from the picked image bytes.
    public static async Task<MediaAttachmentViewModel> CreateAsync(ComposePostWindowViewModel parent, string fileName, string filePath, byte[] imageData)
    {
        var thumbnailImageSource = await CreateThumbnailImageSourceAsync(imageData);
        return new MediaAttachmentViewModel(parent, fileName, filePath, thumbnailImageSource);
    }

    // Builds the attachment for a video file: the preview shows the extracted first frame,
    // falling back to the video placeholder image when the frame cannot be rendered.
    public static async Task<MediaAttachmentViewModel> CreateVideoAsync(ComposePostWindowViewModel parent, string fileName, string filePath)
    {
        var thumbnailImageSource = await CreateVideoThumbnailImageSourceAsync(filePath);
        return new MediaAttachmentViewModel(parent, fileName, filePath, thumbnailImageSource, isVideo: true);
    }

    private static async Task<BitmapImage> CreateThumbnailImageSourceAsync(byte[] imageData)
    {
        var bitmapImage = new BitmapImage();
        using (var stream = new InMemoryRandomAccessStream())
        {
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                using var dataWriter = new DataWriter(outputStream);
                dataWriter.WriteBytes(imageData);
                await dataWriter.StoreAsync();
                await dataWriter.FlushAsync();
            }
            stream.Seek(0);
            await bitmapImage.SetSourceAsync(stream);
        }
        return bitmapImage;
    }

    private static async Task<BitmapImage> CreateVideoThumbnailImageSourceAsync(string filePath)
    {
        var thumbnailStream = await VideoThumbnailExtractor.GetThumbnailAsync(filePath);
        if (thumbnailStream == null) return new BitmapImage(new Uri("ms-appx:///Assets/App/Video.png"));

        using var stream = thumbnailStream;
        var bitmapImage = new BitmapImage();
        await bitmapImage.SetSourceAsync(stream);
        return bitmapImage;
    }

    // Marks the attachment as a spoiler so it is blurred/hidden until the viewer reveals it.
    [RelayCommand]
    private void ToggleSpoiler() => IsSpoiler = !IsSpoiler;

    // Edits the attachment description; an empty result clears it.
    [RelayCommand]
    private async Task EditDescriptionAsync()
    {
        var result = await _parent.ShowInputDialogAsync(new InputDialogParameters("설명 입력", "이 미디어에 대한 설명을 입력해주세요.", "이 미디어에 대한 설명 입력", showCancel: true, defaultText: Description, maxLength: CommonConstants.MaxMediaDescriptionLength));
        Description = result?.Trim() ?? string.Empty;
    }

    // Removes the attachment from the composer (the temp file is deleted by the parent).
    [RelayCommand]
    private void Delete() => _parent.RemoveAttachment(this);

    public void Dispose()
    {
        if (!IsServerMedia && File.Exists(FilePath)) File.Delete(FilePath);
        GC.SuppressFinalize(this);
    }
}