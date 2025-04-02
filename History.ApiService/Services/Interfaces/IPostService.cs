using History.Commons;
using History.Commons.DataTypes;

namespace History.ApiService.Services.Interfaces;

public interface IPostService
{
    /// <summary>
    /// Get post by id.
    /// </summary>
    /// <param name="postId">The id of post to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of post</returns>
    public Task<Result<Post>> GetPostByIdAsync(string postId);

    /// <summary>
    /// Get timeline posts of user.
    /// </summary>
    /// <param name="userId">The id of user to get timeline posts</param>
    /// <param name="fromPostId">The id of post to start from</param>
    /// <param name="limit">The limit of posts to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of posts</returns>
    public Task<Result<List<Post>>> GetTimelinePostsAsync(string userId, string fromPostId = null, int limit = 10);

    /// <summary>
    /// Get posts of user.
    /// </summary>
    /// <param name="requesterId">The id of user who requests posts</param>
    /// <param name="userId">The id of user to get posts</param>
    /// <param name="fromPostId">The id of post to start from</param>
    /// <param name="limit">The limit of posts to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of posts</returns>
    public Task<Result<List<Post>>> GetUserPostsAsync(string requesterId, string userId, string fromPostId = null, int limit = 10);

    /// <summary>
    /// Get count of posts of user.
    /// </summary>
    /// <param name="userId">The id of user to get post count</param>
    /// <param name="requesterId">The id of user who requests post count</param>
    /// <returns>A task that represents the asynchronous operation. with result of post count</returns>
    public Task<Result<long>> GetUserPostCountAsync(string userId, string requesterId = null);
}
