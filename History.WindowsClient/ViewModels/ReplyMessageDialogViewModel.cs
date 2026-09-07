using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Message;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using System.IO;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels;

public partial class ReplyMessageDialogViewModel(BaseViewModel baseViewModel, string receiverId, string receiverName, ImageSource receiverProfileImage = null) : ObservableObject
{
    private static readonly string[] s_imageFileTypeFilters = [".png", ".apng", ".jpg", ".jpeg", ".webp", ".gif", ".tif", ".tiff"];

    public BaseViewModel BaseViewModel { get; } = baseViewModel;
    public string ReceiverId { get; } = receiverId;
    public string ReceiverName { get; } = receiverName;
    public ImageSource ReceiverProfileImage { get; } = receiverProfileImage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextLengthText))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    public partial string Text { get; set; } = string.Empty;

    public string TextLengthText => $"{Text?.Length ?? 0} / 100자";

    public bool CanSend => !string.IsNullOrWhiteSpace(Text) && (Text?.Length ?? 0) <= 100 && !IsSending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AttachmentVisibility))]
    public partial bool HasAttachment { get; set; }

    public bool AttachmentVisibility => HasAttachment;

    [ObservableProperty]
    public partial BitmapImage AttachmentImageSource { get; set; }

    public byte[] AttachmentData { get; private set; }
    public string AttachmentFileName { get; private set; }

    [RelayCommand]
    public async Task HandleMediaTapAsync()
    {
        var result = await BaseViewModel.PickFileAsync(new FileOpenPickerParameters(s_imageFileTypeFilters, PickerLocationId.PicturesLibrary, "이미지 추가"));
        if (result == null) return;

        var fileName = Path.GetFileName(result.Path);
        var imageData = await File.ReadAllBytesAsync(result.Path);
        await ApplyAttachmentAsync(fileName, imageData);
    }

    [RelayCommand]
    public void ClearAttachment()
    {
        AttachmentData = null;
        AttachmentFileName = null;
        AttachmentImageSource = null;
        HasAttachment = false;
    }

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

    public async Task<bool> SendAsync()
    {
        var text = Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await BaseViewModel.ShowMessageDialogAsync(new("오류", "쪽지 내용을 입력하세요."));
            return false;
        }

        if (text.Length > 100)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("오류", "쪽지는 100자 이내로 작성해야 합니다."));
            return false;
        }

        IsSending = true;
        try
        {
            var contents = new List<BaseContent> { new TextContent { Text = text } };
            var files = new Dictionary<string, byte[]>();

            if (HasAttachment && AttachmentData != null && !string.IsNullOrEmpty(AttachmentFileName))
            {
                contents.Add(new UploadContent { FileName = AttachmentFileName });
                files[AttachmentFileName] = AttachmentData;
            }

            var result = await BaseViewModel.ExecuteRequestAsync(new SendMessage(ReceiverId, contents, files));
            if (result.IsSuccess)
            {
                await BaseViewModel.ShowMessageDialogAsync(new("성공", "쪽지가 전송되었습니다."));
                return true;
            }

            return false;
        }
        finally { IsSending = false; }
    }
}
