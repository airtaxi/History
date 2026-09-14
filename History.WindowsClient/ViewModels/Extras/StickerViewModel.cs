using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels.Extras;

// Wraps a sticker entry (icon + metadata) for the sticker list card and owns the card tap,
// which runs on the page view model's dialog surface.
public sealed partial class StickerViewModel(StickerResponseDto sticker, StickersPageViewModel pageViewModel) : ObservableObject
{
    private const string UnknownNickname = "알 수 없음";

    public string Id => sticker.Id;
    public string Name => sticker.Name;
    public bool IsPrivate => sticker.IsPrivate;
    public string AuthorName => sticker.Author?.Nickname ?? UnknownNickname;

    // The raw category string is a comma-separated tag list; each trimmed entry becomes a badge.
    public List<string> Tags => string.IsNullOrEmpty(sticker.Category) ? [] : [.. sticker.Category.Split(',').Select(tag => tag.Trim()).Where(tag => tag.Length > 0)];

    public BitmapImage IconSource => string.IsNullOrEmpty(sticker.IconMediaId) ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(sticker.IconMediaId)));

    [RelayCommand]
    private void HandleTap() => pageViewModel.OpenStickerDetail(Id);
}
