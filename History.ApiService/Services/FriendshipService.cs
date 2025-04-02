using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace History.ApiService.Services;

/// <summary>
/// Implementation of IFriendshipService using MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the FriendshipService class.
/// </remarks>
/// <param name="database">The MongoDB database instance.</param>
public class FriendshipService(IMongoDatabase database) : IFriendshipService
{
    private readonly IMongoCollection<Friendship> _friendshipCollection = database.GetCollection<Friendship>("Friendships");

    /// <inheritdoc/>
    public async Task<Result> SendFriendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId) return Result.Failure(ErrorType.SenderEqualsReceiver);

        // Check if friendship already exists
        var existingFriendship = await _friendshipCollection.Find(f =>
            (f.UserId == senderId && f.FriendId == receiverId) ||
            (f.UserId == receiverId && f.FriendId == senderId)).FirstOrDefaultAsync();

        if (existingFriendship != null) return Result.Failure(ErrorType.Conflict);

        // Create new friendship request
        var friendship = new Friendship
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = senderId,
            FriendId = receiverId,
            Status = FriendshipStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        await _friendshipCollection.InsertOneAsync(friendship);
        return null;
    }

    /// <inheritdoc/>
    public async Task<Result> AcceptFriendRequestAsync(string userId, string requesterId)
    {
        // Find the request
        var request = await _friendshipCollection.Find(f =>
            f.UserId == requesterId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        if (request == null) return ErrorType.NotFound;

        var updateDefinition = Builders<Friendship>.Update.Set(f => f.Status, FriendshipStatus.Ignored);

        var result = await _friendshipCollection.UpdateOneAsync(f => f.Id == request.Id, updateDefinition);

        return result.ModifiedCount > 0 ? ErrorType.NotFound : null;
    }

    /// <inheritdoc/>
    public async Task<Result> DeclineFriendRequestAsync(string userId, string requesterId)
    {
        var request = await _friendshipCollection.Find(f =>
            f.UserId == requesterId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        if (request == null) return ErrorType.NotFound;

        var updateDefinition = Builders<Friendship>.Update.Set(f => f.Status, FriendshipStatus.Declined);

        var result = await _friendshipCollection.UpdateOneAsync(f => f.Id == request.Id, updateDefinition);

        return result.ModifiedCount > 0 ? ErrorType.NotFound : null;
    }

    /// <inheritdoc/>
    public async Task<Result> BlockUserAsync(string userId, string userToBlockId)
    {
        // First, remove any existing friendship
        await _friendshipCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == userToBlockId) ||
            (f.UserId == userToBlockId && f.FriendId == userId));

        // Create blocked relationship
        var blockFriendship = new Friendship
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            FriendId = userToBlockId,
            Status = FriendshipStatus.Blocked,
            CreatedAt = DateTime.UtcNow
        };

        await _friendshipCollection.InsertOneAsync(blockFriendship);
        return null;
    }

    /// <inheritdoc/>
    public async Task<Result> IgnoreUserAsync(string userId, string userToIgnoreId)
    {
        // First, remove any existing friendship
        await _friendshipCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == userToIgnoreId) ||
            (f.UserId == userToIgnoreId && f.FriendId == userId));

        // Create blocked relationship
        var blockFriendship = new Friendship
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            FriendId = userToIgnoreId,
            Status = FriendshipStatus.Ignored,
            CreatedAt = DateTime.UtcNow
        };

        await _friendshipCollection.InsertOneAsync(blockFriendship);
        return null;
    }

    /// <inheritdoc/>
    public async Task<Result> RemoveFriendAsync(string userId, string friendId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == friendId && f.Status == FriendshipStatus.Accepted) ||
            (f.UserId == friendId && f.FriendId == userId && f.Status == FriendshipStatus.Accepted));

        return result.DeletedCount > 0 ? ErrorType.NotFound : null;
    }

    /// <inheritdoc/>
    public async Task<Result> UnblockUserAsync(string userId, string blockedUserId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            f.UserId == userId && f.FriendId == blockedUserId &&
            f.Status == FriendshipStatus.Blocked);

        return result.DeletedCount > 0 ? ErrorType.NotFound : null;
    }

    /// <inheritdoc/>
    public async Task<Result> UnignoreUserAsync(string userId, string ignoredUserId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            f.UserId == userId && f.FriendId == ignoredUserId &&
            f.Status == FriendshipStatus.Ignored);

        return result.DeletedCount > 0 ? ErrorType.NotFound : null;
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
            f.FriendId == userId && f.Status == FriendshipStatus.Requested)
            .ToListAsync();

        return pendingRequests;
    }

    /// <inheritdoc/>
    public async Task<Result<List<Friendship>>> GetSentRequestsAsync(string userId)
    {
        var waitingRequests = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Requested)
            .ToListAsync();

        return waitingRequests;
    }

    /// <inheritdoc/>
    public async Task<Result<List<Friendship>>> GetAllFriendshipsAsync(string userId)
    {
        var friendships = await _friendshipCollection.Find(f =>
            f.UserId == userId || f.FriendId == userId)
            .ToListAsync();

        return friendships;
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
        var ignoredFrienIds = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Ignored)
            .Project(f => f.FriendId)
            .ToListAsync();

        return ignoredFrienIds;
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

        if (friendship == null) return ErrorType.NotFound;
        else return friendship;
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> AreFriendsOfFriendsAsync(string userId, string friendId)
    {
        var user1FriendIdsResult = await GetFriendIdsAsync(userId);
        if (user1FriendIdsResult.IsFailure) return Result<bool>.Failure(user1FriendIdsResult);
        else if (user1FriendIdsResult.Value.Contains(friendId)) return true; // They are direct friends

        var user2FriendIdsResult = await GetFriendIdsAsync(friendId);
        if (user2FriendIdsResult.IsFailure) return Result<bool>.Failure(user2FriendIdsResult);

        return user1FriendIdsResult.Value.Any(f => user2FriendIdsResult.Value.Contains(f));
    }

    /// <inheritdoc/>
    public async Task<Result<HashSet<string>>> GetFriendsOfFriendIdsAsync(string userId)
    {
        var directFriendIdsResult = await GetFriendIdsAsync(userId);
        if (directFriendIdsResult.IsFailure) return Result<HashSet<string>>.Failure(directFriendIdsResult);

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
}