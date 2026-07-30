using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.ApiService.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Create user
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task<Result> CreateUserAsync(User user);

    /// <summary>
    /// Deletes a user by ID. Used for rollback when invite code consumption fails after user creation.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task<Result> DeleteUserAsync(string userId);

    public Task<bool> IsUserCollectionEmptyAsync();

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">The ID of user to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of user</returns>
    public Task<Result<User>> GetUserByIdAsync(string userId);

    /// <summary>
    /// Get users by IDs
    /// </summary>
    /// <param name="userIds">The IDs of users to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of users</returns>
    public Task<Result<List<User>>> GetUsersByIdsAsync(IEnumerable<string> userIds);

    /// <summary>
    /// Get users by birthday
    /// </summary>
    /// <param name="birthday">The birthday to search for</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of users with the specified birthday</returns>
    public Task<Result<List<User>>> GetUsersByBirthdayAsync(DateTime birthday);

    /// <summary>
    /// Get user by handle
    /// </summary>
    /// <param name="handle">The handle of the user to retrieve</param>
    /// <param name="applyPermission">If true, apply user's AllowSearch property</param>
    /// <returns>A task that represents the asynchronous operation, with result of the user</returns>
    public Task<Result<User>> GetUserByHandleAsync(string handle, bool applyPermission);

    /// <summary>
    /// Find users by nickname
    /// </summary>
    /// <param name="query">The nickname to search for</param>
    /// <param name="applyPermission">If true, apply user's AllowSearch property</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of users matching the nickname</returns>
    public Task<Result<List<User>>> FindUsersByNicknameAsync(string query, bool applyPermission, int limit = -1);

    /// <summary>
    /// Approves a user who is not authorized
    /// </summary>
    /// <param name="userId">The identifier of the user to be approved</param>
    /// <returns>A task that represents the asynchronous operation, with result of the approval success</returns>
    public Task<Result> ApproveUnauthorizedUserAsync(string userId);

    /// <summary>
    /// Removes approval for a user who is not authorized.
    /// </summary>
    /// <param name="userId">Identifies the user whose approval is being revoked.</param>
    /// <returns>Returns a task that represents the asynchronous operation, containing the result of the unapproval action.</returns>
    public Task<Result> UnapproveUnauthorizedUserAsync(string userId);

    /// <summary>
    /// Promotes a user to moderator rank.
    /// </summary>
    /// <param name="userId">Identifies the user to be promoted to moderator.</param>
    /// <returns>Returns a task that represents the asynchronous operation, containing the result of the promotion.</returns>
    public Task<Result> MakeUserModeratorAsync(string userId);

    /// <summary>
    /// Retrieves a list of users who have not yet been approved. The number of users returned can be limited based on
    /// the specified criteria.
    /// </summary>
    /// <param name="limit">Specifies the maximum number of unapproved users to retrieve.</param>
    /// <param name="fromUserId">Identifies the user from whom the unapproved users are being fetched.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of unapproved users.</returns>
    public Task<Result<List<User>>> GetUnauthorizedUsersAsync(int limit = 10, string fromUserId = null);

    /// <summary>
    /// Retrieves a list of moderators based on specified criteria.
    /// </summary>
    /// <param name="limit">Specifies the maximum number of moderators to return.</param>
    /// <param name="fromUserId">Identifies the user from whom to retrieve the moderators.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of user objects.</returns>
    public Task<Result<List<User>>> GetModeratorsAsync(int limit = 10, string fromUserId = null);

    /// <summary>
    /// Retrieves a list of user IDs for all moderators.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, containing a list of moderator user IDs.</returns>
    public Task<Result<List<string>>> GetModeratorIdsAsync();

    /// <summary>
    /// Update user's description
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="description">The description to update</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateDescriptionAsync(string userId, string description);

    /// <summary>
    /// Update user's birthday 
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="birthday">The birthday to update. Null if user did not set or don't want to</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateBirthdayAsync(string userId, DateTime? birthday);

    /// <summary>
    /// Update user's nickname
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="nickname">The nickname to update</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateNicknameAsync(string userId, string nickname);

    /// <summary>
    /// Update user's AllowSearch property
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="allowSearch">The value indicating whether the user can be searched or not</param>
    /// <returns>A task that represents the asynchronous operation, with result of update success</returns>
    public Task<Result> UpdateAllowSearchAsync(string userId, bool allowSearch);

    /// <summary>
    /// Update user's FriendListDiscoveryOption property
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="discoveryOption">The new discovery option</param>
    /// <returns>A task that represents the asynchronous operation, with result of update success</returns>
    public Task<Result> UpdateFriendListDiscoveryOptionAsync(string userId, DiscoveryOption discoveryOption);

    /// <summary>
    /// Update user's handle
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="newHandle">The new handle to update</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateHandleAsync(string userId, string newHandle);

    /// <summary>
    /// Update user's profile media
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="image">The image to update. Null if user want to delete profile media</param>
    /// <param name="contentType">The MIME type of the image (e.g. "image/webp"). Used to determine animated WebP handling.</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateProfileMediaAsync(string userId, byte[] image, string contentType = null);

    /// <summary>
    /// Update user's background media
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="image">The image to update. Null if user want to delete background media</param>
    /// <param name="contentType">The MIME type of the image (e.g. "image/webp"). Used to determine animated WebP handling.</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateBackgroundMediaAsync(string userId, byte[] image, string contentType = null);

    /// <summary>
    /// Update user's profile thumbnail media
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="pinnedPostId">The ID of the pinned post to update. Null if user want to delete profile thumbnail media</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdatePinnedPostAsync(string userId, string pinnedPostId);

    /// <summary>
    /// Adds a memo for a user, which can be used to store additional information about the user.
    /// </summary>
    /// <param name="userId">The ID of the user for whom the memo is being added.</param>
    /// <param name="registerId">The ID of the user who is adding the memo</param>
    /// <param name="memo">The content of the memo to be added.</param>
    /// <returns>A task that represents the asynchronous operation, with a result indicating the success or failure of the memo addition.</returns>
    public Task<Result> UpdateMemoAsync(string userId, string requesterId, string memo);

    /// <summary>
    /// Updates the push notification permission for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose permission is being updated.</param>
    /// <param name="type">The type of push notification for which the permission is being updated.</param>
    /// <param name="accessPermission">The new access permission to be set for the user.</param>
    /// <returns>A task that represents the asynchronous operation, with a result indicating the success or failure of the update operation.</returns>
    public Task<Result> UpdatePushNotificationPermissionAsync(string userId, PushNotificationType type, AccessPermission accessPermission);

    /// <summary>
    /// Updates the message receiving permission for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose permission is being updated.</param>
    /// <param name="accessPermission">The new access permission to be set for the user.</param>
    /// <returns>A task that represents the asynchronous operation, with a result indicating the success or failure of the update operation.</returns>
    public Task<Result> UpdateMessageReceivingPermissionAsync(string userId, AccessPermission accessPermission);

    /// <summary>
    /// Withdraws a user's account, deleting their data.
    /// </summary>
    /// <param name="userId">The ID of the user whose account is to be withdrawn.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the withdrawal.</returns>
    public Task<Result> WithdrawAsync(string userId);

    /// <summary>
    /// Filters a list of user IDs to only include those that are allowed to be searched by the user.
    /// </summary>
    /// <param name="userIds">The list of user IDs to filter.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing a list of user IDs that are allowed to be searched.</returns>
    public Task<Result<List<string>>> FilterAllowSearch(List<string> userIds);

    /// <summary>
    /// Filters push notification permissions for a given type and list of user IDs.
    /// </summary>
    /// <param name="userIds">The list of user IDs to filter permissions for.</param>
    /// <param name="type">The type of push notification to filter permissions for.</param>
    /// <returns>A task that represents the asynchronous operation, with a result containing a list of user IDs that are allowed to send the push notification</returns>
    public Task<Result<List<string>>> FilterPushNotificationPermissionsAsync(string userId, IEnumerable<string> recipients, PushNotificationType type);

    /// <summary>
    /// Generate text preview asynchronously based on the provided contents.
    /// </summary>
    /// <param name="contents">The contents of post or comments to generate preview from.</param>
    /// <param name="requesterId">Identifies the entity making the request, which can be optional.</param>
    /// <returns>A task that resolves to a string containing the generated text preview.</returns>
    public Task<string> GenerateTextPreviewFromContentsAsync(IEnumerable<BaseContent> contents, string requesterId = null);

    /// <summary>
    /// Generates a UserResponseDto asynchronously based on the provided user information.
    /// </summary>
    /// <param name="user">Contains the details of the user for whom the response DTO is being generated.</param>
    /// <param name="requesterId">Identifies the entity making the request, which can be optional.</param>
    /// <returns>Returns a task that resolves to a Result containing the UserResponseDto.</returns>
    public Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(User user, string requesterId = null);

    /// <summary>
    /// Generates a user response data transfer object asynchronously.
    /// </summary>
    /// <param name="userId">Identifies the user for whom the response data is generated.</param>
    /// <param name="requesterId">Optionally identifies the requester of the user response data.</param>
    /// <returns>Returns a task that resolves to a result containing the user response data.</returns>
    public Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(string userId, string requesterId = null);

    /// <summary>
    /// Generates a list of user response data transfer objects asynchronously.
    /// </summary>
    /// <param name="users">A collection of user entities to be converted into response DTOs.</param>
    /// <param name="requesterId">An optional identifier for the entity making the request.</param>
    /// <returns>A task that resolves to a result containing a list of user response DTOs.</returns>
    public Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<User> users, string requesterId = null);

    /// <summary>
    /// Generates a list of user response data transfer objects asynchronously based on provided user identifiers.
    /// </summary>
    /// <param name="userIds">A collection of identifiers for users whose response data is to be generated.</param>
    /// <param name="requesterId">An optional identifier for the entity requesting the user response data.</param>
    /// <returns>A task that resolves to a result containing a list of user response data transfer objects.</returns>
    public Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<string> userIds, string requesterId = null);
}
