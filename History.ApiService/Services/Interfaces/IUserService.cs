using History.Commons.DataTypes;

namespace History.ApiService.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Create user
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task CreateUserAsync(User user);

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">The ID of user to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of user</returns>
    public Task<User> GetUserByIdAsync(string userId);

    /// <summary>
    /// Update user's description
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="description">The description to update</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<bool> UpdateDescriptionAsync(string userId, string description);

    /// <summary>
    /// Update user's birthday 
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="birthday">The birthday to update. Null if user did not set or don't want to</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<bool> UpdateBirthdayAsync(string userId, DateTime? birthday);

    /// <summary>
    /// Update user's nickname
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="nickname">The nickname to update</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<bool> UpdateNicknameAsync(string userId, string nickname);

    /// <summary>
    /// Update user's profile media
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="image">The image to update. Null if user want to delete profile media</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<bool> UpdateProfileMediaAsync(string userId, byte[] image);

    /// <summary>
    /// Update user's background media
    /// </summary>
    /// <param name="userId">The ID of user to update</param>
    /// <param name="image">The image to update. Null if user want to delete background media</param>
    /// <returns>A task that represents the asynchronous operation. with result of update success</returns>
    public Task<bool> UpdateBackgroundMediaAsync(string userId, byte[] image);
}
