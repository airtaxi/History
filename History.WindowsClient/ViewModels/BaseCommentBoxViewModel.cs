using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels;

// Base comment box view model shared by History and (future) Kakao Story comment composing.
// Holds the attachment surface and command contracts; derived types implement the actual sending.
// The CommentSent event follows the BaseViewModel.MessageDialogRequested pattern: the view model
// requests UI work and the host page fulfills it (clear editor, focus, scroll to newest).
public abstract partial class BaseCommentBoxViewModel(BaseViewModel baseViewModel) : ObservableObject
{
    // Base view model used for dialog requests, mirroring the post view model pattern.
    protected readonly BaseViewModel BaseViewModel = baseViewModel;

    // Attachment surface shared with the composer bar.
    [ObservableProperty]
    public partial BitmapImage AttachmentImageSource { get; protected set; }
    [ObservableProperty]
    public partial bool HasAttachment { get; protected set; }

    protected byte[] AttachmentData { get; private set; }
    protected string AttachmentFileName { get; private set; }

    public event EventHandler CommentSent;
    public event EventHandler<StickerContent> StickerSelected;

    // Image-only extensions for the comment attachment picker: ((a)png, jp(e)g, webp, gif, tiff).
    private static readonly string[] s_commentImageFileTypeFilters = [".png", ".apng", ".jpg", ".jpeg", ".webp", ".gif", ".tif", ".tiff"];

    // Opens the sticker picker dialog and surfaces the chosen sticker through the
    // StickerSelected event so the host page can insert it into the comment editor.
    [RelayCommand]
    public virtual async Task HandleStickerTapAsync()
    {
        var dialog = new StickerPickerDialog(new StickerPickerViewModel(BaseViewModel));
        await BaseViewModel.ShowContentDialogAsync(dialog);
        if (dialog.SelectedStickerContent != null) StickerSelected?.Invoke(this, dialog.SelectedStickerContent);
    }

    // Opens the image picker through the base view model and applies the single selection
    // as the comment attachment.
    [RelayCommand]
    public virtual async Task HandleMediaTapAsync()
    {
        var result = await BaseViewModel.PickFileAsync(new FileOpenPickerParameters(s_commentImageFileTypeFilters, PickerLocationId.PicturesLibrary, "이미지 추가"));
        if (result == null) return;

        var fileName = Path.GetFileName(result.Path);
        var imageData = await File.ReadAllBytesAsync(result.Path);
        await ApplyAttachmentAsync(fileName, imageData);
    }

    // Sends the comment with the given editor contents. Platform-specific.
    public abstract Task SendCommentAsync(List<BaseContent> contents);

    // Clears the current attachment data and preview (bound to the attachment preview tap).
    // The generated ClearAttachmentCommand stays public even for a protected method.
    [RelayCommand]
    protected void ClearAttachment()
    {
        AttachmentData = null;
        AttachmentFileName = null;
        AttachmentImageSource = null;
        HasAttachment = false;
    }

    // Applies an image pasted into the editor as the current attachment.
    public async Task ApplyAttachmentAsync(string fileName, byte[] imageData)
    {
        ClearAttachment();

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

        AttachmentImageSource = bitmapImage;
        AttachmentFileName = fileName;
        AttachmentData = imageData;
        HasAttachment = true;
    }

    // Drops empty text contents so whitespace-only drafts count as empty.
    protected static void RemoveEmptyTextContents(List<BaseContent> contents) => contents.RemoveAll(x => x is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text));

    protected void RaiseCommentSent() => CommentSent?.Invoke(this, EventArgs.Empty);
}