using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;

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
    public Task<Result<long>> GetUserPostsCountAsync(string userId, string requesterId = null);

    /// <summary>
    /// Generates a list of response data transfer objects for the provided posts asynchronously.
    /// </summary>
    /// <param name="posts">A collection of posts for which response data transfer objects will be created.</param>
    /// <param name="requesterId">Identifies the user making the request for the post responses.</param>
    /// <returns>A task that resolves to a result containing a list of post response data transfer objects.</returns>
    public Task<Result<List<PostResponseDto>>> GeneratePostResponsesDtosAsync(List<Post> posts, string requesterId);

    /// <summary>
    /// Generates a response data transfer object for a given post asynchronously.
    /// </summary>
    /// <param name="post">The post object contains the details of the post for which the response is being generated.</param>
    /// <param name="bannedUserIds">A list of user IDs that are prohibited from interacting with the post.</param>
    /// <returns>Returns a task that resolves to a result containing the post response data transfer object.</returns>
    public Task<Result<PostResponseDto>> GeneratePostResponseDtoAsync(Post post, List<string> bannedUserIds);
}
