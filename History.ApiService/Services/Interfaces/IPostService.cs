using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

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
    /// Get post by id.
    /// </summary>
    /// <param name="postReactionId">The id of post reaction to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of post reaction</returns>
    public Task<Result<PostReaction>> GetPostReactionByIdAsync(string postReactionId);

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
    /// <param name="userId">The id of user to get posts</param>
    /// <param name="requesterId">The id of user who requests posts</param>
    /// <param name="fromPostId">The id of post to start from</param>
    /// <param name="limit">The limit of posts to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of posts</returns>
    public Task<Result<List<Post>>> GetUserPostsAsync(string userId, string requesterId = null, string fromPostId = null, int limit = 10);

    /// <summary>
    /// Get count of posts of user.
    /// </summary>
    /// <param name="userId">The id of user to get post count</param>
    /// <param name="requesterId">The id of user who requests post count</param>
    /// <returns>A task that represents the asynchronous operation. with result of post count</returns>
    public Task<Result<long>> GetUserPostsCountAsync(string userId, string requesterId = null);

    /// <summary>
    /// Asynchronously ignores a specified post for a user. This operation updates the user's preferences regarding the
    /// visibility of the post.
    /// </summary>
    /// <param name="postId">Specifies the post that the user wishes to ignore.</param>
    /// <param name="userId">Identifies the user who wants to ignore the post.</param>
    /// <returns>Returns a task that represents the asynchronous operation, containing the result of the ignore action.</returns>
    public Task<Result> IgnorePostAsync(string postId, string userId);

    /// <summary>
    /// Asynchronously writes a post using the provided user information, request details, and associated files.
    /// </summary>
    /// <param name="userId">Identifies the user who is creating the post.</param>
    /// <param name="requestDto">Contains the details and content of the post being created.</param>
    /// <param name="files">Represents the files that are to be uploaded along with the post.</param>
    /// <returns>Returns a task that represents the asynchronous operation, yielding a result indicating success or failure.</returns>
    public Task<Result> WritePostAsync(string userId, WritePostRequestDto requestDto, IEnumerable<IFormFile> files);

    /// <summary>
    /// Asynchronously modifies a post based on the provided details and files.
    /// </summary>
    /// <param name="postId">Specifies the unique identifier of the post to be modified.</param>
    /// <param name="userId">Identifies the user making the modification request.</param>
    /// <param name="requestDto">Contains the new data and settings for the post modification.</param>
    /// <param name="files">Holds any files that need to be associated with the post during the modification.</param>
    /// <returns>Provides the result of the modification operation.</returns>
    public Task<Result> ModifyPostAsync(string postId, string userId, ModifyPostRequestDto requestDto, IEnumerable<IFormFile> files);

    /// <summary>
    /// Asynchronously deletes a post associated with a specific user.
    /// </summary>
    /// <param name="postId">Specifies the unique identifier of the post to be removed.</param>
    /// <param name="userId">Identifies the user who owns the post to be deleted.</param>
    /// <returns>Returns a task that represents the asynchronous operation, containing the result of the deletion.</returns>
    public Task<Result> DeletePostAsync(string postId, string userId);

    /// <summary>
    /// Asynchronously reposts a specified post for a user.
    /// </summary>
    /// <param name="postId">Specifies the post that is being reposted.</param>
    /// <param name="userId">Identifies the user who is reposting the content.</param>
    /// <returns>Returns a task that represents the result of the repost operation.</returns>
    public Task<Result> RepostPostAsync(string postId, string userId);

    /// <summary>
    /// Handles the reaction to a post by a user asynchronously.
    /// Adds reaction if not already present, or removes it if it exists.
    /// </summary>
    /// <param name="postId">Specifies the post that is being reacted to.</param>
    /// <param name="userId">Identifies the user who is reacting to the post.</param>
    /// <param name="type">Indicates the type of reaction being made to the post.</param>
    /// <returns>Returns a task that represents the asynchronous operation, yielding a result of the reaction handling.</returns>
    public Task<Result> HandlePostReactionAsync(string postId, string userId, PostReactionType type);

    /// <summary>
    /// Searches for posts based on a specified query and returns a list of matching posts.
    /// </summary>
    /// <param name="query">The search term used to find relevant posts.</param>
    /// <param name="requesterId">Identifies the user making the search request.</param>
    /// <param name="fromPostId">Specifies the starting point for the search, allowing for pagination.</param>
    /// <param name="limit">Determines the maximum number of posts to return in the search results.</param>
    /// <returns>A task that resolves to a result containing a list of posts that match the search criteria.</returns>
    public Task<Result<List<Post>>> SearchPostsAsync(string query, string requesterId, string fromPostId = null, int limit = 10);


    /// <summary>
    /// Changes the discovery option for a specified post.
    /// </summary>
    /// <param name="postId">The id of the post for which the discovery option is being changed.</param>
    /// <param name="userId">The id of the user who is changing the discovery option.</param>
    /// <param name="discoveryOption">The new discovery option to be set for the post.</param>
    /// <param name="selectedUserIds">If the discovery option is set to selected users, this list contains the ids of the users who are selected.</param>
    /// <returns></returns>
    public Task<Result> ChangeDiscoveryOptionAsync(string postId, string userId, DiscoveryOption discoveryOption, List<string> selectedUserIds);

    /// <summary>
    /// Fills the external URL content.
    /// </summary>
    /// <param name="externalUrlContent">The external URL content to fill.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the fill operation.</returns>
    public Task<Result> FillExternalUrlContentAsync(ExternalUrlContent externalUrlContent);

    /// <summary>
    /// Checks if a user has access to a specific post asynchronously.
    /// </summary>
    /// <param name="postId">Identifies the post for which access is being verified.</param>
    /// <param name="requesterId">Identifies the user requesting access to the post.</param>
    /// <returns>Returns a task that resolves to a result indicating access permissions.</returns>
    public Task<Result> CheckAccessAsync(string postId, string requesterId);

    /// <summary>
    /// Checks if a user has access to a specific post asynchronously.
    /// </summary>
    /// <param name="post">The post object containing the details for which access is being verified.</param>
    /// <param name="requesterId">Identifies the user requesting access to the post.</param>
    /// <returns>Returns a task that resolves to a result indicating access permissions.</returns>
    public Task<Result> CheckAccessAsync(Post post, string requesterId);

    /// <summary>
    /// Generates a response data transfer object for a given post asynchronously.
    /// </summary>
    /// <param name="post">The post object containing the details for which the response is generated.</param>
    /// <param name="requesterId">Identifies the user making the request for the post response.</param>
    /// <returns>Returns a task that resolves to a result containing the post response data transfer object.</returns>
    public Task<Result<PostResponseDto>> GeneratePostResponseDtoAsync(Post post, string requesterId);

    /// <summary>
    /// Generates a list of response data transfer objects for the provided posts asynchronously.
    /// </summary>
    /// <param name="posts">A collection of posts for which response data transfer objects will be created.</param>
    /// <param name="requesterId">Identifies the user making the request for the post responses.</param>
    /// <returns>A task that resolves to a result containing a list of post response data transfer objects.</returns>
    public Task<Result<List<PostResponseDto>>> GeneratePostResponseDtosAsync(List<Post> posts, string requesterId);
}
