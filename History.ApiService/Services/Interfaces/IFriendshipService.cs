using History.Commons;
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
    public Task<Result> SendFriendRequestAsync(string senderId, string receiverId);

    /// <summary>
    /// Accepts a friend request.
    /// </summary>
    /// <param name="userId">The ID of the user accepting the request.</param>
    /// <param name="userIdToAccept">The ID of the user to be accepted.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> AcceptFriendRequestAsync(string userId, string userIdToAccept);

    /// <summary>
    /// Declines a friend request.
    /// </summary>
    /// <param name="userId">The ID of the user declining the request.</param>
    /// <param name="userIdToDecline">The ID of the user to be declined.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> DeclineFriendRequestAsync(string userId, string userIdToDecline);

    /// <summary>
    /// Cancels a friend request.
    /// </summary>
    /// <param name="userId">The ID of the user canceling the request.</param>
    /// <param name="userIdToCancel">The ID of the user to be canceled.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> CancelFriendRequestAsync(string userId, string userIdToCancel);

    /// <summary>
    /// Blocks a user.
    /// </summary>
    /// <param name="userId">The ID of the user performing the block.</param>
    /// <param name="userIdToBlock">The ID of the user to be blocked.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> BlockUserAsync(string userId, string userIdToBlock);

    /// <summary>
    /// Ignores a friend request.
    /// </summary>
    /// <param name="userId">The ID of the user ignoring the request.</param>
    /// <param name="friendId">The ID of the user to be ignored.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> IgnoreUserAsync(string userId, string friendId);

    /// <summary>
    /// Removes a friend from the user's friend list.
    /// </summary>
    /// <param name="userId">The ID of the user removing the friend.</param>
    /// <param name="userIdToRemove">The ID of the user to be removed from the friend list.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> RemoveFriendAsync(string userId, string userIdToRemove);

    /// <summary>
    /// Unblocks a previously blocked user.
    /// </summary>
    /// <param name="userId">The ID of the user performing the unblock.</param>
    /// <param name="blockedUserId">The ID of the user to be unblocked.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> UnblockUserAsync(string userId, string blockedUserId);

    /// <summary>
    /// Unignores a previously ignored user.
    /// </summary>
    /// <param name="userId">The ID of the user performing the unignore.</param>
    /// <param name="ignoredUserId">The ID of the user to be unignored.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
    public Task<Result> UnignoreUserAsync(string userId, string ignoredUserId);

    /// <summary>
    /// Gets all friend's IDs for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose friends are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of friend IDs.</returns>
    public Task<Result<List<string>>> GetFriendIdsAsync(string userId);

    /// <summary>
    /// Gets all pending friend requests for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose pending requests are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of pending friend requests.</returns>
    public Task<Result<List<Friendship>>> GetPendingRequestsAsync(string userId);

    /// <summary>
    /// Gets all sent friend requests for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose awaiting requests are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of sent friend requests.</returns>
    public Task<Result<List<Friendship>>> GetSentRequestsAsync(string userId);

    /// <summary>
    /// Gets all friendships for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose friendships are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of friendships.</returns>
    public Task<Result<List<Friendship>>> GetAllFriendshipsAsync(string userId);

    /// <summary>
    /// Retrieves a list of user IDs that are blocked, blocked by, or ignored by the specified user.
    /// </summary>
    /// <param name="userId">Identifies the user for whom the banned or ignored user IDs are being retrieved.</param>
    /// <returns>A list of strings representing the IDs of banned or ignored users.</returns>
    public Task<Result<List<string>>> GetBannedUserIdsAsync(string userId);

    /// <summary>
    /// Gets all blocked user's IDs for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose blocked list is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of blocked user IDs.</returns>
    public Task<Result<List<string>>> GetBlockedUserIdsAsync(string userId);

    /// <summary>
    /// Get all user IDs that blocked the user.
    /// </summary>
    /// <param name="userId">The ID of the user whose blocker list is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of blocker user IDs.</returns>
    public Task<Result<List<string>>> GetBlockerUserIdsAsync(string userId);

    /// <summary>
    /// Get all ignored user's IDs for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose ignored list is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of ignored user IDs.</returns>
    public Task<Result<List<string>>> GetIgnoredUserIdsAsync(string userId);

    /// <summary>
    /// Checks if two users are friends.
    /// </summary>
    /// <param name="userId">The ID of the first user.</param>
    /// <param name="friendId">The ID of the second user.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating if they are friends.</returns>
    public Task<Result<bool>> AreFriendsAsync(string userId, string friendId);

    /// <summary>
    /// Gets the friendship between two users.
    /// </summary>
    /// <param name="userId">The ID of the first user.</param>
    /// <param name="friendId">The ID of the second user.</param>
    /// <returns>A task representing the asynchronous operation, with the friendship.</returns>
    public Task<Result<Friendship>> GetFriendshipAsync(string userId, string friendId);

    /// <summary>
    /// Checks if two users are connected through friends of friends. (Include user's friends)
    /// </summary>
    /// <param name="userId">The ID of the first user.</param>
    /// <param name="friendId">The ID of the second user.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating if they are friends of friends.</returns>
    public Task<Result<bool>> AreFriendsOfFriendsAsync(string userId, string friendId);

    /// <summary>
    /// Gets the IDs of friends of friends for a user. (Include user's friends)
    /// </summary>
    /// <param name="userId">The ID of the user whose friends of friends are to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with a collection of friend IDs.</returns>
    public Task<Result<HashSet<string>>> GetFriendsOfFriendIdsAsync(string userId);

    /// <summary>
    /// Gets the count of friends for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose friend count is to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation, with the count of friends.</returns>
    public Task<Result<long>> GetUserFriendCountAsync(string userId);

    /// <summary>
    /// Retrieves a list of user IDs that are friends with the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user whose friends are being retrieved.</param>
    /// <param name="requesterId">The ID of the user making the request.</param>
    /// <returns>A task that represents the asynchronous operation, containing a result of a list of friend IDs.</returns>
    public Task<Result<List<string>>> GetUserFriendIdsAsync(string userId, string requesterId);
}
