using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.VisualBasic;
using MongoDB.Driver;

namespace History.ApiService.Services;


public class CommentService(IMongoDatabase database, IUserService userService, IPostService postService, IMediaService mediaService, IFriendshipService friendshipService) : ICommentService
{
    private readonly IMongoCollection<Comment> _commentCollection = database.GetCollection<Comment>("Comments");
    private readonly IMongoCollection<CommentLike> _commentLikeCollection = database.GetCollection<CommentLike>("CommentLikes");

    /// <inheritdoc />
    public async Task<Result<List<Comment>>> GetCommentsByPostIdAsync(string postId, string requesterId, string fromCommentId = null, int limit = 10)
    {
        var accessResult = await postService.CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure) return Result<List<Comment>>.Failure(accessResult);

        var filter = Builders<Comment>.Filter.Eq(f => f.PostId, postId);
        if (!string.IsNullOrEmpty(fromCommentId))
        {
            var fromComment = await _commentCollection.Find(f => f.Id == fromCommentId).FirstOrDefaultAsync();
            if (fromComment != null)
            {
                var timeFilter = Builders<Comment>.Filter.Lt(f => f.CreatedAt, fromComment.CreatedAt);
                filter = Builders<Comment>.Filter.And(filter, timeFilter);
            }
        }

        if (requesterId != null)
        {
            var requseterBannedFriendIdsResult = await friendshipService.GetBannedUserIdsAsync(requesterId);

            filter = Builders<Comment>.Filter.And(filter, Builders<Comment>.Filter.Nin(f => f.UserId, requseterBannedFriendIdsResult.Value));
        }

        var comments = await _commentCollection
            .Find(filter)
            .Sort(Builders<Comment>.Sort.Descending(f => f.CreatedAt))
            .Limit(limit)
            .ToListAsync();

