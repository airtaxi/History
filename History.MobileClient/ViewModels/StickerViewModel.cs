using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.ViewModels;

public class StickerViewModel(StickerResponseDto sticker)
{
    public StickerResponseDto Sticker => sticker;

    public string Id => sticker.Id;
    public string Name => sticker.Name;
    public string Category => sticker.Category;
    public string Description => sticker.Description;
    public bool IsPrivate => sticker.IsPrivate;
    public string AuthorName => sticker.Author?.Nickname ?? "알 수 없음";
    public string IconUri => Utils.GenerateMediaUri(sticker.IconMediaId);
    public DateTime CreatedAt => sticker.CreatedAt;
}
