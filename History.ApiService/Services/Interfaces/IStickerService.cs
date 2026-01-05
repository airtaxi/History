using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;

namespace History.ApiService.Services.Interfaces;

public interface IStickerService
{
    /// <summary>
    /// Creates a sticker.
    /// </summary>
    /// <param name="authorId">Author ID</param>
    /// <param name="name">Sticker name</param>
    /// <param name="category">Sticker category</param>
    /// <param name="description">Sticker description</param>
    /// <param name="isPrivate">Whether the sticker is private</param>
    /// <param name="iconFile">Icon file</param>
    /// <param name="assetFiles">Sticker asset files</param>
    /// <returns>Created sticker</returns>
    Task<Result<Sticker>> CreateStickerAsync(string authorId, string name, string category, string description, bool isPrivate, IFormFile iconFile, IEnumerable<IFormFile> assetFiles);

    /// <summary>
    /// Gets a sticker by ID.
    /// </summary>
    /// <param name="stickerId">Sticker ID</param>
    /// <returns>Sticker</returns>
    Task<Result<Sticker>> GetStickerByIdAsync(string stickerId);

    /// <summary>
    /// Gets a list of stickers.
    /// </summary>
    /// <param name="requesterId">Requester ID</param>
    /// <param name="from">Pagination cursor ID</param>
    /// <param name="limit">Number of items to retrieve</param>
    /// <returns>List of stickers</returns>
    Task<Result<List<Sticker>>> GetStickersAsync(string requesterId, string from, int limit);

    /// <summary>
    /// Gets a list of stickers created by a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="requesterId">Requester ID</param>
    /// <param name="from">Pagination cursor ID</param>
    /// <param name="limit">Number of items to retrieve</param>
    /// <returns>List of stickers</returns>
    Task<Result<List<Sticker>>> GetStickersByUserIdAsync(string userId, string requesterId, string from, int limit);

    /// <summary>
    /// Searches stickers.
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="requesterId">Requester ID</param>
    /// <param name="from">Pagination cursor ID</param>
    /// <param name="limit">Number of items to retrieve</param>
    /// <returns>List of stickers</returns>
    Task<Result<List<Sticker>>> SearchStickersAsync(string query, string requesterId, string from, int limit);

    /// <summary>
    /// Deletes a sticker.
    /// </summary>
    /// <param name="stickerId">Sticker ID</param>
    /// <param name="requesterId">Requester ID</param>
    /// <returns>Result</returns>
    Task<Result> DeleteStickerAsync(string stickerId, string requesterId);

    /// <summary>
    /// Gets a list of sticker assets.
    /// </summary>
    /// <param name="stickerId">Sticker ID</param>
    /// <param name="requesterId">Requester ID</param>
    /// <returns>List of sticker assets</returns>
    Task<Result<List<StickerAsset>>> GetStickerAssetsAsync(string stickerId, string requesterId);

    /// <summary>
    /// Gets a sticker asset by ID.
    /// </summary>
    /// <param name="assetId">Sticker asset ID</param>
    /// <returns>Sticker asset</returns>
    Task<Result<StickerAsset>> GetStickerAssetByIdAsync(string assetId);

    /// <summary>
    /// Generates a sticker response DTO.
    /// </summary>
    /// <param name="sticker">Sticker</param>
    /// <param name="requesterId">Requester ID</param>
    /// <returns>Sticker response DTO</returns>
    Task<Result<StickerResponseDto>> GenerateStickerResponseDtoAsync(Sticker sticker, string requesterId);

    /// <summary>
    /// Generates a list of sticker response DTOs.
    /// </summary>
    /// <param name="stickers">List of stickers</param>
    /// <param name="requesterId">Requester ID</param>
    /// <returns>List of sticker response DTOs</returns>
    Task<Result<List<StickerResponseDto>>> GenerateStickerResponseDtosAsync(IEnumerable<Sticker> stickers, string requesterId);

    /// <summary>
    /// Subscribes to a sticker.
    /// </summary>
    /// <param name="stickerId">Sticker ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Result</returns>
    Task<Result> SubscribeStickerAsync(string stickerId, string userId);

    /// <summary>
    /// Unsubscribes from a sticker.
    /// </summary>
    /// <param name="stickerId">Sticker ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Result</returns>
    Task<Result> UnsubscribeStickerAsync(string stickerId, string userId);

    /// <summary>
    /// Gets a list of stickers subscribed by a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="from">Pagination cursor ID</param>
    /// <param name="limit">Number of items to retrieve</param>
    /// <returns>List of stickers</returns>
    Task<Result<List<Sticker>>> GetSubscribedStickersAsync(string userId, string from, int limit);

    /// <summary>
    /// Checks if a user is subscribed to a sticker.
    /// </summary>
    /// <param name="stickerId">Sticker ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Whether subscribed</returns>
    Task<bool> IsSubscribedAsync(string stickerId, string userId);

    /// <summary>
    /// Records sticker asset usage.
    /// </summary>
    /// <param name="stickerId">Sticker ID</param>
    /// <param name="assetId">Sticker asset ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Result</returns>
    Task<Result> RecordStickerUsageAsync(string stickerId, string assetId, string userId);

    /// <summary>
    /// Gets recently used sticker assets.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Number of items to retrieve</param>
    /// <returns>List of sticker assets</returns>
    Task<Result<List<StickerAsset>>> GetRecentStickerAssetsAsync(string userId, int limit);

    /// <summary>
    /// Handles user withdrawal.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Result</returns>
    Task<Result> HandleWithdraw(string userId);
}
