using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels;

// Shared single-image attachment surface for editors that hold at most one image
// (comment box, message write/reply dialogs): the preview source, visibility, and the
// decode-and-apply flow for pasted or picked image bytes.
public abstract partial class ImageAttachmentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial BitmapImage AttachmentImageSource { get; protected set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AttachmentVisibility))]
    public partial bool HasAttachment { get; protected set; }

    public bool AttachmentVisibility => HasAttachment;

    protected byte[] AttachmentData { get; private set; }
    protected string AttachmentFileName { get; private set; }

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

    // Applies an image pasted into the editor or picked from the picker as the current attachment.
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
}
