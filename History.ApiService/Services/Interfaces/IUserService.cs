using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;

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
    /// Update user's profile media
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="image">The image to update. Null if user want to delete profile media</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateProfileMediaAsync(string userId, byte[] image);

    /// <summary>
    /// Update user's background media
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="image">The image to update. Null if user want to delete background media</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<Result> UpdateBackgroundMediaAsync(string userId, byte[] image);

    /// <summary>
    /// Generates a UserResponseDto for a user.
    /// </summary>
    /// <param name="user">The user to generate the DTO for.</param>
    /// <param name="requesterId">The ID of the user requesting the DTO.</param>
    /// <returns>A task representing the asynchronous operation, with the UserResponseDto.</returns>
    public Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(User user, string requesterId = null);

    /// <summary>
    /// Generates a UserResponseDto for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to generate the DTO for.</param>
    /// <param name="requesterId">The ID of the user requesting the DTO.</param>
    /// <returns>A task representing the asynchronous operation, with the UserResponseDto.</returns>
    public Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(string userId, string requesterId = null);

    /// <summary>
    /// Generates a UserResponseDto for a user.
    /// </summary>
    /// <param name="user">The user to generate the DTO for.</param>
    /// <param name="requesterId">The ID of the user requesting the DTO.</param>
    /// <returns>A task representing the asynchronous operation, with the UserResponseDto.</returns>
    public Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<User> user, string requesterId = null);

    /// <summary>
    /// Generates a UserResponseDto for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to generate the DTO for.</param>
    /// <param name="requesterId">The ID of the user requesting the DTO.</param>
    /// <returns>A task representing the asynchronous operation, with the UserResponseDto.</returns>
    public Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<string> userId, string requesterId = null);
}
