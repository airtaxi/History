using System.Text;
using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class UserService(IMongoDatabase database, IMediaService mediaService, IServiceProvider serviceProvider) : IUserService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");
    private readonly IMongoCollection<UserMemo> _userMemoCollection = database.GetCollection<UserMemo>("UserMemos");

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
    public async Task<Result> DeleteUserAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var result = await _userCollection.DeleteOneAsync(filter);
        return result.DeletedCount > 0 ? Result.Success() : (ErrorType.NotFound, "사용자를 찾을 수 없습니다.");
    }

    /// <inheritdoc />
    public async Task<bool> IsUserCollectionEmptyAsync() => await _userCollection.CountDocumentsAsync(FilterDefinition<User>.Empty) == 0;

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
    public async Task<Result<List<User>>> GetUsersByBirthdayAsync(DateTime timestamp)
    {
        var month = timestamp.Month;
        var day = timestamp.Day;
        var birthday = timestamp.ToString("MM-dd");
        var filter = Builders<User>.Filter.Eq(u => u.Birthday, birthday);
        var users = await _userCollection.Find(filter).ToListAsync();
        if (users == null || users.Count == 0) return (ErrorType.NotFound, "해당 생일을 가진 사용자를 찾을 수 없습니다.");
        return users;
    }

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
        var update = Builders<User>.Update.Set(u => u.Rank, Rank.User);

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
    public async Task<Result> UpdateBirthdayAsync(string userId, DateTime? timestamp)
    {
        var birthday = timestamp?.ToString("MM-dd");

        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Birthday, birthday);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "생일을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UpdateNicknameAsync(string userId, string nickname)
    {
        nickname = Utils.SanitizeText(nickname);
        if (string.IsNullOrEmpty(nickname)) return (ErrorType.BadRequest, "닉네임은 비워둘 수 없습니다.");

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
        handle = Utils.SanitizeText(handle.Trim().ToLower());

        if (handle.Contains(' ') || handle.Contains('@') || handle.Contains('#') || handle.Contains('!') || handle.Contains('$') || handle.Contains('%') || handle.Contains('^') || handle.Contains('&') || handle.Contains('*') || handle.Contains('(') || handle.Contains(')') || handle.Contains('+'))
            return (ErrorType.BadRequest, "허용되지 않는 문자가 포함되어 있습니다.\n공백이나 특수 문자(@, #, !, $, %, ^, &, *, (, ), +)는 사용할 수 없습니다.");

        var existingUser = await _userCollection.Find(u => u.Handle == handle).FirstOrDefaultAsync();
        if (existingUser != null) return (ErrorType.Conflict, "이미 사용 중인 핸들입니다.");

        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Handle, handle);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "핸들을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> UpdateProfileMediaAsync(string userId, byte[] image, string contentType = null)
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
                .Unset(u => u.ProfileThumbnailMediaId)
                .Unset(u => u.UsesAnimatedProfileMedia);

            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "프로필 이미지를 삭제하는 중 오류가 발생했습니다.");
        }
        else
        {
            var isWebP = contentType != null && contentType.Contains("webp", StringComparison.OrdinalIgnoreCase);

            var thumbnailConvertResult = isWebP ? MediaEncodingHelper.ConvertAnimatedWebP(image, true, 256, 256) : MediaEncodingHelper.ConvertImage(image, false, true, 256);
            var thumbnailBytes = thumbnailConvertResult.Data;
            var thumbnailContentType = thumbnailConvertResult.MimeType;

            var thumbnailMediaResult = await mediaService.CreateMediaAsync(MediaBucket.Profile, userId, userId, thumbnailBytes, thumbnailContentType);
            if (thumbnailMediaResult.IsFailure) return thumbnailMediaResult.CastFailure<bool>();

            var convertResult = isWebP ? MediaEncodingHelper.ConvertAnimatedWebP(image, true, 512, 512) : MediaEncodingHelper.ConvertImage(image, false, true, 512);
            var usesAnimatedProfileMedia = convertResult.IsAnimated;
            var bytes = convertResult.Data;
            var contentTypeHeader = convertResult.MimeType;

            var mediaResult = await mediaService.CreateMediaAsync(MediaBucket.Profile, userId, userId, bytes, contentTypeHeader, thumbnailMediaResult.Value.Id);
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
    public async Task<Result> UpdateBackgroundMediaAsync(string userId, byte[] image, string contentType = null)
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
                .Unset(u => u.BackgroundThumbnailMediaId)
                .Unset(u => u.UsesAnimatedBackgroundMedia);

            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "배경 이미지를 삭제하는 중 오류가 발생했습니다.");
        }
        else
        {
            var isWebP = contentType != null && contentType.Contains("webp", StringComparison.OrdinalIgnoreCase);

            var thumbnailConvertResult = isWebP
                ? MediaEncodingHelper.ConvertAnimatedWebP(image, true, 1000, 1000)
                : MediaEncodingHelper.ConvertImage(image, false, true, 1000);
            var thumbnailBytes = thumbnailConvertResult.Data;
            var thumbnailContentType = thumbnailConvertResult.MimeType;

            var thumbnailMediaResult = await mediaService.CreateMediaAsync(MediaBucket.Background, userId, userId, thumbnailBytes, thumbnailContentType);
            if (thumbnailMediaResult.IsFailure) return thumbnailMediaResult.CastFailure<bool>();

            var convertResult = isWebP
                ? MediaEncodingHelper.ConvertAnimatedWebP(image, true, 1000, 1000)
                : MediaEncodingHelper.ConvertImage(image, false, true, 1000);
            var usesAnimatedBackgroundMedia = convertResult.IsAnimated;
            var contentTypeHeader = convertResult.MimeType;
            var bytes = convertResult.Data;

            var mediaResult = await mediaService.CreateMediaAsync(MediaBucket.Background, userId, userId, bytes, contentTypeHeader, thumbnailMediaResult.Value.Id);
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
    public async Task<Result> UpdateMemoAsync(string userId, string requesterId, string memo)
    {
        if (userId == requesterId) return (ErrorType.BadRequest, "자신에게 메모를 작성할 수 없습니다.");

        var userResult = await GetUserByIdAsync(userId);
        if (userResult.IsFailure) return userResult.CastFailure();

        var requesterResult = await GetUserByIdAsync(requesterId);
        if (requesterResult.IsFailure) return requesterResult.CastFailure();

        await _userMemoCollection.DeleteManyAsync(m => m.UserId == userId && m.RegisteredBy == requesterId);

        memo = Utils.SanitizeText(memo);

        if (string.IsNullOrEmpty(memo)) return Result.Success();
        if (memo.Length > CommonConstants.MaxMemoLength) return (ErrorType.BadRequest, $"메모는 {CommonConstants.MaxMemoLength}자 이하로 작성해야 합니다.");

        var userMemo = new UserMemo
        {
            UserId = userId,
            RegisteredBy = requesterId,
            Memo = memo
        };

        while (true)
        {
            userMemo.Id = Guid.NewGuid().ToString("N");
            var existingMemo = await _userMemoCollection.Find(m => m.Id == userMemo.Id).FirstOrDefaultAsync();
            if (existingMemo == null) break;
        }

        await _userMemoCollection.InsertOneAsync(userMemo);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> UpdatePushNotificationPermissionAsync(string userId, PushNotificationType type, AccessPermission accessPermission)
    {
        var userResult = await GetUserByIdAsync(userId);
        if (userResult.IsFailure) return userResult.CastFailure();

        FilterDefinition<User> filter;
        UpdateDefinition<User> update;

        if (type == PushNotificationType.FavoriteFriendNewPost)
        {
            if (accessPermission < AccessPermission.Everyone && accessPermission > AccessPermission.OnlyMe)
                return (ErrorType.BadRequest, "관심 친구의 새 게시글 푸시 알림 설정은 켜짐 (모든 사람) 또는 꺼짐 (나만)으로 설정할 수 있습니다.");

            var isOn = accessPermission == AccessPermission.Everyone;
            filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            update = Builders<User>.Update.Set(u => u.IsFavoriteFriendNewPostPushNotificationEnabled, isOn);
        }
        else
        {
            var fieldName = $"{type}PushNotificationPermission";
            filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            update = Builders<User>.Update.Set(fieldName, accessPermission);
        }

        var result = await _userCollection.UpdateOneAsync(filter, update);
        return result.MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "푸시 알림 권한을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc/>
    public async Task<Result> UpdateMessageReceivingPermissionAsync(string userId, AccessPermission accessPermission)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.MessageReceivingPermission, accessPermission);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? Result.Success() : (ErrorType.NotFound, "메시지 수신 권한을 변경하는 중 오류가 발생했습니다.");
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> FilterAllowSearch(List<string> userIds)
    {
        if (userIds == null || !userIds.Any()) return new List<string>();
        var filter = Builders<User>.Filter.In(u => u.Id, userIds) & Builders<User>.Filter.Eq(u => u.AllowSearch, true);
        var ids = await _userCollection.Find(filter).Project(u => u.Id).ToListAsync();
        return ids;
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> FilterPushNotificationPermissionsAsync(string userId, IEnumerable<string> recipients, PushNotificationType type)
    {
        if (recipients == null || !recipients.Any()) return new List<string>();
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var userFriendsResult = await friendshipService.GetFriendIdsAsync(userId);
        if (userFriendsResult.IsFailure) return userFriendsResult;

        var userFriendsOfFriendIdsResult = await friendshipService.GetFriendsOfFriendIdsAsync(userId);
        if (userFriendsOfFriendIdsResult.IsFailure) return userFriendsOfFriendIdsResult.CastFailure<List<string>>();

        FilterDefinition<User> filter;
        if (type == PushNotificationType.FavoriteFriendNewPost) filter = Builders<User>.Filter.In(u => u.Id, recipients) & (Builders<User>.Filter.Eq(u => u.IsFavoriteFriendNewPostPushNotificationEnabled, true) | Builders<User>.Filter.Exists(u => u.IsFavoriteFriendNewPostPushNotificationEnabled, false));
        else
        {
            var typeString = type.ToString();
            var fieldName = $"{typeString}PushNotificationPermission";
            filter = Builders<User>.Filter.Or(Builders<User>.Filter.Eq(fieldName, AccessPermission.Everyone) & Builders<User>.Filter.In(u => u.Id, recipients),
                Builders<User>.Filter.Eq(fieldName, AccessPermission.FriendsOfFriends) & Builders<User>.Filter.In(u => u.Id, userFriendsOfFriendIdsResult.Value) & Builders<User>.Filter.In(u => u.Id, recipients),
                Builders<User>.Filter.Eq(fieldName, AccessPermission.Friends) & Builders<User>.Filter.In(u => u.Id, userFriendsResult.Value) & Builders<User>.Filter.In(u => u.Id, recipients));
        }

        var ids = await _userCollection.Find(filter).Project(u => u.Id).ToListAsync();
        return ids.Distinct().ToList();
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
        var messageService = serviceProvider.GetRequiredService<IMessageService>();
        var inviteCodeService = serviceProvider.GetRequiredService<IInviteCodeService>();

        await postService.HandleWithdrawAsync(userId);
        await commentService.HandleWithdrawAsync(userId);
        await friendshipService.HandleWithdrawAsync(userId);
        await refreshTokenService.HandleWithdrawAsync(userId);
        await notificationService.HandleWithdrawAsync(userId);
        await messageService.HandleWithdrawAsync(userId);
        await inviteCodeService.HandleWithdrawAsync(userId);
        await mediaService.DeleteMediasByUserIdAsync(userId);

        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        await _userCollection.DeleteOneAsync(filter);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<string> GenerateTextPreviewFromContentsAsync(IEnumerable<BaseContent> contents, string requesterId = null)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var textTypeContents = contents.Where(x => x is TextContent || x is ProfileContent || x is HashtagContent);
        var profileContents = textTypeContents.OfType<ProfileContent>();
        var profileUserIds = profileContents.Select(x => x.UserId).Distinct();
        var users = await GetUsersByIdsAsync(profileUserIds);
        var bannedUsers = await friendshipService.GetBannedUserIdsAsync(requesterId);
        users.Value.RemoveAll(x => bannedUsers.Value.Contains(x.Id));

        var builder = new StringBuilder();
        foreach (var textTypeContent in textTypeContents)
        {
            if (textTypeContent is TextContent textContent)
            {
                var text = textContent.Text.ReplaceLineEndings().Replace(Environment.NewLine, "");
                builder.Append(text);
            }
            else if (textTypeContent is ProfileContent profileContent)
            {
                var user = users.Value.FirstOrDefault(x => x.Id == profileContent.UserId);
                var nickname = user?.Nickname ?? "탈퇴한 사용자";
                builder.Append(nickname);
            }
            else if (textTypeContent is HashtagContent hashtagContent)
            {
                builder.Append('#' + hashtagContent.Tag);
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
            var IsFavoriteFriendResult = await friendshipService.IsFavoriteFriendAsync(requesterId, user.Id);

            result.Friendship = friendshipResult.Value;
            result.IsFavorite = IsFavoriteFriendResult.Value;

            var userMemo = await _userMemoCollection.Find(m => m.UserId == user.Id && m.RegisteredBy == requesterId).FirstOrDefaultAsync();
            if (userMemo != null) result.Nickname += $" ({userMemo.Memo})";
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
        var favoriteFriendIdsResult = await friendshipService.GetFavoriteFriendIdsAsync(requesterId);
        var userMemos = await _userMemoCollection.Find(m => m.RegisteredBy == requesterId).ToListAsync();
        foreach (var result in results)
        {
            var userMemo = userMemos.FirstOrDefault(m => m.UserId == result.UserId && m.RegisteredBy == requesterId);
            if (userMemo != null) result.Nickname += $" ({userMemo.Memo})";
            result.IsFavorite = favoriteFriendIdsResult.Value.Contains(result.UserId);
            result.Friendship = friendshipsResult.Value.FirstOrDefault(x => x.FriendId == result.UserId);
        }

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
