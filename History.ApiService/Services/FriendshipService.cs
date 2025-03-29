using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public async Task<bool> SendFriendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId) return false;

        // Check if friendship already exists
        var existingFriendship = await _friendshipCollection.Find(f =>
            (f.UserId == senderId && f.FriendId == receiverId) ||
            (f.UserId == receiverId && f.FriendId == senderId)).FirstOrDefaultAsync();

        if (existingFriendship != null) return false;

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
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> AcceptFriendRequestAsync(string userId, string requesterId)
    {
        // Find the request
        var request = await _friendshipCollection.Find(f =>
            f.UserId == requesterId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        if (request == null) return false;

        // Update the request to Accepted
        var updateDefinition = Builders<Friendship>.Update.Set(f => f.Status, FriendshipStatus.Accepted);

        var result = await _friendshipCollection.UpdateOneAsync(f => f.Id == request.Id, updateDefinition);

        if (result.ModifiedCount == 0) return false;

        // Create reciprocal friendship
        var reciprocalFriendship = new Friendship
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            FriendId = requesterId,
            Status = FriendshipStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        };

        await _friendshipCollection.InsertOneAsync(reciprocalFriendship);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeclineFriendRequestAsync(string userId, string requesterId)
    {
        var request = await _friendshipCollection.Find(f =>
            f.UserId == requesterId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        if (request == null) return false;

        var updateDefinition = Builders<Friendship>.Update.Set(f => f.Status, FriendshipStatus.Declined);

        var result = await _friendshipCollection.UpdateOneAsync(f => f.Id == request.Id, updateDefinition);

        return result.ModifiedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> BlockUserAsync(string userId, string userToBlockId)
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
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> IgnoreRequestAsync(string userId, string requesterId)
    {
        var request = await _friendshipCollection.Find(f =>
            f.UserId == requesterId && f.FriendId == userId &&
            f.Status == FriendshipStatus.Requested).FirstOrDefaultAsync();

        if (request == null) return false;

        var updateDefinition = Builders<Friendship>.Update.Set(f => f.Status, FriendshipStatus.Ignored);

        var result = await _friendshipCollection.UpdateOneAsync(f => f.Id == request.Id, updateDefinition);

        return result.ModifiedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveFriendAsync(string userId, string friendId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            (f.UserId == userId && f.FriendId == friendId && f.Status == FriendshipStatus.Accepted) ||
            (f.UserId == friendId && f.FriendId == userId && f.Status == FriendshipStatus.Accepted));

        return result.DeletedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> UnblockUserAsync(string userId, string blockedUserId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            f.UserId == userId && f.FriendId == blockedUserId &&
            f.Status == FriendshipStatus.Blocked);

        return result.DeletedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> UnignoreUserAsync(string userId, string ignoredUserId)
    {
        var result = await _friendshipCollection.DeleteManyAsync(f =>
            f.UserId == userId && f.FriendId == ignoredUserId &&
            f.Status == FriendshipStatus.Ignored);

        return result.DeletedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetFriendIdsAsync(string userId)
    {
        var friendIds = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Accepted)
            .Project(f => f.FriendId)
            .ToListAsync();

        return friendIds;
    }

    /// <inheritdoc/>
    public async Task<List<Friendship>> GetPendingRequestsAsync(string userId)
    {
        var pendingRequests = await _friendshipCollection.Find(f =>
            f.FriendId == userId && f.Status == FriendshipStatus.Requested)
            .ToListAsync();

        return pendingRequests;
    }

    /// <inheritdoc/>
    public async Task<List<Friendship>> GetAwaitingRequestsAsync(string userId)
    {
        var waitingRequests = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Requested)
            .ToListAsync();

        return waitingRequests;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetBlockedUserIdsAsync(string userId)
    {
        var blockedFriendIds = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Blocked)
            .Project(f => f.FriendId)
            .ToListAsync();

        return blockedFriendIds;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetIgnoredUserIdsAsync(string userId)
    {
        var ignoredFrienIds = await _friendshipCollection.Find(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Ignored)
            .Project(f => f.FriendId)
            .ToListAsync();

        return ignoredFrienIds;
    }

    /// <inheritdoc/>
    public async Task<bool> AreFriendsAsync(string userId1, string userId2)
    {
        var friendship = await _friendshipCollection.Find(f =>
            f.UserId == userId1 && f.FriendId == userId2 &&
            f.Status == FriendshipStatus.Accepted).FirstOrDefaultAsync();

        return friendship != null;
    }

    /// <inheritdoc/>
    public async Task<FriendshipStatus?> GetFriendshipStatusAsync(string userId1, string userId2)
    {
        var friendship = await _friendshipCollection.Find(f =>
            f.UserId == userId1 && f.FriendId == userId2).FirstOrDefaultAsync();

        return friendship?.Status;
    }

    /// <inheritdoc/>
    public async Task<bool> AreFriendsOfFriendsAsync(string userId1, string userId2)
    {
        var user1FriendIds = await GetFriendIdsAsync(userId1);
        if (user1FriendIds.Contains(userId2)) return true; // They are direct friends

        var user2FriendIds = await GetFriendIdsAsync(userId2);
        return user1FriendIds.Any(f => user2FriendIds.Contains(f));
    }

    /// <inheritdoc/>
    public async Task<HashSet<string>> GetFriendsOfFriendIdsAsync(string userId)
    {
        var directFriendIds = await GetFriendIdsAsync(userId);

        var indirectFriendIds = await _friendshipCollection.Find(f =>
            directFriendIds.Contains(f.UserId) && f.Status == FriendshipStatus.Accepted)
            .Project(x => x.FriendId)
            .ToListAsync();

        var result = new HashSet<string>(directFriendIds.Union(indirectFriendIds));
        result.Remove(userId); // Ignore the user itself
        return result;
    }
}