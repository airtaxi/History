using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Dto;

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
    public Task<Result<List<UserResponseDto>>> GenerateUserResponseDtoAsync(IEnumerable<User> user, string requesterId = null);

    /// <summary>
    /// Generates a UserResponseDto for a user.
    /// </summary>
    /// <param name="userId">The ID of the user to generate the DTO for.</param>
    /// <param name="requesterId">The ID of the user requesting the DTO.</param>
    /// <returns>A task representing the asynchronous operation, with the UserResponseDto.</returns>
    public Task<Result<List<UserResponseDto>>> GenerateUserResponseDtoAsync(IEnumerable<string> userId, string requesterId = null);
}
