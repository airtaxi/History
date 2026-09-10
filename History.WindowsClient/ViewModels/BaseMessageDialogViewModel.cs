using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Message;
using History.Commons.DataTypes.Contents;
using History.Commons;
using System.IO;

namespace History.WindowsClient.ViewModels;

// Shared message-compose surface for the write and reply dialogs: the text state, the
// 100-character limit, and the send pipeline. The receiver id and any pre-send
// validation are supplied by the derived dialog.
public abstract partial class BaseMessageDialogViewModel : ImageAttachmentViewModel
{
    protected BaseMessageDialogViewModel(BaseViewModel baseViewModel) => BaseViewModel = baseViewModel;

    public BaseViewModel BaseViewModel { get; }

    protected abstract string ReceiverId { get; }

    // Whether the account mode accepts an image attachment on a mail.
    public virtual bool IsAttachmentAvailable => true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextLengthText))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    public partial string Text { get; set; } = string.Empty;

    public string TextLengthText => $"{Text?.Length ?? 0} / 100자";

    public virtual bool CanSend => !string.IsNullOrWhiteSpace(Text) && (Text?.Length ?? 0) <= 100 && !IsSending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    public partial bool IsSending { get; set; }

    public bool IsSent { get; private set; }

    // Rejects invalid states before the send pipeline runs (e.g. a write dialog without
    // a selected receiver).
    protected virtual Task<bool> ValidateAsync() => Task.FromResult(true);

    [RelayCommand]
    public async Task HandleMediaTapAsync()
    {
        var result = await BaseViewModel.PickImageAsync();
        if (result == null) return;

        var fileName = Path.GetFileName(result.Path);
        var imageData = await File.ReadAllBytesAsync(result.Path);
        await ApplyAttachmentAsync(fileName, imageData);
    }

    public async Task<bool> SendAsync()
    {
        if (!await ValidateAsync()) return false;

        var text = Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, "쪽지 내용을 입력하세요."));
            return false;
        }

        if (text.Length > 100)
        {
            await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, "쪽지는 100자 이내로 작성해야 합니다."));
            return false;
        }

        IsSending = true;
        try
        {
            var result = await SendContentsAsync(text);
            if (result.IsSuccess)
            {
                IsSent = true;
                await BaseViewModel.ShowMessageDialogAsync(new("성공", "쪽지가 전송되었습니다."));
                return true;
            }

            return false;
        }
        finally { IsSending = false; }
    }

    // Sends the composed mail through the History message API. Account-mode dialogs
    // override this to send through their own mail API.
    protected virtual async Task<Result> SendContentsAsync(string text)
    {
        var contents = new List<BaseContent> { new TextContent { Text = text } };
        var files = new Dictionary<string, byte[]>();

        if (HasAttachment && AttachmentData != null && !string.IsNullOrEmpty(AttachmentFileName))
        {
            contents.Add(new UploadContent { FileName = AttachmentFileName });
            files[AttachmentFileName] = AttachmentData;
        }

        return await BaseViewModel.ExecuteRequestAsync(new SendMessage(ReceiverId, contents, files));
    }
}
