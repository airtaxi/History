using History.Commons.DataTypes;
using History.Commons.Enums;

namespace History.ApiService.Services.Interfaces;

/// <summary>
/// Service for managing friendship relationships between users.
/// </summary>
public interface IFriendshipService
{
    /// <summary>
    /// Sends a friend request from one user to another.
    /// </summary>
    /// <param name="senderId">The ID of the user sending the request.</param>
    /// <param name="receiverId">The ID of the user receiving the request.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> SendFriendRequestAsync(string senderId, string receiverId);

    /// <summary>
    /// Accepts a friend request.
    /// </summary>
    /// <param name="userId">The ID of the user accepting the request.</param>
    /// <param name="requesterId">The ID of the user who sent the request.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> AcceptFriendRequestAsync(string userId, string requesterId);

    /// <summary>
    /// Declines a friend request.
    /// </summary>
    /// <param name="userId">The ID of the user declining the request.</param>
    /// <param name="requesterId">The ID of the user who sent the request.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> DeclineFriendRequestAsync(string userId, string requesterId);

    /// <summary>
    /// Blocks a user.
    /// </summary>
    /// <param name="userId">The ID of the user performing the block.</param>
    /// <param name="userToBlockId">The ID of the user to be blocked.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> BlockUserAsync(string userId, string userToBlockId);

    /// <summary>
    /// Ignores a friend request.
    /// </summary>
    /// <param name="userId">The ID of the user ignoring the request.</param>
    /// <param name="requesterId">The ID of the user who sent the request.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> IgnoreRequestAsync(string userId, string requesterId);

    /// <summary>
    /// Removes a friend from the user's friend list.
    /// </summary>
    /// <param name="userId">The ID of the user removing the friend.</param>
    /// <param name="friendId">The ID of the friend to be removed.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> RemoveFriendAsync(string userId, string friendId);

    /// <summary>
    /// Unblocks a previously blocked user.
    /// </summary>
    /// <param name="userId">The ID of the user performing the unblock.</param>
    /// <param name="blockedUserId">The ID of the user to be unblocked.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> UnblockUserAsync(string userId, string blockedUserId);

    /// <summary>
    /// Unignores a previously ignored user.
    /// </summary>
    /// <param name="userId">The ID of the user performing the unignore.</param>
    /// <param name="ignoredUserId">The ID of the user to be unignored.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<bool> UnignoreUserAsync(string userId, string ignoredUserId);

    /// <summary>
    /// Gets all friend's IDs for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose friends are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of friend IDs.</returns>
    public Task<List<string>> GetFriendIdsAsync(string userId);

    /// <summary>
    /// Gets all pending friend requests for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose pending requests are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of pending friend requests.</returns>
    public Task<List<Friendship>> GetPendingRequestsAsync(string userId);

    /// <summary>
    /// Gets all awaiting friend requests for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose awaiting requests are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of waiting friend requests.</returns>
    public Task<List<Friendship>> GetAwaitingRequestsAsync(string userId);

    /// <summary>
    /// Gets all blocked user's IDs for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose blocked list is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of blocked user IDs.</returns>
    public Task<HashSet<string>> GetBlockedUserIdsAsync(string userId);

    /// <summary>
    /// Get all ignored user's IDs for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose ignored list is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of ignored user IDs.</returns>
    public Task<HashSet<string>> GetIgnoredUserIdsAsync(string userId);

    /// <summary>
    /// Checks if two users are friends.
    /// </summary>
    /// <param name="userId1">The ID of the first user.</param>
    /// <param name="userId2">The ID of the second user.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating if they are friends.</returns>
    public Task<bool> AreFriendsAsync(string userId1, string userId2);

    /// <summary>
    /// Gets the friendship status between two users.
    /// </summary>
    /// <param name="userId1">The ID of the first user.</param>
    /// <param name="userId2">The ID of the second user.</param>
    /// <returns>A task representing the asynchronous operation, with the friendship status.</returns>
    public Task<FriendshipStatus?> GetFriendshipStatusAsync(string userId1, string userId2);

    /// <summary>
    /// Checks if two users are connected through friends of friends. (Include user's friends)
    /// </summary>
    /// <param name="userId1">The ID of the first user.</param>
    /// <param name="userId2">The ID of the second user.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating if they are friends of friends.</returns>
    public Task<bool> AreFriendsOfFriendsAsync(string userId1, string userId2);

    /// <summary>
    /// Gets the IDs of friends of friends for a user. (Include user's friends)
    /// </summary>
    /// <param name="userId">The ID of the user whose friends of friends are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of friend IDs.</returns>
    public Task<HashSet<string>> GetFriendsOfFriendIdsAsync(string userId);
}
