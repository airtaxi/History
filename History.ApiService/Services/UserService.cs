using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class UserService(IMongoDatabase database, IMediaService mediaService, IServiceProvider serviceProvider) : IUserService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");

    /// <inheritdoc />
    public async Task<Result> CreateUserAsync(User user)
    {
        var isUserCollectionEmpty = await _userCollection.CountDocumentsAsync(FilterDefinition<User>.Empty) == 0;
        if (isUserCollectionEmpty) user.Rank = Rank.Admin;
        else user.Rank = Rank.Unauthorized;

        user.CreatedAt = DateTime.UtcNow;

        await _userCollection.InsertOneAsync(user);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<User>> GetUserByIdAsync(string id)
    {
        var user = await _userCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return (ErrorType.NotFound, "사용자를 찾을 수 없습니다.");

        return user;
    }

    /// <inheritdoc />
    public async Task<Result<List<User>>> GetUsersByIdsAsync(IEnumerable<string> userIds) => await _userCollection.Find(u => userIds.Contains(u.Id)).ToListAsync();

    /// <inheritdoc />
    public async Task<Result<User>> GetUserByHandleAsync(string handle, bool applyPermission)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Handle, handle);
        if (applyPermission) filter &= Builders<User>.Filter.Eq(u => u.AllowSearch, true);

        var user = await _userCollection.Find(filter).FirstOrDefaultAsync();
        if (user == null) return (ErrorType.NotFound, "사용자를 찾을 수 없습니다.");
        return user;
    }

    /// <inheritdoc />
    public async Task<Result<List<User>>> FindUsersByNicknameAsync(string query, bool applyPermission)
    {
        var filter = Builders<User>.Filter.Regex(u => u.Nickname, new BsonRegularExpression(query, "i"));
        if (applyPermission) filter &= Builders<User>.Filter.Eq(u => u.AllowSearch, true);
        return await _userCollection.Find(filter).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result> ApproveUnauthorizedUserAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId) & Builders<User>.Filter.Eq(u => u.Rank, Rank.Unauthorized);
        var update = Builders<User>.Update.Set(u => u.Rank, Rank.Unauthorized);

        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "승인할 사용자를 찾을 수 없습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UnapproveUnauthorizedUserAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId) & Builders<User>.Filter.Eq(u => u.Rank, Rank.Unauthorized);

        return (await _userCollection.DeleteOneAsync(filter)).DeletedCount > 0 ? Result.Success() : (ErrorType.NotFound, "승인 취소할 사용자를 찾을 수 없습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> MakeUserModeratorAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId) & Builders<User>.Filter.Eq(u => u.Rank, Rank.User);
        var update = Builders<User>.Update.Set(u => u.Rank, Rank.Moderator);

        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "관리자로 만들 일반 사용자를 찾을 수 없습니다.");
    }

    /// <inheritdoc />
    public async Task<Result<List<User>>> GetUnauthorizedUsersAsync(int limit = 50, string fromUserId = null)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Rank, Rank.Unauthorized);

        if (!string.IsNullOrEmpty(fromUserId))
        {
            var fromUser = _userCollection.Find(u => u.Id == fromUserId).FirstOrDefault();
            if (fromUser != null)
            {
                filter &= Builders<User>.Filter.Gt(u => u.CreatedAt, fromUser.CreatedAt);
            }
        }

        return await _userCollection.Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<List<User>>> GetModeratorsAsync(int limit = 10, string fromUserId = null)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Rank, Rank.Moderator);

        if (!string.IsNullOrEmpty(fromUserId))
        {
            var fromUser = _userCollection.Find(u => u.Id == fromUserId).FirstOrDefault();
            if (fromUser != null)
            {
                filter &= Builders<User>.Filter.Gt(u => u.CreatedAt, fromUser.CreatedAt);
            }
        }

        return await _userCollection.Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateDescriptionAsync(string userId, string description)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Description, description);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "소개글을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UpdateBirthdayAsync(string userId, DateTime? birthday)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Birthday, birthday);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "생일을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UpdateNicknameAsync(string userId, string nickname)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Nickname, nickname);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "닉네임을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAllowSearchAsync(string userId, bool allowSearch)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.AllowSearch, allowSearch);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "검색 허용 여부를 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UpdateProfileMediaAsync(string userId, byte[] image)
    {
        var userResult = await GetUserByIdAsync(userId);
        if (userResult.Error != null) return userResult.CastFailure();
        else if (userResult == null) return (ErrorType.NotFound, "사용자를 찾을 수 없습니다.");

        if (userResult.Value.ProfileMediaId != null) await mediaService.DeleteMediaByIdAsync(userResult.Value.ProfileMediaId);

        if (image == null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Unset(u => u.ProfileMediaId)
                .Set(u => u.UsesAnimatedProfileMedia, false);

            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "프로필 이미지를 삭제하는 중 오류가 발생했습니다.");
        }
        else
        {
            var convertResult = ImageMagickHelper.ConvertAndSave(image, true, 384);
            var bytes = convertResult.Data;
            var contentType = convertResult.MimeType;
            var usesAnimatedProfileMedia = convertResult.IsMp4;

            var mediaResult = await mediaService.CreateMediaAsync(MediaBucket.Profile, userId, userId, bytes, contentType);
            if (mediaResult.Error != null) return mediaResult.CastFailure<bool>();

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.ProfileMediaId, mediaResult.Value.Id)
                .Set(u => u.UsesAnimatedProfileMedia, usesAnimatedProfileMedia);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "프로필 이미지를 변경하는 중 오류가 발생했습니다.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateBackgroundMediaAsync(string userId, byte[] image)
    {
        var userResult = await GetUserByIdAsync(userId);
        if (userResult.Error != null) return userResult.CastFailure<bool>();
        else if (userResult == null) return (ErrorType.NotFound, "사용자를 찾을 수 없습니다.");

        if (userResult.Value.BackgroundMediaId != null) await mediaService.DeleteMediaByIdAsync(userResult.Value.BackgroundMediaId);

        if (image == null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Unset(u => u.BackgroundMediaId)
                .Set(u => u.UsesAnimatedBackgroundMedia, false);

            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "배경 이미지를 삭제하는 중 오류가 발생했습니다.");
        }
        else
        {
            var convertResult = ImageMagickHelper.ConvertAndSave(image, false, 720);
            var bytes = convertResult.Data;
            var contentType = convertResult.MimeType;
            var usesAnimatedBackgroundMedia = convertResult.IsMp4;

            var mediaResult = await mediaService.CreateMediaAsync(MediaBucket.Background, userId, userId, bytes, contentType);
            if (mediaResult.Error != null) return mediaResult.CastFailure<bool>();

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.BackgroundMediaId, mediaResult.Value.Id)
                .Set(u => u.UsesAnimatedBackgroundMedia, usesAnimatedBackgroundMedia);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "배경 이미지를 변경하는 중 오류가 발생했습니다.");
        }
    }
    /// <inheritdoc/>
    public async Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(User user, string requesterId = null)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var result = new UserResponseDto(user);

        if(requesterId != null)
        {

            var blockedUserIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
            if (blockedUserIdsResult.IsFailure) return blockedUserIdsResult.CastFailure<UserResponseDto>();
            else if (blockedUserIdsResult.Value.Contains(user.Id)) return (ErrorType.Forbidden, "차단한 사용자 접근 오류");

            var ignoredUserIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
            if (ignoredUserIdsResult.IsFailure) return ignoredUserIdsResult.CastFailure<UserResponseDto>();
            else if (ignoredUserIdsResult.Value.Contains(user.Id)) return (ErrorType.Forbidden, "무시한 사용자 접근 오류");

            var blockerUserIdsResult = await friendshipService.GetBlockerUserIdsAsync(user.Id);
            if (blockerUserIdsResult.IsFailure) return blockerUserIdsResult.CastFailure<UserResponseDto>();
            else if (blockerUserIdsResult.Value.Contains(requesterId)) return (ErrorType.Forbidden, "차단당한 사용자 접근 오류");
        }

        var friendshipResult = await friendshipService.GetFriendshipAsync(user.Id, requesterId);
        result.Friendship = friendshipResult.Value;

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(string userId, string requesterId = null)
    {
        var userResult = await GetUserByIdAsync(userId);
        if (userResult.IsFailure) return userResult.CastFailure<UserResponseDto>();

        return await GenerateUserResponseDtoAsync(userResult, requesterId);
    }

    /// <inheritdoc/>
    public async Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<User> users, string requesterId = null)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var results = users.Select(x => new UserResponseDto(x)).ToList();

        var bannedUserIds = new List<string>();
        if (requesterId != null) {
            bannedUserIds = await friendshipService.GetBannedUserIdsAsync(requesterId);
        }

        results.RemoveAll(x => bannedUserIds.Contains(x.UserId));

        var friendshipsResult = await friendshipService.GetAllFriendshipsAsync(requesterId);
        if (friendshipsResult.IsFailure) return friendshipsResult.CastFailure<List<UserResponseDto>>();

        foreach (var result in results) result.Friendship = friendshipsResult.Value.FirstOrDefault(x => x.FriendId == requesterId);

        return results;
    }

    /// <inheritdoc/>
    public async Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<string> userIds, string requesterId = null)
    {
        var usersResult = await GetUsersByIdsAsync(userIds);
        if (usersResult.IsFailure) return usersResult.CastFailure<List<UserResponseDto>>();

        return await GenerateUserResponseDtosAsync(usersResult.Value, requesterId);
    }
}
