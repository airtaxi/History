using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;

namespace History.ApiService.Services.Interfaces;

public interface ICommentService
{
    /// <summary>
    /// Get comment by id.
    /// </summary>
    /// <param name="commentId">The id of comment to get</param>
    /// <param name="requesterId">The id of user who requests comment</param>
    /// <returns>A task that represents the asynchronous operation. with result of comment</returns>
    public Task<Result<Comment>> GetCommentByIdAsync(string commentId);

    /// <summary>
    /// Get comment by post id.
    /// </summary>
    /// <param name="postId">The id of post to get comments</param>
    /// <param name="fromCommentId">The id of comment to start from</param>
    /// <param name="limit">The limit of comments to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of comments</returns>
    public Task<Result<List<Comment>>> GetCommentsByPostIdAsync(string postId, string requesterId, string fromCommentId = null, int limit = 10);

    /// <summary>
    /// Get count of comments by post id.
    /// </summary>
    /// <param name="postId">The id of post to get comments count</param>
    /// <param name="requesterId">The id of user who requests comments count</param>
    /// <returns>A task that represents the asynchronous operation. with result of comments count</returns>
    public Task<Result<int>> GetCommentsCountByPostIdAsync(string postId, string requesterId);

    /// <summary>
    /// Create comment to post
    /// </summary>
    /// <param name="postId">The post id to create comment</param>
    /// <param name="contents">The contents of comment</param>
    /// <param name="requesterId">The id of user who requests create</param>
    /// <param name="files">The files to upload</param>
    /// <returns>A task that represents the asynchronous operation. with result of created comment</returns>
    public Task<Result<Comment>> WriteCommentAsync(string postId, List<BaseContent> contents, string requesterId, IEnumerable<IFormFile> files);

    /// <summary>
    /// Modify comment by id
    /// </summary>
    /// <param name="commentId">The comment id to modify</param>
    /// <param name="contents">The contents to apply</param>
    /// <param name="requesterId">The id of user who requests modify</param>
    /// <param name="files">The files to upload</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task<Result> ModifyCommentAsync(string commentId, List<BaseContent> contents, string requesterId, IEnumerable<IFormFile> files);

    /// <summary>
    /// Delete comment by id 
    /// </summary>
    /// <param name="commentId">The comment id to delete</param>
    /// <param name="requesterId">The id of user who requests delete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task<Result> DeleteCommentAsync(string commentId, string requesterId);

    /// <summary>
    /// Likes or unlikes a comment based on the user's permission and existing like status.
    /// </summary>
    /// <param name="commentId">Identifies the specific comment to be liked or unliked.</param>
    /// <param name="requesterId">Identifies the user attempting to like or unlike the comment.</param>
    /// <returns>Returns a result indicating the success or failure of the like operation.</returns>
    public Task<Result> HandleLikeCommentAsync(string commentId, string requesterId);

    /// <summary>
    /// Generates a list of comment response data transfer objects asynchronously based on provided comments.
    /// </summary>
    /// <param name="comment">A collection of comments used to create response data transfer objects.</param>
    /// <param name="requesterId">Identifies the user making the request for comment responses.</param>
    /// <returns>A list of successful comment response data transfer objects.</returns>
    public Task<Result<List<CommentResponseDto>>> GenerateCommentResponseDtosAsync(IEnumerable<Comment> comment, string requesterId);

    /// <summary>
    /// Generates a CommentResponseDto from a given Comment object, including details about likes and user information.
    /// </summary>
    /// <param name="comment">Provides the comment data needed to create the response DTO.</param>
    /// <param name="requesterId">Identifies the user making the request to tailor the response accordingly.</param>
    /// <returns>Returns a Task containing the generated CommentResponseDto with relevant details.</returns>
    public Task<Result<CommentResponseDto>> GenerateCommentResponseDtoAsync(Comment comment, string requesterId);
}
