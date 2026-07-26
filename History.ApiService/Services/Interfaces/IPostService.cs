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
    /// Get public posts of user
    /// </summary>
    /// <param name="userId">The id of user to get public posts</param>
    /// <param name="fromPostId">The id of post to start from</param>
    /// <param name="limit">The limit of posts to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of posts</returns>
    public Task<Result<List<Post>>> GetPublicPostsAsync(string userId, string fromPostId = null, int limit = 10);

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
    /// <returns>Returns a task that represents the asynchronous operation, yielding the created post on success or a failure result.</returns>
    public Task<Result<Post>> WritePostAsync(string userId, WritePostRequestDto requestDto, IEnumerable<IFormFile> files);

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
    /// <param name="requeesterId">The identifier of the user requesting the deletion.</param>
    /// <returns>Returns a task that represents the asynchronous operation, containing the result of the deletion.</returns>
    public Task<Result> DeletePostAsync(string postId, string requeesterId);

    /// <summary>
    /// Asynchronously reposts or un-repost a specified post for a user.
    /// </summary>
    /// <param name="postId">Specifies the post that is being reposted.</param>
    /// <param name="requesterId">The identifier of the user who is performing the repost operation.</param>
    /// <returns>Returns a task that represents the result of the repost operation.</returns>
    public Task<Result> HandleRepostAsync(string postId, string requesterId);

    /// <summary>
    /// Handles the reaction to a post by a user asynchronously.
    /// Adds reaction if not already present, or removes it if it exists.
    /// </summary>
    /// <param name="postId">Specifies the post that is being reacted to.</param>
    /// <param name="userId">Identifies the user who is reacting to the post.</param>
    /// <param name="type">Indicates the type of reaction being made to the post.</param>
    /// <returns>Returns a task that represents the asynchronous operation, yielding a result of the reaction handling.</returns>
    public Task<Result> HandlePostReactionAsync(string postId, string userId, ReactionType type);

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
    /// Create public post from existing post
    /// </summary>
    /// <param name="postId">The id of the post to create public post from</param>
    /// <param name="requesterId">The id of the user who requests to create public post</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the public post creation.</returns>
    public Task<Result> WritePublicPostAsync(string postId, string requesterId);

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
    /// Checks if a user has access to comment on a specific post asynchronously.
    /// </summary>
    /// <param name="postId">The unique identifier of the post for which comment access is being checked.</param>
    /// <param name="requesterId">The unique identifier of the user requesting access to comment on the post.</param>
    /// <returns>A task that resolves to a result indicating whether the user has permission to comment on the post.</returns>
    public Task<Result> CheckCommentAccessAsync(string postId, string requesterId);

    /// <summary>
    /// Checks if a user has access to comment on a specific post asynchronously.
    /// </summary>
    /// <param name="post">The post object containing the details for which comment access is being checked.</param>
    /// <param name="requesterId">The unique identifier of the user requesting access to comment on the post.</param>
    /// <returns>A task that resolves to a result indicating whether the user has permission to comment on the post.</returns>
    public Task<Result> CheckCommentAccessAsync(Post post, string requesterId);

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

    /// <summary>
    /// Handles the withdrawal of a user, deleting their posts and reactions.
    /// </summary>
    /// <param name="userId">The ID of the user whose posts and reactions are to be deleted.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the withdrawal process.</returns>
    public Task<Result> HandleWithdrawAsync(string userId);

    /// <summary>
    /// Handles voting on a poll in a post.
    /// </summary>
    /// <param name="postId">The ID of the post containing the poll.</param>
    /// <param name="pollId">The ID of the poll.</param>
    /// <param name="requesterId">The ID of the user voting.</param>
    /// <param name="requestDto">The vote request containing selected option indices.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the vote.</returns>
    public Task<Result> VotePollAsync(string postId, string pollId, string requesterId, VotePollRequestDto requestDto);

    /// <summary>
    /// Gets poll votes for a specific poll.
    /// </summary>
    /// <param name="pollId">The ID of the poll.</param>
    /// <returns>A task that represents the asynchronous operation, containing the list of poll votes.</returns>
    public Task<Result<List<PollVote>>> GetPollVotesAsync(string pollId);

    /// <summary>
    /// Gets poll vote for a specific user on a poll.
    /// </summary>
    /// <param name="pollId">The ID of the poll.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the poll vote if exists.</returns>
    public Task<Result<PollVote>> GetPollVoteAsync(string pollId, string userId);

    /// <summary>
    /// Gets voters who voted for a specific option in a poll.
    /// </summary>
    /// <param name="postId">The ID of the post containing the poll.</param>
    /// <param name="pollId">The ID of the poll.</param>
    /// <param name="optionIndex">The index of the poll option.</param>
    /// <param name="requesterId">The ID of the user requesting the voters list.</param>
    /// <returns>A task that represents the asynchronous operation, containing the list of poll voters.</returns>
    public Task<Result<List<PollVoterResponseDto>>> GetPollVotersAsync(string postId, string pollId, int optionIndex, string requesterId);

    /// <summary>
    /// Bulk changes discovery option for posts of the requester.
    /// </summary>
    /// <param name="userId">The ID of the user who requests the operation.</param>
    /// <param name="from">Optional current discovery option filter.</param>
    /// <param name="to">New discovery option to set.</param>
    /// <returns>A task that represents the asynchronous operation, containing the count of affected posts.</returns>
    public Task<Result<long>> BulkChangeDiscoveryOptionAsync(string userId, DiscoveryOption? from, DiscoveryOption to);

    /// <summary>
    /// Bulk deletes posts of the requester.
    /// </summary>
    /// <param name="userId">The ID of the user who requests the operation.</param>
    /// <param name="discoveryOption">Optional discovery option filter.</param>
    /// <returns>A task that represents the asynchronous operation, containing the count of deleted posts.</returns>
    public Task<Result<long>> BulkDeletePostsAsync(string userId, DiscoveryOption? discoveryOption);

    /// <summary>
    /// Bookmarks a post for the user.
    /// </summary>
    /// <param name="postId">The ID of the post to bookmark.</param>
    /// <param name="userId">The ID of the user who bookmarks the post.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the bookmark action.</returns>
    public Task<Result> BookmarkPostAsync(string postId, string userId);

    /// <summary>
    /// Removes a bookmark from a post for the user.
    /// </summary>
    /// <param name="postId">The ID of the post to unbookmark.</param>
    /// <param name="userId">The ID of the user who unbookmarks the post.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the unbookmark action.</returns>
    public Task<Result> UnbookmarkPostAsync(string postId, string userId);

    /// <summary>
    /// Gets the bookmarked posts for the user.
    /// </summary>
    /// <param name="userId">The ID of the user who requests the bookmarks.</param>
    /// <param name="fromPostId">The ID of the post to start from for pagination.</param>
    /// <param name="limit">The maximum number of posts to return.</param>
    /// <returns>A task that represents the asynchronous operation, containing the list of bookmarked posts.</returns>
    public Task<Result<List<Post>>> GetBookmarkedPostsAsync(string userId, string fromPostId = null, int limit = 20);

    /// <summary>
    /// Checks if a post is bookmarked by the user.
    /// </summary>
    /// <param name="postId">The ID of the post to check.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing true if bookmarked, false otherwise.</returns>
    public Task<Result<bool>> IsPostBookmarkedAsync(string postId, string userId);
}
