using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Dialogs;

namespace History.WindowsClient.ViewModels;

// Base comment box view model shared by History and (future) Kakao Story comment composing.
// Holds the attachment surface and command contracts; derived types implement the actual sending.
// The CommentSent event follows the BaseViewModel.MessageDialogRequested pattern: the view model
// requests UI work and the host page fulfills it (clear editor, focus, scroll to newest).
public abstract partial class BaseCommentBoxViewModel(BaseViewModel baseViewModel) : ImageAttachmentViewModel
{
    // Base view model used for dialog requests, mirroring the post view model pattern.
    protected readonly BaseViewModel BaseViewModel = baseViewModel;

    public event EventHandler CommentSent;
    public event EventHandler<StickerContent> StickerSelected;

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
        var result = await BaseViewModel.PickImageAsync();
        if (result == null) return;

        var fileName = Path.GetFileName(result.Path);
        var imageData = await File.ReadAllBytesAsync(result.Path);
        await ApplyAttachmentAsync(fileName, imageData);
    }

    // Sends the comment with the given editor contents. Platform-specific.
    public abstract Task SendCommentAsync(List<BaseContent> contents);

    // Drops empty text contents so whitespace-only drafts count as empty.
    protected static void RemoveEmptyTextContents(List<BaseContent> contents) => contents.RemoveAll(x => x is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text));

    protected void RaiseCommentSent() => CommentSent?.Invoke(this, EventArgs.Empty);
}