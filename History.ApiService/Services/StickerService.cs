using History.ApiService.DataTypes;
using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class StickerService(IMongoDatabase database, IMediaService mediaService, IUserService userService) : IStickerService
{
    private readonly IMongoCollection<Sticker> _stickerCollection = database.GetCollection<Sticker>("Stickers");
    private readonly IMongoCollection<StickerAsset> _stickerAssetCollection = database.GetCollection<StickerAsset>("StickerAssets");
    private readonly IMongoCollection<StickerSubscription> _subscriptionCollection = database.GetCollection<StickerSubscription>("StickerSubscriptions");
    private readonly IMongoCollection<RecentStickerUsage> _recentUsageCollection = database.GetCollection<RecentStickerUsage>("RecentStickerUsages");

    private const int MaxStickerSize = 384;
    private const int MaxAssetCount = 200;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const int MaxRecentUsageCount = 50;

    /// <inheritdoc />
    public async Task<Result<Sticker>> CreateStickerAsync(string authorId, string name, string category, string description, bool isPrivate, IFormFile iconFile, IEnumerable<IFormFile> assetFiles)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(name)) return (ErrorType.BadRequest, "스티커 이름을 입력해주세요.");
        if (name.Length > 50) return (ErrorType.BadRequest, "스티커 이름은 50자 이하로 입력해주세요.");
        if (string.IsNullOrWhiteSpace(category)) return (ErrorType.BadRequest, "스티커 카테고리를 입력해주세요.");
        if (category.Length > 30) return (ErrorType.BadRequest, "스티커 카테고리는 30자 이하로 입력해주세요.");
        if (description?.Length > 200) return (ErrorType.BadRequest, "스티커 설명은 200자 이하로 입력해주세요.");

        if (iconFile == null) return (ErrorType.BadRequest, "스티커 아이콘을 업로드해주세요.");
        if (!iconFile.ContentType.StartsWith("image/")) return (ErrorType.BadRequest, "스티커 아이콘은 이미지 파일만 가능합니다.");
        if (iconFile.Length > MaxFileSize) return (ErrorType.BadRequest, $"스티커 아이콘 파일 크기가 너무 큽니다. {MaxFileSize / 1024 / 1024}MB 이하로 업로드해주세요.");

        var assetFileList = assetFiles?.ToList() ?? [];
        if (assetFileList.Count == 0) return (ErrorType.BadRequest, "스티커 에셋을 최소 1개 이상 업로드해주세요.");
        if (assetFileList.Count > MaxAssetCount) return (ErrorType.BadRequest, $"스티커 에셋은 최대 {MaxAssetCount}개까지 업로드 가능합니다.");

        foreach (var assetFile in assetFileList)
        {
            if (!assetFile.ContentType.StartsWith("image/")) return (ErrorType.BadRequest, $"스티커 에셋 '{assetFile.FileName}'은(는) 이미지 파일만 가능합니다.");
            if (assetFile.Length > MaxFileSize) return (ErrorType.BadRequest, $"스티커 에셋 '{assetFile.FileName}' 파일 크기가 너무 큽니다. {MaxFileSize / 1024 / 1024}MB 이하로 업로드해주세요.");
        }

        // Read and signature-check every upload before creating anything, so a disguised file is
        // rejected without leaving orphaned media or sticker records behind
        byte[] iconBytes;
        var assetItems = new List<(IFormFile File, byte[] Data)>();

        try
        {
            using (var iconStream = new MemoryStream())
            {
                await iconFile.CopyToAsync(iconStream);
                iconBytes = iconStream.ToArray();
            }

            foreach (var assetFile in assetFileList)
            {
                using var assetStream = new MemoryStream();
                await assetFile.CopyToAsync(assetStream);
                assetItems.Add((assetFile, assetStream.ToArray()));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"업로드 파일 읽기 실패: {ex.Message}");
            return (ErrorType.ProgramError, "업로드 파일을 읽는 중 오류가 발생했습니다.");
        }

        if (!MediaTypeDetector.IsImage(iconBytes)) return (ErrorType.BadRequest, "스티커 아이콘은 이미지 파일만 가능합니다.");

        var invalidAssetItem = assetItems.FirstOrDefault(x => !MediaTypeDetector.IsImage(x.Data));
        if (invalidAssetItem != default) return (ErrorType.BadRequest, $"스티커 에셋 '{invalidAssetItem.File.FileName}'은(는) 이미지 파일만 가능합니다.");

        // Create sticker
        var sticker = new Sticker
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = Utils.SanitizeText(name),
            Category = Utils.SanitizeText(category),
            AuthorId = authorId,
            Description = Utils.SanitizeText(description),
            IsPrivate = isPrivate,
            CreatedAt = DateTime.UtcNow
        };

        // Upload icon
        try
        {
            // Convert image (384x384 limit, no GIF conversion)
            var iconConvertResult = ConvertStickerImage(() => MediaEncodingHelper.ConvertImage(iconBytes, false, maxWidth: MaxStickerSize, maxHeight: MaxStickerSize), "스티커 아이콘");
            if (iconConvertResult.IsVideo) return (ErrorType.BadRequest, "스티커 아이콘은 정적 이미지만 가능합니다.");

            var iconMediaResult = await mediaService.CreateMediaAsync(MediaBucket.Sticker, sticker.Id, authorId, iconConvertResult.Data, iconConvertResult.MimeType);
            if (iconMediaResult.IsFailure) return iconMediaResult.CastFailure<Sticker>();

            sticker.IconMediaId = iconMediaResult.Value.Id;
        }
        catch (StickerImageConversionException exception)
        {
            return (ErrorType.BadRequest, exception.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"스티커 아이콘 업로드 실패: {ex.Message}");
            return (ErrorType.ProgramError, "스티커 아이콘 업로드 중 오류가 발생했습니다.");
        }

        // Save sticker
        await _stickerCollection.InsertOneAsync(sticker);

        // Upload assets
        try
        {
            var uploadTasks = assetItems.Select(async assetItem =>
            {
                // Convert animated images (GIF/WebP/APNG) to animated WebP, static images to static WebP
                var assetConvertResult = ConvertStickerImage(() => MediaEncodingHelper.ConvertAnimatedImage(assetItem.Data, maxWidth: MaxStickerSize, maxHeight: MaxStickerSize), $"스티커 에셋 '{assetItem.File.FileName}'");

                var assetMediaResult = await mediaService.CreateMediaAsync(MediaBucket.Sticker, sticker.Id, authorId, assetConvertResult.Data, assetConvertResult.MimeType);
                if (assetMediaResult.IsFailure) throw new InvalidOperationException(assetMediaResult.ErrorMessage);

                var stickerAsset = new StickerAsset
                {
                    Id = Guid.NewGuid().ToString("N"),
                    StickerId = sticker.Id,
                    MediaId = assetMediaResult.Value.Id,
                    IsAnimated = assetConvertResult.IsAnimated
                };

                return stickerAsset;
            });

            var stickerAssets = await Task.WhenAll(uploadTasks);
            await _stickerAssetCollection.InsertManyAsync(stickerAssets);
        }
        catch (StickerImageConversionException exception)
        {
            // Rollback: delete sticker and related media
            await RollbackStickerAsync(sticker);
            return (ErrorType.BadRequest, exception.Message);
        }
        catch (Exception ex)
        {
            // Rollback: delete sticker and related media
            await RollbackStickerAsync(sticker);
            Console.WriteLine($"스티커 에셋 업로드 실패: {ex.Message}");
            return (ErrorType.ProgramError, "스티커 에셋 업로드 중 오류가 발생했습니다.");
        }

        return sticker;
    }

    // A sticker image conversion failure always stems from the upload itself, so the raw ffmpeg output
    // (which carries build information and temp file paths) stays in the server log and only a client
    // error message is returned.
    private sealed class StickerImageConversionException(string message) : Exception(message);

    private static MediaConvertResult ConvertStickerImage(Func<MediaConvertResult> convertImage, string label)
    {
        try { return convertImage(); }
        catch (Exception exception)
        {
            Console.WriteLine($"{label} 변환 실패: {exception.Message}");
            throw new StickerImageConversionException($"{label}은(는) 지원하지 않거나 손상된 이미지 파일입니다.");
        }
    }

    private async Task RollbackStickerAsync(Sticker sticker)
    {
        await _stickerCollection.DeleteOneAsync(s => s.Id == sticker.Id);
        await mediaService.DeleteMediaByAssociatedIdAsync(sticker.Id);
    }

    /// <inheritdoc />
    public async Task<Result<Sticker>> GetStickerByIdAsync(string stickerId)
    {
        var sticker = await _stickerCollection.Find(s => s.Id == stickerId).FirstOrDefaultAsync();
        if (sticker == null) return (ErrorType.NotFound, "스티커를 찾을 수 없습니다.");
        return sticker;
    }

    /// <inheritdoc />
    public async Task<Result<List<Sticker>>> GetStickersAsync(string requesterId, string from, int limit)
    {
        var filterBuilder = Builders<Sticker>.Filter;
        var filter = filterBuilder.Or(
            filterBuilder.Eq(s => s.IsPrivate, false),
            filterBuilder.Eq(s => s.AuthorId, requesterId)
        );

        if (!string.IsNullOrEmpty(from))
        {
            var fromSticker = await _stickerCollection.Find(s => s.Id == from).FirstOrDefaultAsync();
            if (fromSticker != null)
            {
                filter &= filterBuilder.Lt(s => s.CreatedAt, fromSticker.CreatedAt);
            }
        }

        var stickers = await _stickerCollection.Find(filter)
            .SortByDescending(s => s.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        return stickers;
    }

    /// <inheritdoc />
    public async Task<Result<List<Sticker>>> GetStickersByUserIdAsync(string userId, string requesterId, string from, int limit)
    {
        var filterBuilder = Builders<Sticker>.Filter;
        var filter = filterBuilder.Eq(s => s.AuthorId, userId);

        // Exclude private stickers if not the owner
        if (userId != requesterId)
        {
            filter &= filterBuilder.Eq(s => s.IsPrivate, false);
        }

        if (!string.IsNullOrEmpty(from))
        {
            var fromSticker = await _stickerCollection.Find(s => s.Id == from).FirstOrDefaultAsync();
            if (fromSticker != null)
            {
                filter &= filterBuilder.Lt(s => s.CreatedAt, fromSticker.CreatedAt);
            }
        }

        var stickers = await _stickerCollection.Find(filter)
            .SortByDescending(s => s.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        return stickers;
    }

    /// <inheritdoc />
    public async Task<Result<List<Sticker>>> SearchStickersAsync(string query, string requesterId, string from, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return (ErrorType.BadRequest, "검색어를 입력해주세요.");

        var filterBuilder = Builders<Sticker>.Filter;
        var filter = filterBuilder.Or(
            filterBuilder.Eq(s => s.IsPrivate, false),
            filterBuilder.Eq(s => s.AuthorId, requesterId)
        );

        filter &= filterBuilder.Or(
            filterBuilder.Regex(s => s.Name, new MongoDB.Bson.BsonRegularExpression(query, "i")),
            filterBuilder.Regex(s => s.Category, new MongoDB.Bson.BsonRegularExpression(query, "i")),
            filterBuilder.Regex(s => s.Description, new MongoDB.Bson.BsonRegularExpression(query, "i"))
        );

        if (!string.IsNullOrEmpty(from))
        {
            var fromSticker = await _stickerCollection.Find(s => s.Id == from).FirstOrDefaultAsync();
            if (fromSticker != null)
            {
                filter &= filterBuilder.Lt(s => s.CreatedAt, fromSticker.CreatedAt);
            }
        }

        var stickers = await _stickerCollection.Find(filter)
            .SortByDescending(s => s.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        return stickers;
    }

    /// <inheritdoc />
    public async Task<Result> DeleteStickerAsync(string stickerId, string requesterId)
    {
        var stickerResult = await GetStickerByIdAsync(stickerId);
        if (stickerResult.IsFailure) return stickerResult.CastFailure();

        var sticker = stickerResult.Value;

        // Check permission (owner or moderator+)
        var requesterResult = await userService.GetUserByIdAsync(requesterId);
        if (requesterResult.IsFailure) return requesterResult.CastFailure();

        var requester = requesterResult.Value;
        if (sticker.AuthorId != requesterId && requester.Rank < Rank.Moderator)
        {
            return (ErrorType.Forbidden, "스티커를 삭제할 권한이 없습니다.");
        }

        // Delete assets
        await _stickerAssetCollection.DeleteManyAsync(sa => sa.StickerId == stickerId);

        // Delete media
        await mediaService.DeleteMediaByAssociatedIdAsync(stickerId);

        // Delete sticker
        await _stickerCollection.DeleteOneAsync(s => s.Id == stickerId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<List<StickerAsset>>> GetStickerAssetsAsync(string stickerId, string requesterId)
    {
        var stickerResult = await GetStickerByIdAsync(stickerId);
        if (stickerResult.IsFailure) return stickerResult.CastFailure<List<StickerAsset>>();

        var sticker = stickerResult.Value;

        // Error if private sticker and not the owner
        if (sticker.IsPrivate && sticker.AuthorId != requesterId)
        {
            return (ErrorType.Forbidden, "스티커를 조회할 권한이 없습니다.");
        }

        var assets = await _stickerAssetCollection.Find(sa => sa.StickerId == stickerId).ToListAsync();
        return assets;
    }

    /// <inheritdoc />
    public async Task<Result<StickerAsset>> GetStickerAssetByIdAsync(string assetId)
    {
        var asset = await _stickerAssetCollection.Find(sa => sa.Id == assetId).FirstOrDefaultAsync();
        if (asset == null) return (ErrorType.NotFound, "스티커 에셋을 찾을 수 없습니다.");
        return asset;
    }

    /// <inheritdoc />
    public async Task<Result<StickerResponseDto>> GenerateStickerResponseDtoAsync(Sticker sticker, string requesterId)
    {
        var authorResult = await userService.GenerateUserResponseDtoAsync(sticker.AuthorId, requesterId);
        var isSubscribed = await IsSubscribedAsync(sticker.Id, requesterId);

        var dto = new StickerResponseDto
        {
            Id = sticker.Id,
            Name = sticker.Name,
            Category = sticker.Category,
            Description = sticker.Description,
            IconMediaId = sticker.IconMediaId,
            IsPrivate = sticker.IsPrivate,
            CreatedAt = sticker.CreatedAt,
            ModifiedAt = sticker.ModifiedAt,
            Author = authorResult.IsSuccess ? authorResult.Value : null,
            IsSubscribed = isSubscribed,
            IsOwner = sticker.AuthorId == requesterId
        };

        return dto;
    }

    /// <inheritdoc />
    public async Task<Result<List<StickerResponseDto>>> GenerateStickerResponseDtosAsync(IEnumerable<Sticker> stickers, string requesterId)
    {
        var tasks = stickers.Select(s => GenerateStickerResponseDtoAsync(s, requesterId));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.IsSuccess).Select(r => r.Value).ToList();
    }

    /// <inheritdoc />
    public async Task<Result> SubscribeStickerAsync(string stickerId, string userId)
    {
        var stickerResult = await GetStickerByIdAsync(stickerId);
        if (stickerResult.IsFailure) return stickerResult.CastFailure();

        var sticker = stickerResult.Value;

        // Private stickers cannot be subscribed
        if (sticker.IsPrivate) return (ErrorType.Forbidden, "비공개 스티커는 구독할 수 없습니다.");

        // Own sticker does not need subscription
        if (sticker.AuthorId == userId) return (ErrorType.BadRequest, "본인의 스티커는 구독할 필요가 없습니다.");

        // Check if already subscribed
        var existing = await _subscriptionCollection.Find(s => s.StickerId == stickerId && s.UserId == userId).FirstOrDefaultAsync();
        if (existing != null) return (ErrorType.BadRequest, "이미 구독한 스티커입니다.");

        var subscription = new StickerSubscription
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            StickerId = stickerId,
            SubscribedAt = DateTime.UtcNow
        };

        await _subscriptionCollection.InsertOneAsync(subscription);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UnsubscribeStickerAsync(string stickerId, string userId)
    {
        var result = await _subscriptionCollection.DeleteOneAsync(s => s.StickerId == stickerId && s.UserId == userId);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "구독 정보를 찾을 수 없습니다.");
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<List<Sticker>>> GetSubscribedStickersAsync(string userId, string from, int limit)
    {
        var filterBuilder = Builders<StickerSubscription>.Filter;
        var filter = filterBuilder.Eq(s => s.UserId, userId);

        if (!string.IsNullOrEmpty(from))
        {
            var fromSub = await _subscriptionCollection.Find(s => s.StickerId == from && s.UserId == userId).FirstOrDefaultAsync();
            if (fromSub != null)
            {
                filter &= filterBuilder.Lt(s => s.SubscribedAt, fromSub.SubscribedAt);
            }
        }

        var subscriptions = await _subscriptionCollection.Find(filter)
            .SortByDescending(s => s.SubscribedAt)
            .Limit(limit)
            .ToListAsync();

        var stickerIds = subscriptions.Select(s => s.StickerId).ToList();
        var stickers = await _stickerCollection.Find(s => stickerIds.Contains(s.Id)).ToListAsync();

        // Maintain subscription order
        var orderedStickers = stickerIds
            .Select(id => stickers.FirstOrDefault(s => s.Id == id))
            .Where(s => s != null)
            .ToList();

        return orderedStickers;
    }

    /// <inheritdoc />
    public async Task<bool> IsSubscribedAsync(string stickerId, string userId)
    {
        var subscription = await _subscriptionCollection.Find(s => s.StickerId == stickerId && s.UserId == userId).FirstOrDefaultAsync();
        return subscription != null;
    }

    /// <inheritdoc />
    public async Task<Result> RecordStickerUsageAsync(string stickerId, string assetId, string userId)
    {
        // Check if asset exists
        var assetResult = await GetStickerAssetByIdAsync(assetId);
        if (assetResult.IsFailure) return assetResult.CastFailure();

        var asset = assetResult.Value;
        if (asset.StickerId != stickerId) return (ErrorType.BadRequest, "스티커 ID와 에셋이 일치하지 않습니다.");

        // Find existing usage record
        var existing = await _recentUsageCollection.Find(r => r.UserId == userId && r.StickerAssetId == assetId).FirstOrDefaultAsync();

        if (existing != null)
        {
            // Update existing record
            var update = Builders<RecentStickerUsage>.Update.Set(r => r.LastUsedAt, DateTime.UtcNow);
            await _recentUsageCollection.UpdateOneAsync(r => r.Id == existing.Id, update);
        }
        else
        {
            // Add new record
            var usage = new RecentStickerUsage
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                StickerId = stickerId,
                StickerAssetId = assetId,
                LastUsedAt = DateTime.UtcNow
            };
            await _recentUsageCollection.InsertOneAsync(usage);

            // Delete oldest records if exceeding max count
            var count = await _recentUsageCollection.CountDocumentsAsync(r => r.UserId == userId);
            if (count > MaxRecentUsageCount)
            {
                var oldestUsages = await _recentUsageCollection.Find(r => r.UserId == userId)
                    .SortBy(r => r.LastUsedAt)
                    .Limit((int)(count - MaxRecentUsageCount))
                    .ToListAsync();

                var oldestIds = oldestUsages.Select(u => u.Id).ToList();
                await _recentUsageCollection.DeleteManyAsync(r => oldestIds.Contains(r.Id));
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<List<StickerAsset>>> GetRecentStickerAssetsAsync(string userId, int limit)
    {
        var recentUsages = await _recentUsageCollection.Find(r => r.UserId == userId)
            .SortByDescending(r => r.LastUsedAt)
            .Limit(limit)
            .ToListAsync();

        var assetIds = recentUsages.Select(r => r.StickerAssetId).ToList();
        var assets = await _stickerAssetCollection.Find(a => assetIds.Contains(a.Id)).ToListAsync();

        // Maintain usage order
        var orderedAssets = assetIds
            .Select(id => assets.FirstOrDefault(a => a.Id == id))
            .Where(a => a != null)
            .ToList();

        return orderedAssets;
    }

    /// <inheritdoc />
    public async Task<Result> HandleWithdraw(string userId)
    {
        // Delete subscriptions
        await _subscriptionCollection.DeleteManyAsync(s => s.UserId == userId);

        // Delete recent usage records
        await _recentUsageCollection.DeleteManyAsync(r => r.UserId == userId);

        return Result.Success();
    }
}
