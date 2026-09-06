using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels;

// Local image attachment for the post composer: owns the temp file that will be uploaded,
// the preview thumbnail, the optional description, and the spoiler flag. Dialog requests
// (description edit) and the removal from the list are fulfilled by the owning composer.
public sealed partial class MediaAttachmentViewModel : ObservableObject, IDisposable
{
    private readonly ComposePostWindowViewModel _parent;

    private byte[] _data;

    public string FileName { get; }
    public string FilePath { get; }
    public BitmapImage ThumbnailImageSource { get; }

    // Upload payload: read lazily from the temp file so large images are not buffered in
    // memory for the whole editing session.
    public byte[] Data
    {
        get
        {
            if (_data == null && File.Exists(FilePath)) _data = File.ReadAllBytes(FilePath);
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

    public string DisplayDescription => string.IsNullOrWhiteSpace(Description) ? "설명 추가" : Description;

    public string SpoilerGlyph => IsSpoiler ? "\uED1A" : "\uE7B3";

    public string SpoilerToolTip => IsSpoiler ? "스포일러 해제" : "스포일러로 표시";

    private MediaAttachmentViewModel(ComposePostWindowViewModel parent, string fileName, string filePath, BitmapImage thumbnailImageSource)
    {
        _parent = parent;
        FileName = fileName;
        FilePath = filePath;
        ThumbnailImageSource = thumbnailImageSource;
    }

    // Builds the attachment and its preview thumbnail from the picked image bytes.
    public static async Task<MediaAttachmentViewModel> CreateAsync(ComposePostWindowViewModel parent, string fileName, string filePath, byte[] imageData)
    {
        var thumbnailImageSource = await CreateThumbnailImageSourceAsync(imageData);
        return new MediaAttachmentViewModel(parent, fileName, filePath, thumbnailImageSource);
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
        if (File.Exists(FilePath)) File.Delete(FilePath);
        GC.SuppressFinalize(this);
    }
}