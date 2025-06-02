using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Driver;
using System.Text;

namespace History.ApiService.Services;

public class UserService(IMongoDatabase database, IMediaService mediaService, IServiceProvider serviceProvider) : IUserService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");

    /// <inheritdoc />
    public async Task<Result> CreateUserAsync(User user)
    {
        var isUserCollectionEmpty = await _userCollection.CountDocumentsAsync(FilterDefinition<User>.Empty) == 0;
        if (isUserCollectionEmpty) user.Rank = Rank.Admin;
        else user.Rank = Rank.User;

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
    public async Task<Result<List<User>>> FindUsersByNicknameAsync(string query, bool applyPermission, int limit = 0)
    {
        var filter = Builders<User>.Filter.Where(u => u.Nickname.Contains(query));
        if (applyPermission) filter &= Builders<User>.Filter.Eq(u => u.AllowSearch, true);
        if (limit > 0) return await _userCollection.Find(filter).Limit(limit).ToListAsync();
        else return await _userCollection.Find(filter).ToListAsync();
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
        var filter = Builders<User>.Filter.Eq(u => u.Rank, Rank.Moderator) | 
                     Builders<User>.Filter.Eq(u => u.Rank, Rank.Admin);

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
    public async Task<Result<List<string>>> GetModeratorIdsAsync()
    {
        var filter = Builders<User>.Filter.Eq(u => u.Rank, Rank.Moderator) |
            Builders<User>.Filter.Eq(u => u.Rank, Rank.Admin);
        var users = await _userCollection.Find(filter).Project(u => new { u.Id }).ToListAsync();
        return users.Select(u => u.Id).ToList();
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
    public async Task<Result> UpdateFriendListDiscoveryOptionAsync(string userId, DiscoveryOption discoveryOption)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.FriendListDiscoveryOption, discoveryOption);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "친구 목록 공개 범위를 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UpdateHandleAsync(string userId, string handle)
    {
        handle = handle.Trim().ToLower();

        if (handle.Contains(' ') || handle.Contains('@') || handle.Contains('#') || handle.Contains('!') || handle.Contains('$') || handle.Contains('%') || handle.Contains('^') || handle.Contains('&') || handle.Contains('*') || handle.Contains('(') || handle.Contains(')') || handle.Contains('+'))
            return (ErrorType.BadRequest, "허용되지 않는 문자가 포함되어 있습니다.\n공백이나 특수 문자(@, #, !, $, %, ^, &, *, (, ), +)는 사용할 수 없습니다.");

        var existingUser = await _userCollection.Find(u => u.Handle == handle).FirstOrDefaultAsync();
        if (existingUser != null) return (ErrorType.Conflict, "이미 사용 중인 핸들입니다.");

        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Handle, handle);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "핸들을 변경하는 중 오류가 발생했습니다.");
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
            var thumbnailConvertResult = MediaEncodingHelper.ConvertImage(image, false, 256);
            var thumbnailBytes = thumbnailConvertResult.Data;
            var thumbnailContentType = thumbnailConvertResult.MimeType;

            var thumbnailMediaResult = await mediaService.CreateMediaAsync(MediaBucket.Profile, userId, userId, thumbnailBytes, thumbnailContentType);
            if (thumbnailMediaResult.IsFailure) return thumbnailMediaResult.CastFailure<bool>();

            var convertResult = MediaEncodingHelper.ConvertImage(image, false, 512);
            var usesAnimatedProfileMedia = convertResult.IsVideo;
            var bytes = convertResult.Data;
            var contentType = convertResult.MimeType;

            var mediaResult = await mediaService.CreateMediaAsync(MediaBucket.Profile, userId, userId, bytes, contentType, thumbnailMediaResult.Value.Id);
            if (mediaResult.IsFailure) return mediaResult.CastFailure<bool>();

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.ProfileMediaId, mediaResult.Value.Id)
                .Set(u => u.ProfileThumbnailMediaId, thumbnailMediaResult.Value.Id)
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
            var thumbnailConvertResult = MediaEncodingHelper.ConvertImage(image, false, 1000);
            var thumbnailBytes = thumbnailConvertResult.Data;
            var thumbnailContentType = thumbnailConvertResult.MimeType;

            var thumbnailMediaResult = await mediaService.CreateMediaAsync(MediaBucket.Background, userId, userId, thumbnailBytes, thumbnailContentType);
            if (thumbnailMediaResult.IsFailure) return thumbnailMediaResult.CastFailure<bool>();

            var convertResult = MediaEncodingHelper.ConvertImage(image, false, 1000);
            var usesAnimatedBackgroundMedia = convertResult.IsVideo;
            var contentType = convertResult.MimeType;
            var bytes = convertResult.Data;

            var mediaResult = await mediaService.CreateMediaAsync(MediaBucket.Background, userId, userId, bytes, contentType, thumbnailMediaResult.Value.Id);
            if (mediaResult.IsFailure) return mediaResult.CastFailure<bool>();

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.BackgroundMediaId, mediaResult.Value.Id)
                .Set(u => u.BackgroundThumbnailMediaId, thumbnailMediaResult.Value.Id)
                .Set(u => u.UsesAnimatedBackgroundMedia, usesAnimatedBackgroundMedia);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "배경 이미지를 변경하는 중 오류가 발생했습니다.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePinnedPostAsync(string userId, string pinnedPostId)
    {
        var postService = serviceProvider.GetRequiredService<IPostService>();

        var userResult = await GetUserByIdAsync(userId);
        if (userResult.Error != null) return userResult.CastFailure<bool>();
        else if (userResult == null) return (ErrorType.NotFound, "사용자를 찾을 수 없습니다.");

        var postResult = await postService.GetPostByIdAsync(pinnedPostId);
        if (postResult.Error != null) return postResult.CastFailure<bool>();
        else if (postResult == null) return (ErrorType.NotFound, "게시글을 찾을 수 없습니다.");
        else if (postResult.Value.UserId != userId) return (ErrorType.Forbidden, "고정 게시글은 자신의 게시글만 설정할 수 있습니다.");

        var isUnpinning = userResult.Value.PinnedPostId == pinnedPostId;

        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.PinnedPostId, isUnpinning ? null : pinnedPostId);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "고정 게시글을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc/>
    public async Task<Result> WithdrawAsync(string userId)
    {
        var postService = serviceProvider.GetRequiredService<IPostService>();
        var commentService = serviceProvider.GetRequiredService<ICommentService>();
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();
        var refreshTokenService = serviceProvider.GetRequiredService<IRefreshTokenService>();
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        var mediaService = serviceProvider.GetRequiredService<IMediaService>();

        await postService.HandleWithdrawAsync(userId);
        await commentService.HandleWithdrawAsync(userId);
        await friendshipService.HandleWithdrawAsync(userId);
        await refreshTokenService.HandleWithdrawAsync(userId);
        await notificationService.HandleWithdrawAsync(userId);
        await mediaService.DeleteMediasByUserIdAsync(userId);

        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        await _userCollection.DeleteOneAsync(filter);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<string> GenerateTextPreviewFromContentsAsync(IEnumerable<BaseContent> contents, string requesterId = null)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var textAndProfileContents = contents.Where(x => x is TextContent || x is ProfileContent);
        var profileContents = textAndProfileContents.OfType<ProfileContent>();
        var profileUserIds = profileContents.Select(x => x.UserId).Distinct();
        var users = await GetUsersByIdsAsync(profileUserIds);
        var bannedUsers = await friendshipService.GetBannedUserIdsAsync(requesterId);
        users.Value.RemoveAll(x => bannedUsers.Value.Contains(x.Id));

        var builder = new StringBuilder();
        foreach (var textAndProfileContent in textAndProfileContents)
        {
            if (textAndProfileContent is TextContent textContent)
            {
                var text = textContent.Text.ReplaceLineEndings().Replace(Environment.NewLine, "");
                builder.Append(text);
            }
            else if (textAndProfileContent is ProfileContent profileContent)
            {
                var user = users.Value.FirstOrDefault(x => x.Id == profileContent.UserId);
                var nickname = user?.Nickname ?? "탈퇴한 사용자";
                builder.Append(nickname + ' ');
            }
        }
        return builder.ToString();
    }

    /// <inheritdoc/>
    public async Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(User user, string requesterId = null)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var result = new UserResponseDto(user);

        if(requesterId != null)
        {
            var requesterResult = await GetUserByIdAsync(requesterId);
            if (requesterResult.IsFailure) return requesterResult.CastFailure<UserResponseDto>();

            if (requesterResult.Value.Rank < Rank.Moderator)
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

            var friendshipResult = await friendshipService.GetFriendshipAsync(requesterId, user.Id);
            result.Friendship = friendshipResult.Value;
        }

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
        if (requesterId != null)
        {
            var requesterResult = await GetUserByIdAsync(requesterId);
            if (requesterResult.IsFailure) return requesterResult.CastFailure<List<UserResponseDto>>();

            if (requesterResult.Value.Rank < Rank.Moderator)
                bannedUserIds = await friendshipService.GetBannedUserIdsAsync(requesterId);
        }
        results.RemoveAll(x => bannedUserIds.Contains(x.UserId));

        var friendshipsResult = await friendshipService.GetAllFriendshipsAsync(requesterId);
        foreach (var result in results) result.Friendship = friendshipsResult.Value.FirstOrDefault(x => x.FriendId == result.UserId);

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
