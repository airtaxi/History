using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.ViewModels;

// Compose post window state. This is a UI shell: every writing-related action (media
// attachment, poll, kakao cross-post, reservation, submit) is a stub to be filled in later.
public sealed partial class ComposePostWindowViewModel : BaseViewModel
{
    // Image-only extensions for the media picker, shared with the comment attachment flow.
    private static readonly string[] s_imageFileTypeFilters = [".png", ".apng", ".jpg", ".jpeg", ".webp", ".gif", ".tif", ".tiff"];

    private const int CommentPermissionNotSetSelectedIndex = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDiscoveryOptionDisplayText))]
    [NotifyPropertyChangedFor(nameof(SelectedDiscoveryOptionGlyph))]
    [NotifyPropertyChangedFor(nameof(SelectedDiscoveryIndex))]
    public partial ComposePostDiscoveryOptionItemViewModel SelectedDiscoveryOptionItem { get; set; }

    public DiscoveryOption SelectedDiscoveryOption => SelectedDiscoveryOptionItem?.Option ?? DiscoveryOption.FriendsOfFriends;

    [ObservableProperty]
    public partial bool IsShareRepostDisallowed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCommentPermissionDisplayText))]
    [NotifyPropertyChangedFor(nameof(SelectedCommentPermissionGlyph))]
    public partial ComposePostCommentPermissionItemViewModel SelectedCommentPermissionItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReservationScheduled))]
    [NotifyPropertyChangedFor(nameof(ReservationButtonContent))]
    public partial DateTime? ReservationTime { get; set; }

    // Kakao cross-post toggle. The login flow is not wired up yet; the toggle itself only
    // reflects the current stub state until the kakao game posting is implemented.
    [ObservableProperty]
    public partial bool IsKakaoPostEnabled { get; set; }

    public ObservableCollection<ComposePostDiscoveryOptionItemViewModel> DiscoveryOptionItems { get; } = [.. Enum.GetValues<DiscoveryOption>().OrderBy(x => (int)x).Select(option => new ComposePostDiscoveryOptionItemViewModel(option))];

    public ObservableCollection<ComposePostCommentPermissionItemViewModel> CommentPermissionItems { get; } = [.. Enum.GetValues<AccessPermission>().OrderBy(x => (int)x).Select(permission => new ComposePostCommentPermissionItemViewModel(permission)).Prepend(new ComposePostCommentPermissionItemViewModel(null))];

    public string SelectedDiscoveryOptionDisplayText => SelectedDiscoveryOptionItem?.DisplayText ?? SelectedDiscoveryOption.ToDisplayString();

    public string SelectedDiscoveryOptionGlyph => PostHelper.GetDiscoveryOptionGlyph(SelectedDiscoveryOption);

    public int SelectedDiscoveryIndex
    {
        get => SelectedDiscoveryOptionItem == null ? 0 : DiscoveryOptionItems.IndexOf(SelectedDiscoveryOptionItem);
        set
        {
            if (value < 0 || value >= DiscoveryOptionItems.Count) return;
            SelectedDiscoveryOptionItem = DiscoveryOptionItems[value];
        }
    }

    public string SelectedCommentPermissionDisplayText => SelectedCommentPermissionItem?.DisplayText ?? CommentPermissionItems[CommentPermissionNotSetSelectedIndex].DisplayText;

    public string SelectedCommentPermissionGlyph => SelectedCommentPermissionItem?.Glyph ?? CommentPermissionItems[CommentPermissionNotSetSelectedIndex].Glyph;

    public bool IsReservationScheduled => ReservationTime.HasValue;

    public string ReservationButtonContent => ReservationTime.HasValue ? "예약됨" : "게시 예약";

    public event EventHandler<StickerContent> StickerSelected;
    public event EventHandler<string> MediaFileSelected;

    public ComposePostWindowViewModel()
    {
        SelectedDiscoveryOptionItem = DiscoveryOptionItems.FirstOrDefault(x => x.Option == SelectedDiscoveryOption) ?? DiscoveryOptionItems[0];
        SelectedCommentPermissionItem = CommentPermissionItems[CommentPermissionNotSetSelectedIndex];
    }

    partial void OnSelectedDiscoveryOptionItemChanged(ComposePostDiscoveryOptionItemViewModel value)
    {
        if (value == null) return;

        foreach (var item in CommentPermissionItems) item.UpdateAvailability(value.Option);

        // A disabled selection cannot survive a scope narrowing: fall back to the not-set entry.
        if (SelectedCommentPermissionItem != null && !SelectedCommentPermissionItem.IsEnabled) SelectedCommentPermissionItem = CommentPermissionItems[CommentPermissionNotSetSelectedIndex];
    }

    partial void OnSelectedCommentPermissionItemChanged(ComposePostCommentPermissionItemViewModel value)
    {
        if (value == null || value.IsEnabled) return;

        // Ignore disabled picks so the selection cannot land on an unavailable permission.
        SelectedCommentPermissionItem = CommentPermissionItems[CommentPermissionNotSetSelectedIndex];
    }

    // Opens the sticker picker dialog and surfaces the chosen sticker to the window so it
    // can be inserted into the editor.
    [RelayCommand]
    private async Task HandleStickerTapAsync()
    {
        var dialog = new StickerPickerDialog(new StickerPickerViewModel(this));
        await ShowContentDialogAsync(dialog);
        if (dialog.SelectedStickerContent != null) StickerSelected?.Invoke(this, dialog.SelectedStickerContent);
    }

    // Picks a single image attachment. Applying the attachment to the post contents is a
    // TODO: the pick itself is real so the dialog flow can be exercised from the shell.
    [RelayCommand]
    private async Task HandleMediaTapAsync()
    {
        var result = await PickFileAsync(new FileOpenPickerParameters(s_imageFileTypeFilters, PickerLocationId.PicturesLibrary, "이미지 추가"));
        if (result == null) return;

        // TODO: apply the selected image to the post attachments and render the preview surface.
        MediaFileSelected?.Invoke(this, result.Path);
    }

    // Asks for a URL to attach. Embedding the hyperlink into the editor contents is a TODO.
    [RelayCommand]
    private async Task HandleUrlTapAsync()
    {
        var url = await ShowInputDialogAsync(new InputDialogParameters("URL 입력", "게시글에 첨부할 URL을 입력해주세요.", "첨부하고자 하는 URL을 입력하세요", showCancel: true));
        if (string.IsNullOrWhiteSpace(url)) return;

        // TODO: create the HyperlinkContent/external-url preview from the entered URL.
    }

    // TODO: poll composing is not implemented yet; the poll button stays a no-op until the
    // poll editor surface is designed.
    [RelayCommand]
    private void HandlePollTap() { }

    // TODO: post reservation (date/time picking) is not implemented yet.
    [RelayCommand]
    private void HandleReservationTap() { }

    // TODO: kakao login and cross-post routing is decided here when the game posting is
    // implemented; until then the toggle press shows the login-needed hint.
    [RelayCommand]
    private async Task HandleKakaoPostTapAsync()
    {
        IsKakaoPostEnabled = false;
        await ShowMessageDialogAsync(new MessageDialogParameters("카카오 게시", "카카오 게시 연동은 아직 준비 중입니다."));
    }

    // TODO: collect the editor contents and send the WritePost request with the selected
    // discovery option, comment permission, share setting, and reservation time.
    [RelayCommand]
    private async Task HandleSubmitAsync() => await ShowMessageDialogAsync(new MessageDialogParameters("게시글 작성", "게시글 작성 기능은 아직 준비 중입니다."));
}