        return comments;
    }

    /// <inheritdoc />
    public async Task<Result<Comment>> WriteCommentAsync(string postId, List<BaseContent> contents, string requesterId, IEnumerable<IFormFile> files)
    {
        if (requesterId == null) Result<Comment>.Failure(ErrorType.Unauthorized, "로그인이 필요합니다.");

        // Check access
        var accessResult = await postService.CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure) return Result<Comment>.Failure(accessResult);

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Comment, postId, requesterId, contents, files);
        if (uploadResult.IsFailure) return Result<Comment>.Failure(uploadResult);

        // Create comment
        var comment = new Comment
        {
            PostId = postId,
            UserId = requesterId,
            Contents = contents,
            CreatedAt = DateTime.UtcNow,
        };

        while (true)
        {
            comment.Id = Guid.NewGuid().ToString("N");
            var existingComment = _commentCollection.Find(f => f.Id == comment.Id).FirstOrDefault();

            if (existingComment == null) break;
        }
        comment.CreatedAt = DateTime.UtcNow;

        // Insert the comment
        await _commentCollection.InsertOneAsync(comment);

        // return newly created comment
        return comment;
    }

    /// <inheritdoc />
    public async Task<Result> ModifyCommentAsync(string commentId, List<BaseContent> contents, string requesterId, IEnumerable<IFormFile> files)
    {
        var permissionResult = await CheckPermissionAsync(commentId, requesterId);
        if (permissionResult.IsFailure) return permissionResult;

        // Fetch original comment before update
        var originalComment = await _commentCollection.Find(f => f.Id == commentId).FirstOrDefaultAsync();

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Comment, originalComment.PostId, requesterId, contents, files);
        if (uploadResult.IsFailure) return Result<Comment>.Failure(uploadResult);

        // Update Comment
        var filter = Builders<Comment>.Filter.Eq(f => f.Id, commentId);
        var update = Builders<Comment>.Update.Set(f => f.Contents, contents).Set(f => f.ModifiedAt, DateTime.UtcNow);

        var result = await _commentCollection.UpdateOneAsync(filter, update);

        // Delete Media
        var originalCommentMediaIds = originalComment.Contents.OfType<MediaContent>().Select(s => s.MediaId).ToList();
        var mediaIds = contents.OfType<MediaContent>().Select(s => s.MediaId).ToList();

        var deletedMediaIds = originalCommentMediaIds.Except(mediaIds).ToList();
        foreach (var mediaId in deletedMediaIds) await mediaService.DeleteMediaByIdAsync(mediaId);

        return result.ModifiedCount > 0 ? Result.Success() : Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCommentAsync(string commentId, string requesterId)
    {
        var permissionResult = await CheckPermissionAsync(commentId, requesterId);

        if (permissionResult.IsSuccess)
        {
            // Delete Comment
            var result = await _commentCollection.DeleteOneAsync(f => f.Id == commentId);
            if (result.DeletedCount == 0) return Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");

            var deleteResult = await mediaService.DeleteMediaByAssociatedIdAsync(commentId);
            if (deleteResult.IsFailure) return deleteResult;

            return Result.Success();
        }
        else return permissionResult;
    }

    /// <inheritdoc />
    public async Task<Result> HandleLikeCommentAsync(string commentId, string requesterId)
    {
        var permissionResult = await CheckPermissionAsync(commentId, requesterId);
        if (permissionResult.IsFailure) return permissionResult;

        var existingLike = await _commentLikeCollection.Find(f => f.CommentId == commentId && f.UserId == requesterId).FirstOrDefaultAsync();
        if (existingLike != null)
        {
            // Unlike
            var result = await _commentLikeCollection.DeleteOneAsync(f => f.CommentId == commentId && f.UserId == requesterId);
            return result.DeletedCount > 0 ? Result.Success() : Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");
        }

        // Like
        var commentLike = new CommentLike
        {
            CommentId = commentId,
            UserId = requesterId,
            CreatedAt = DateTime.UtcNow
        };

        while (true)
        {
            commentLike.Id = Guid.NewGuid().ToString("N");

            var existingCommentLike = await _commentLikeCollection.Find(f => f.Id == commentLike.Id).FirstOrDefaultAsync();
            if (existingCommentLike == null) break;
        }

        await _commentLikeCollection.InsertOneAsync(commentLike);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<List<CommentResponseDto>>> GenerateCommentResponseDtosAsync(IEnumerable<Comment> comment, string requesterId)
    {
        var tasks = comment.Select(c => GenerateCommentResponseDtoAsync(c, requesterId)).ToList();
        await Task.WhenAll(tasks);

        return tasks.Where(x => x.Result.IsSuccess).Select(t => t.Result.Value).ToList();
    }

    /// <inheritdoc />
    public async Task<Result<CommentResponseDto>> GenerateCommentResponseDtoAsync(Comment comment, string requesterId)
    {
        var responseDto = new CommentResponseDto
        {
            Id = comment.Id,

            PostId = comment.PostId,
            UserId = comment.UserId,

            Contents = comment.Contents,

            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.ModifiedAt,
        };

        var likedUserIds = await _commentLikeCollection
            .Find(f => f.CommentId == comment.Id)
            .Project(f => f.UserId)
            .ToListAsync();

        var likedUsersDtoResult = await userService.GenerateUserResponseDtosAsync(likedUserIds, requesterId);

        responseDto.LikedUsers = likedUsersDtoResult.Value;
        return responseDto;
    }

    private async Task<Result> CheckPermissionAsync(string commentId, string requesterId)
    {
        var comment = await _commentCollection.Find(f => f.Id == commentId).FirstOrDefaultAsync();
        if (comment == null) return Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");

        var postResult = await postService.GetPostByIdAsync(comment.PostId);
        if (postResult.Error == ErrorType.NotFound) return Result.Failure(ErrorType.NotFound, "게시글을 찾을 수 없습니다.");
        else if (postResult.IsFailure) return postResult.CastFailure();

        var requesterResult = await userService.GetUserByIdAsync(requesterId);
        if (requesterResult.Error == ErrorType.NotFound) return Result.Failure(ErrorType.NotFound, "사용자를 찾을 수 없습니다.");
        else if (requesterResult.IsFailure) return requesterResult.CastFailure();

        var hasAccess = false;

        if (requesterId == comment.UserId) hasAccess = true;
        else if (requesterId == postResult.Value.UserId) hasAccess = true;
        else if (requesterResult.Value.Rank > Rank.User) hasAccess = true;

        return hasAccess ? Result.Success() : Result.Failure(ErrorType.Forbidden, "권한이 없습니다.");
    }
}
