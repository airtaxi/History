using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Content;
using MongoDB.Driver;

namespace History.ApiService.Services;


public class CommentService(IMongoDatabase database, IUserService userService, IPostService postService, IMediaService mediaService, IFriendshipService friendshipService) : ICommentService
{
    private readonly IMongoCollection<Comment> _commentCollection = database.GetCollection<Comment>("Comments");

    /// <inheritdoc />
    public async Task<Result<List<Comment>>> GetCommentsByPostIdAsync(string postId, string requesterId, string fromCommentId = null, int limit = 10)
    {
        var accessResult = await CheckAccessAsync(postId, requesterId);
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
            var requseterBlockedFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);
            var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
            var requesterBlockerFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);

            filter = Builders<Comment>.Filter.And(filter, Builders<Comment>.Filter.Nin(f => f.UserId, requseterBlockedFriendIdsResult.Value));
            filter = Builders<Comment>.Filter.And(filter, Builders<Comment>.Filter.Nin(f => f.UserId, requesterIgnoredFriendIdsResult.Value));
            filter = Builders<Comment>.Filter.And(filter, Builders<Comment>.Filter.Nin(f => f.UserId, requesterBlockerFriendIdsResult.Value));
        }

        var comments = await _commentCollection
            .Find(filter)
            .Sort(Builders<Comment>.Sort.Descending(f => f.CreatedAt))
            .Limit(limit)
            .ToListAsync();

        return comments;
    }

    /// <inheritdoc />
    public async Task<Result<Comment>> CreateCommentAsync(string postId, List<BaseContent> contents, string requesterId)
    {
        if (requesterId == null) Result<Comment>.Failure(ErrorType.Unauthorized, "로그인이 필요합니다.");

        var accessResult = await CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure) return Result<Comment>.Failure(accessResult);

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
    public async Task<Result> ModifyCommentByIdAsync(string commentId, List<BaseContent> contents, string requesterId)
    {
        var permissionResult = await CheckPermissionAsync(commentId, requesterId);

        if (permissionResult.IsSuccess)
        {
            // Fetch original comment before update
            var originalComment = await _commentCollection.Find(f => f.Id == commentId).FirstOrDefaultAsync();

            // Update Comment
            var filter = Builders<Comment>.Filter.Eq(f => f.Id, commentId);
            var update = Builders<Comment>.Update.Set(f => f.Contents, contents).Set(f => f.ModifiedAt, DateTime.UtcNow);

            var result = await _commentCollection.UpdateOneAsync(filter, update);

            // Delete Media
            var originalCommentMediaIds = originalComment.Contents.OfType<MediaContent>().Select(s => s.MediaId).ToList();
            var mediaIds = contents.OfType<MediaContent>().Select(s => s.MediaId).ToList();

            var deletedMediaIds = originalCommentMediaIds.Except(mediaIds).ToList();
            foreach (var mediaId in deletedMediaIds) await mediaService.DeleteMediaByMediaIdAsync(mediaId);

            return result.ModifiedCount > 0 ? Result.Success() : Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");
        }
        else return permissionResult;
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCommentByIdAsync(string commentId, string requesterId)
    {
        var permissionResult = await CheckPermissionAsync(commentId, requesterId);

        if (permissionResult.IsSuccess)
        {
            var comment = await _commentCollection.Find(f => f.Id == commentId).FirstOrDefaultAsync();

            // Delete Comment
            var result = await _commentCollection.DeleteOneAsync(f => f.Id == commentId);

            // Delete Media
            var mediaIds = comment.Contents.OfType<MediaContent>().Select(s => s.MediaId).ToList();
            foreach (var mediaId in mediaIds) await mediaService.DeleteMediaByMediaIdAsync(mediaId);

            return result.DeletedCount > 0 ? Result.Success() : Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");
        }
        else return permissionResult;
    }

    private async Task<Result> CheckAccessAsync(string postId, string requesterId)
    {
        var postResult = await postService.GetPostByIdAsync(postId);
        if (postResult.IsFailure) return Result<Comment>.Failure(ErrorType.NotFound, "게시글을 찾을 수 없습니다.");

        var postAuthorId = postResult.Value.UserId;

        // Apply discovery option / privacy settings
        var postDiscoveryOption = postResult.Value.DiscoveryOption;
        if (postDiscoveryOption < Commons.Enums.DiscoveryOption.Everyone)
        {
            bool hasAccess;
            if (postDiscoveryOption == Commons.Enums.DiscoveryOption.FriendsOfFriends) hasAccess = await friendshipService.AreFriendsOfFriendsAsync(postAuthorId, requesterId);
            else if (postDiscoveryOption == Commons.Enums.DiscoveryOption.Friends) hasAccess = await friendshipService.AreFriendsAsync(postAuthorId, requesterId);
            else if (postDiscoveryOption == Commons.Enums.DiscoveryOption.SelectedUsers) hasAccess = postResult.Value.DiscoveryOptionSelectedUserIds.Contains(requesterId);
            else if (postDiscoveryOption == Commons.Enums.DiscoveryOption.OnlyMe) hasAccess = postAuthorId == requesterId;
            else
            {
                var requesterBlockerIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);
                if (requesterBlockerIdsResult.IsFailure) return requesterBlockerIdsResult;
                else if (requesterBlockerIdsResult.Value.Contains(postAuthorId)) hasAccess = false;
                else hasAccess = true;
            }

            if (!hasAccess) return Result<Comment>.Failure(ErrorType.Forbidden, "이 게시물에 댓글을 달 수 있는 권한이 없습니다.");
        }

        return Result.Success();
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
        else if (requesterResult.Value.Rank > Commons.Enums.Rank.User) hasAccess = true;

        return hasAccess ? Result.Success() : Result.Failure(ErrorType.Forbidden, "권한이 없습니다.");
    }
}
