using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using Microsoft.Extensions.Primitives;

namespace History.ApiService.Services.Interfaces;

public interface ICommentService
{
    /// <summary>
    /// Get comment by post id.
    /// </summary>
    /// <param name="postId">The id of post to get comments</param>
    /// <param name="fromCommentId">The id of comment to start from</param>
    /// <param name="limit">The limit of comments to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of comments</returns>
    public Task<Result<List<Comment>>> GetCommentsByPostIdAsync(string postId, string requesterId, string fromCommentId = null, int limit = 10);


    /// <summary>
    /// Create comment to post
    /// </summary>
    /// <param name="postId">The post id to create comment</param>
    /// <param name="contents">The contents of comment</param>
    /// <param name="requesterId">The id of user who requests create</param>
    /// <param name="files">The files to upload</param>
    /// <returns>A task that represents the asynchronous operation. with result of created comment</returns>
    public Task<Result<Comment>> CreateCommentAsync(string postId, List<BaseContent> contents, string requesterId, IEnumerable<IFormFile> files);

    /// <summary>
    /// Modify comment by id
    /// </summary>
    /// <param name="commentId">The comment id to modify</param>
    /// <param name="contents">The contents to apply</param>
    /// <param name="requesterId">The id of user who requests modify</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task<Result> ModifyCommentAsync(string commentId, List<BaseContent> contents, string requesterId);

    /// <summary>
    /// Delete comment by id 
    /// </summary>
    /// <param name="commentId">The comment id to delete</param>
    /// <param name="requesterId">The id of user who requests delete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task<Result> DeleteCommentAsync(string commentId, string requesterId);
}
