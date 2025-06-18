using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

/// <summary>
/// Implementation of IFriendshipService using MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the FriendshipService class.
/// </remarks>
/// <param name="database">The MongoDB database instance.</param>
public class FriendshipService(IMongoDatabase database, INotificationService notificationService, IServiceProvider serviceProvider) : IFriendshipService
{
    private readonly IMongoCollection<Friendship> _friendshipCollection = database.GetCollection<Friendship>("Friendships");
    private readonly IMongoCollection<FavoriteFriend> _favoriteFriendCollection = database.GetCollection<FavoriteFriend>("FavoriteFriends");

    /// <inheritdoc/>
    public async Task<Result<Friendship>> GetFriendshipByIdAsync(string friendshipId)
    {
        var friendship = await _friendshipCollection.Find(f => f.Id == friendshipId).FirstOrDefaultAsync();
        if (friendship == null) return (ErrorType.NotFound, "친구 관계를 찾을 수 없습니다.");
        else return friendship;
    }

    /// <inheritdoc/>
    public async Task<Result> SendFriendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId) return Result.Failure(ErrorType.BadRequest, "자기 자신에게 친구 요청을 보낼 수 없습니다.");

        // Check if friendship already exists
        var existingFriendship = await _friendshipCollection.Find(f =>
            (f.UserId == senderId && f.FriendId == receiverId) ||
            (f.UserId == receiverId && f.FriendId == senderId)).FirstOrDefaultAsync();

        if (existingFriendship != null) return Result.Failure(ErrorType.Conflict, "차단한 또는 무시한 사용자거나 이미 친구 요청을 보낸 사용자입니다.");

        // Create new friendship request
        var requestFriendship = new Friendship
        {
            UserId = senderId,
            FriendId = receiverId,
            Status = FriendshipStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        while (true)
        {
            requestFriendship.Id = Guid.NewGuid().ToString("N");
            existingFriendship = await _friendshipCollection.Find(f => f.Id == requestFriendship.Id).FirstOrDefaultAsync();
            if (existingFriendship == null) break;
        }

        var waitingFriendship = new Friendship
        {
            UserId = receiverId,
            FriendId = senderId,
            CreatedAt = requestFriendship.CreatedAt,
            Status = FriendshipStatus.Waiting
        };

        while (true)
        {
            waitingFriendship.Id = Guid.NewGuid().ToString("N");
            existingFriendship = await _friendshipCollection.Find(f => f.Id == waitingFriendship.Id).FirstOrDefaultAsync();
            if (existingFriendship == null) break;
        }

        await _friendshipCollection.InsertOneAsync(requestFriendship);
        await _friendshipCollection.InsertOneAsync(waitingFriendship);

        // Send notification
        await notificationService.SendNotificationsAsync(NotificationType.FriendRequest, waitingFriendship.Id);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> AcceptFriendRequestAsync(string userId, string userToAcceptId)
    {
        // Find the request
        var requestFriendship = await _friendshipCollection.Find(f =>
            f.UserId == userToAcceptId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        var waitingFriendship = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.FriendId == userToAcceptId &&
            f.Status == FriendshipStatus.Waiting).FirstOrDefaultAsync();

        if (requestFriendship == null || waitingFriendship == null) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        UpdateResult result;
        var updateDefinition = Builders<Friendship>.Update
            .Set(f => f.Status, FriendshipStatus.Accepted)
            .Set(f => f.CreatedAt, DateTime.UtcNow);

        result = await _friendshipCollection.UpdateOneAsync(f => f.Id == requestFriendship.Id, updateDefinition);
        if (result.ModifiedCount == 0) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        result = await _friendshipCollection.UpdateOneAsync(f => f.Id == waitingFriendship.Id, updateDefinition);
        if (result.ModifiedCount == 0) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        // Send notifications
        await notificationService.SendNotificationsAsync(NotificationType.FriendRequest, waitingFriendship.Id);
        await notificationService.SendNotificationsAsync(NotificationType.FriendRequest, requestFriendship.Id);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> DeclineFriendRequestAsync(string userId, string userToDeclineId)
    {
        var requestFriendship = await _friendshipCollection.Find(f =>
            f.UserId == userToDeclineId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        var waitingFriendship = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.FriendId == userToDeclineId &&
            f.Status == FriendshipStatus.Waiting).FirstOrDefaultAsync();

        if (requestFriendship == null || waitingFriendship == null) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        DeleteResult result;

        result = await _friendshipCollection.DeleteOneAsync(f => f.Id == requestFriendship.Id);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        result = await _friendshipCollection.DeleteOneAsync(f => f.Id == waitingFriendship.Id);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        await notificationService.DeleteNotificationsAsync("AssociatedId", waitingFriendship.Id, NotificationType.FriendRequest);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> CancelFriendRequestAsync(string userId, string userToCancelId)
    {
        var requestFriendship = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.FriendId == userToCancelId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        var waitingFriendship = await _friendshipCollection.Find(f =>
            f.UserId == userToCancelId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Waiting).FirstOrDefaultAsync();

        if (requestFriendship == null || waitingFriendship == null) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        DeleteResult result;

        result = await _friendshipCollection.DeleteOneAsync(f => f.Id == requestFriendship.Id);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        result = await _friendshipCollection.DeleteOneAsync(f => f.Id == waitingFriendship.Id);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "친구 요청을 찾을 수 없습니다.");

        await notificationService.DeleteNotificationsAsync("AssociatedId", waitingFriendship.Id, NotificationType.FriendRequest);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> BlockUserAsync(string userId, string userToBlockId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var userToBlockResult = await userService.GetUserByIdAsync(userToBlockId);
        if (userToBlockResult.IsFailure) return userToBlockResult.CastFailure<Result>();

        if (userToBlockResult.Value.Rank >= Rank.Moderator)
            return (ErrorType.BadRequest, "관리자 또는 운영진을 차단할 수 없습니다.");

        var existingFriendships = await _friendshipCollection.Find(f =>
            (f.UserId == userId && f.FriendId == userToBlockId) ||
            (f.UserId == userToBlockId && f.FriendId == userId)).ToListAsync();

        // First, remove any existing friendship
        await _friendshipCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == userToBlockId) ||
            (f.UserId == userToBlockId && f.FriendId == userId));

        // Second, remove any existing favorite friendship
        await _favoriteFriendCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == userToBlockId) ||
            (f.UserId == userToBlockId && f.FriendId == userId));

        // Create blocked relationship
        var blockFriendship = new Friendship
        {
            UserId = userId,
            FriendId = userToBlockId,
            Status = FriendshipStatus.Blocked,
            CreatedAt = DateTime.UtcNow
        };

        while (true)
        {
            blockFriendship.Id = Guid.NewGuid().ToString("N");

            var existingFriendship = await _friendshipCollection.Find(f => f.Id == blockFriendship.Id).FirstOrDefaultAsync();
            if (existingFriendship == null) break;
        }

        await _friendshipCollection.InsertOneAsync(blockFriendship);

        if (existingFriendships.Count > 0) await notificationService.DeleteNotificationsAsync("AssociatedId", existingFriendships.Select(x => x.Id), NotificationType.FriendRequest);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> IgnoreUserAsync(string userId, string userToIgnoreId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var userToIgnoreResult = await userService.GetUserByIdAsync(userToIgnoreId);
        if (userToIgnoreResult.IsFailure) return userToIgnoreResult.CastFailure<Result>();

        if (userToIgnoreResult.Value.Rank >= Rank.Moderator)
            return (ErrorType.BadRequest, "관리자 또는 운영진을 무시할 수 없습니다.");

        var existingFriendships = await _friendshipCollection.Find(f =>
            (f.UserId == userId && f.FriendId == userToIgnoreId) ||
            (f.UserId == userToIgnoreId && f.FriendId == userId)).ToListAsync();

        // First, remove any existing friendship
        await _friendshipCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == userToIgnoreId) ||
            (f.UserId == userToIgnoreId && f.FriendId == userId));

        // Second, remove any existing favorite friendship
        await _favoriteFriendCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == userToIgnoreId) ||
            (f.UserId == userToIgnoreId && f.FriendId == userId));

        // Create blocked relationship
        var blockFriendship = new Friendship
        {
            UserId = userId,
            FriendId = userToIgnoreId,
            Status = FriendshipStatus.Ignored,
            CreatedAt = DateTime.UtcNow
        };

        while (true)
        {
            blockFriendship.Id = Guid.NewGuid().ToString("N");

            var existingFriendship = await _friendshipCollection.Find(f => f.Id == blockFriendship.Id).FirstOrDefaultAsync();
            if (existingFriendship == null) break;
        }

        await _friendshipCollection.InsertOneAsync(blockFriendship);

        if (existingFriendships.Count > 0) await notificationService.DeleteNotificationsAsync("AssociatedId", existingFriendships.Select(x => x.Id), NotificationType.FriendRequest);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> RemoveFriendAsync(string userId, string friendId)
    {
        var existingFriendships = await _friendshipCollection.Find(f =>
            (f.UserId == userId && f.FriendId == friendId && f.Status == FriendshipStatus.Accepted) ||
            (f.UserId == friendId && f.FriendId == userId && f.Status == FriendshipStatus.Accepted)).ToListAsync();

        // First, remove any existing friendship
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == friendId && f.Status == FriendshipStatus.Accepted) ||
            (f.UserId == friendId && f.FriendId == userId && f.Status == FriendshipStatus.Accepted));

        // Second, remove any existing favorite friendship
        await _favoriteFriendCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == friendId) ||
            (f.UserId == friendId && f.FriendId == userId));

        if (existingFriendships.Count > 0) await notificationService.DeleteNotificationsAsync("AssociatedId", existingFriendships.Select(x => x.Id), NotificationType.FriendRequest);

        return result.DeletedCount > 0 ? Result.Success() : (ErrorType.NotFound, "친구 관계를 찾을 수 없습니다.");
    }

    /// <inheritdoc/>
    public async Task<Result> UnblockUserAsync(string userId, string blockedUserId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            f.UserId == userId && f.FriendId == blockedUserId &&
            f.Status == FriendshipStatus.Blocked);

        return result.DeletedCount > 0 ? Result.Success() : (ErrorType.NotFound, "차단한 사용자를 찾을 수 없습니다.");
    }

    /// <inheritdoc/>
    public async Task<Result> UnignoreUserAsync(string userId, string ignoredUserId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            f.UserId == userId && f.FriendId == ignoredUserId &&
            f.Status == FriendshipStatus.Ignored);

        return result.DeletedCount > 0 ? Result.Success() : (ErrorType.NotFound, "무시한 사용자를 찾을 수 없습니다.");
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> GetFriendIdsAsync(string userId)
    {
        var friendIds = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Accepted)
            .Project(f => f.FriendId)
            .ToListAsync();

        return friendIds;
    }

    /// <inheritdoc/>
    public async Task<Result<List<Friendship>>> GetPendingRequestsAsync(string userId)
    {
        var pendingRequests = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Waiting)
            .ToListAsync();

        return pendingRequests;
    }

    /// <inheritdoc/>
    public async Task<Result<List<Friendship>>> GetWaitingRequestsAsync(string userId)
    {
        var waitingRequests = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Requested)
            .ToListAsync();

        return waitingRequests;
    }

    /// <inheritdoc/>
    public async Task<Result<List<Friendship>>> GetAllFriendshipsAsync(string userId) => await _friendshipCollection.Find(f => f.UserId == userId).ToListAsync();

    /// <summary>
    /// Retrieves a list of user IDs that are blocked, blocked by, or ignored by the specified user.
    /// </summary>
    /// <param name="userId">Identifies the user for whom the banned or ignored user IDs are being retrieved.</param>
    /// <returns>A list of strings representing the IDs of banned or ignored users.</returns>
    public async Task<Result<List<string>>> GetBannedUserIdsAsync(string userId)
    {
        // Get blocked users
        var filter = Builders<Friendship>.Filter.Eq(f => f.UserId, userId) &
                     Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Blocked);

        // Add ignored users
        filter |= Builders<Friendship>.Filter.Eq(f => f.UserId, userId) &
                     Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Ignored);

        var userBlockedIds = await _friendshipCollection.Find(filter)
            .Project(f => f.FriendId)
            .ToListAsync();

        // Add blocker users
        filter = Builders<Friendship>.Filter.Eq(f => f.FriendId, userId) &
                     Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Blocked);

        var userBlockerIds = await _friendshipCollection.Find(filter)
            .Project(f => f.UserId)
            .ToListAsync();

        // Combine both results and remove duplicates
        var allBannedUserIds = userBlockedIds.Union(userBlockerIds).Distinct().ToList();

        // return the combined list of banned user IDs
        return allBannedUserIds;
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> GetBlockedUserIdsAsync(string userId)
    {
        var blockedFriendIds = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Blocked)
            .Project(f => f.FriendId)
            .ToListAsync();

        return blockedFriendIds;
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> GetBlockerUserIdsAsync(string userId)
    {
        var blockerFriendIds = await _friendshipCollection.Find(f =>
            f.FriendId == userId && f.Status == FriendshipStatus.Blocked)
            .Project(f => f.UserId)
            .ToListAsync();

        return blockerFriendIds;
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> GetIgnoredUserIdsAsync(string userId)
    {
        var ignoredFriendIds = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Ignored)
            .Project(f => f.FriendId)
            .ToListAsync();

        return ignoredFriendIds;
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> AreFriendsAsync(string userId, string friendId)
    {
        var friendship = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.FriendId == friendId &&
            f.Status == FriendshipStatus.Accepted).FirstOrDefaultAsync();

        return friendship != null;
    }

    /// <inheritdoc/>
    public async Task<Result<Friendship>> GetFriendshipAsync(string userId, string friendId)
    {
        var friendship = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.FriendId == friendId).FirstOrDefaultAsync();

        if (friendship == null) return (ErrorType.NotFound, "친구 관계를 찾을 수 없습니다.");
        else return friendship;
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> AreFriendsOfFriendsAsync(string userId, string friendId)
    {
        var user1FriendIdsResult = await GetFriendIdsAsync(userId);
        if (user1FriendIdsResult.IsFailure) return user1FriendIdsResult.CastFailure<bool>();
        else if (user1FriendIdsResult.Value.Contains(friendId)) return true; // They are direct friends

        var user2FriendIdsResult = await GetFriendIdsAsync(friendId);
        if (user2FriendIdsResult.IsFailure) return user2FriendIdsResult.CastFailure<bool>();

        return user1FriendIdsResult.Value.Any(f => user2FriendIdsResult.Value.Contains(f));
    }

    /// <inheritdoc/>
    public async Task<Result<HashSet<string>>> GetFriendsOfFriendIdsAsync(string userId)
    {
        var directFriendIdsResult = await GetFriendIdsAsync(userId);
        if (directFriendIdsResult.IsFailure) return directFriendIdsResult.CastFailure<HashSet<string>>();

        var indirectFriendIds = await _friendshipCollection.Find(f =>
            directFriendIdsResult.Value.Contains(f.UserId) && f.Status == FriendshipStatus.Accepted)
            .Project(x => x.FriendId)
            .ToListAsync();

        var result = new HashSet<string>(directFriendIdsResult.Value.Union(indirectFriendIds));
        result.Remove(userId); // Ignore the user itself
        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<long>> GetUserFriendCountAsync(string userId) => await _friendshipCollection.CountDocumentsAsync(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted);

    /// <inheritdoc/>
    public async Task<Result<List<string>>> GetUserFriendIdsAsync(string userId, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var userResult = await userService.GetUserByIdAsync(userId);
        if (userResult.IsFailure) return (ErrorType.NotFound, userResult.ErrorMessage);

        var hasAccess = requesterId == userId;
        if(!hasAccess)
        {
            if (userResult.Value.FriendListDiscoveryOption == DiscoveryOption.Everyone) hasAccess = true;
            else if (requesterId == null) hasAccess = false;
            else if (userResult.Value.FriendListDiscoveryOption == DiscoveryOption.FriendsOfFriends)
            {
                var friendsOfFriendsResult = await GetFriendsOfFriendIdsAsync(requesterId);
                hasAccess = friendsOfFriendsResult.Value.Contains(requesterId);
            }
            else if (userResult.Value.FriendListDiscoveryOption == DiscoveryOption.Friends)
            {
                var areFriendsResult = await AreFriendsAsync(requesterId, userId);
                hasAccess = areFriendsResult.Value;
            }
        }

        if (!hasAccess) return (ErrorType.Forbidden, "해당 사용자의 친구 목록 공개 범위 설정에 따라 친구 목록을 볼 수 없습니다.");

        var friendIdsResult = await GetFriendIdsAsync(userId);

        // Remove blocked, ignored friends, and friends who blocked the requester
        if (requesterId != null)
        {
            var bannedUserIds = await GetBannedUserIdsAsync(requesterId);

            friendIdsResult.Value.RemoveAll(bannedUserIds.Value.Contains);
        }

        return friendIdsResult;
    }

    /// <inheritdoc/>
    public async Task<Result> HandleWithdrawAsync(string userId)
    {
        // Delete all friendships related to the user
        await _friendshipCollection.DeleteManyAsync(f => f.UserId == userId || f.FriendId == userId);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> GetFavoriteFriendIdsAsync(string userId)
    {
        var favoriteFriends = await _favoriteFriendCollection.Find(f => f.UserId == userId).ToListAsync();
        var friendIds = favoriteFriends.Select(f => f.FriendId).ToList();
        return friendIds;
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> GetFavoritedFriendIdsAsync(string userId)
    {
        var favoritedFriends = await _favoriteFriendCollection.Find(f => f.FriendId == userId).ToListAsync();
        var userIds = favoritedFriends.Select(f => f.UserId).ToList();
        return userIds;
    }

    /// <inheritdoc/>
    public async Task<Result> AddFavoriteFriendAsync(string userId, string friendId)
    {
        var existingFavorite = await _favoriteFriendCollection.Find(f => f.UserId == userId && f.FriendId == friendId).FirstOrDefaultAsync();
        if (existingFavorite != null) return Result.Failure(ErrorType.Conflict, "이미 관심 친구에 추가된 친구입니다.");

        var favoriteFriend = new FavoriteFriend
        {
            UserId = userId,
            FriendId = friendId,
            CreatedAt = DateTime.UtcNow
        };

        while (true)
        {
            favoriteFriend.Id = Guid.NewGuid().ToString("N");
            var existingFriendship = await _favoriteFriendCollection.Find(f => f.Id == favoriteFriend.Id).FirstOrDefaultAsync();
            if (existingFriendship == null) break;
        }
        await _favoriteFriendCollection.InsertOneAsync(favoriteFriend);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> RemoveFavoriteFriendAsync(string userId, string friendId)
    {
        var result = await _favoriteFriendCollection.DeleteOneAsync(f => f.UserId == userId && f.FriendId == friendId);
        return result.DeletedCount > 0 ? Result.Success() : (ErrorType.NotFound, "관심 친구를 찾을 수 없습니다.");
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> IsFavoriteFriendAsync(string userId, string friendId)
    {
        var favoriteFriend = await _favoriteFriendCollection.Find(f => f.UserId == userId && f.FriendId == friendId).FirstOrDefaultAsync();
        return favoriteFriend != null;
    }
}