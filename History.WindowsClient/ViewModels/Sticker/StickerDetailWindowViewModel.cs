using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Api.Sticker;
using History.Commons.DataTypes.ResponseDtos;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.Sticker;

// Sticker detail window state: loads the sticker and its assets, owns the subscribe toggle,
// the delete flow, and the in-window asset preview overlay.
public sealed partial class StickerDetailWindowViewModel(string stickerId) : BaseViewModel
{
    private const string UnknownNickname = "알 수 없음";
    private const string DeleteConfirmTitle = "스티커 삭제";
    private const string DeleteConfirmMessage = "정말로 이 스티커를 삭제하시겠습니까?\n삭제된 스티커는 복구할 수 없습니다.";
    private const string DeletedTitle = "안내";
    private const string DeletedMessage = "스티커가 삭제되었습니다.";

    // Raised when the delete flow finished successfully, so the hosting window closes itself.
    public event EventHandler CloseRequested;

    // Whether the detail changed sticker data (subscription state or deletion), so the hosted
    // sticker list refreshes when the window closes.
    public bool HasChanges { get; private set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AuthorName { get; set; } = UnknownNickname;

    [ObservableProperty]
    public partial List<string> Tags { get; set; } = [];

    [ObservableProperty]
    public partial string CreatedAtText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrivateBadgeVisibility))]
    public partial bool IsPrivate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DescriptionVisibility))]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BitmapImage IconSource { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubscribeButtonText))]
    public partial bool IsSubscribed { get; set; }

    [ObservableProperty]
    public partial Visibility SubscribeButtonVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility DeleteButtonVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyStateVisibility))]
    public partial ObservableCollection<StickerAssetItemViewModel> Assets { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyStateVisibility))]
    public partial bool IsInitialized { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AssetPreviewVisibility))]
    public partial bool IsAssetPreviewVisible { get; set; }

    [ObservableProperty]
    public partial BitmapImage PreviewAssetImageSource { get; set; }

    public Visibility PrivateBadgeVisibility => IsPrivate ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DescriptionVisibility => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility EmptyStateVisibility => IsInitialized && Assets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AssetPreviewVisibility => IsAssetPreviewVisible ? Visibility.Visible : Visibility.Collapsed;

    public string SubscribeButtonText => IsSubscribed ? "구독 취소" : "구독하기";

    // Loads the sticker and its assets. Returns false when the sticker is gone, so the window
    // closes instead of leaving an empty detail behind.
    public async Task<bool> InitializeAsync()
    {
        var stickerResult = await ExecuteRequestAsync(new GetSticker(stickerId));
        if (!stickerResult.IsSuccess) return false;

        ApplySticker(stickerResult.Value);

        var assetsResult = await ExecuteRequestAsync(new GetStickerAssets(stickerId));
        if (assetsResult.IsSuccess) Assets = new ObservableCollection<StickerAssetItemViewModel>(assetsResult.Value.Select(asset => new StickerAssetItemViewModel(asset, this)));

        IsInitialized = true;
        return true;
    }

    // Shows the tapped asset in the in-window preview overlay.
    public void ShowAssetPreview(StickerAssetItemViewModel asset)
    {
        PreviewAssetImageSource = asset.ImageSource;
        IsAssetPreviewVisible = true;
    }

    // Closes the asset preview overlay; the window's Escape key runs this before closing the
    // window itself.
    [RelayCommand]
    public void CloseAssetPreview()
    {
        IsAssetPreviewVisible = false;
        PreviewAssetImageSource = null;
    }

    private void ApplySticker(StickerResponseDto sticker)
    {
        Name = sticker.Name;
        AuthorName = sticker.Author?.Nickname ?? UnknownNickname;

        // The raw category string is a comma-separated tag list; each trimmed entry becomes a badge.
        Tags = string.IsNullOrEmpty(sticker.Category) ? [] : [.. sticker.Category.Split(',').Select(tag => tag.Trim()).Where(tag => tag.Length > 0)];
        CreatedAtText = sticker.CreatedAt.ToLocalTime().ToString("yyyy. M. d.");
        IsPrivate = sticker.IsPrivate;
        Description = sticker.Description;
        IconSource = string.IsNullOrEmpty(sticker.IconMediaId) ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(sticker.IconMediaId)));
        IsSubscribed = sticker.IsSubscribed;

        // The subscribe toggle is for other users' public stickers; delete stays on the
        // viewer's own sticker.
        SubscribeButtonVisibility = !sticker.IsOwner && !sticker.IsPrivate ? Visibility.Visible : Visibility.Collapsed;
        DeleteButtonVisibility = sticker.IsOwner ? Visibility.Visible : Visibility.Collapsed;
    }

    // Toggles the subscription state of the sticker.
    [RelayCommand]
    private async Task ToggleSubscribeAsync()
    {
        var result = IsSubscribed ? await ExecuteRequestAsync(new UnsubscribeSticker(stickerId)) : await ExecuteRequestAsync(new SubscribeSticker(stickerId));
        if (!result.IsSuccess) return;

        IsSubscribed = !IsSubscribed;
        HasChanges = true;
    }

    // Confirms, deletes the sticker, reports the result, and closes the detail. The sticker
    // list refreshes through HasChanges once the window is gone.
    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmResult = await ShowMessageDialogAsync(new MessageDialogParameters(DeleteConfirmTitle, DeleteConfirmMessage, "삭제", cancelButtonText: "취소"));
        if (confirmResult != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new DeleteSticker(stickerId));
        if (!result.IsSuccess) return;

        HasChanges = true;
        await ShowMessageDialogAsync(new MessageDialogParameters(DeletedTitle, DeletedMessage));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
