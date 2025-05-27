using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.OpenApi.Validations;
using MongoDB.Driver;

namespace History.ApiService.Services;


public class CommentService(IMongoDatabase database, IMediaService mediaService, INotificationService notificationService, IServiceProvider serviceProvider) : ICommentService
{
    private readonly IMongoCollection<Comment> _commentCollection = database.GetCollection<Comment>("Comments");
    private readonly IMongoCollection<CommentLike> _commentLikeCollection = database.GetCollection<CommentLike>("CommentLikes");

    /// <inheritdoc />
    public async Task<Result<Comment>> GetCommentByIdAsync(string commentId)
    {
        var comment = await _commentCollection.Find(f => f.Id == commentId).FirstOrDefaultAsync();
        if (comment == null) return (ErrorType.NotFound, "댓글을 찾을 수 없습니다.");
        return comment;
    }

    /// <inheritdoc />
    public async Task<Result<CommentLike>> GetCommentLikeByIdAsync(string commentLikeId)
    {
        var commentLike = await _commentLikeCollection.Find(f => f.Id == commentLikeId).FirstOrDefaultAsync();
        if (commentLike == null) return (ErrorType.NotFound, "댓글 좋아요를 찾을 수 없습니다.");
        return commentLike;
    }

    public async Task<Result<List<string>>> GetCommenterUserIdsByPostIdAsync(string postId)
    {
        var userIds = await _commentCollection
            .Find(x => x.PostId == postId)
            .Project(x => x.UserId)
            .ToListAsync();

        return userIds.Distinct().ToList();
    }

    public async Task<Result<List<Comment>>> GetCommentsByPostIdAsync(string postId, string requesterId, string fromCommentId = null, int limit = 10)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();
        var postService = serviceProvider.GetRequiredService<IPostService>();

        var accessResult = await postService.CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure) return accessResult.CastFailure< List<Comment>>();

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
            var requesterBannedFriendIdsResult = await friendshipService.GetBannedUserIdsAsync(requesterId);
            filter = Builders<Comment>.Filter.And(filter, Builders<Comment>.Filter.Nin(f => f.UserId, requesterBannedFriendIdsResult.Value));
        }

        var comments = await _commentCollection
            .Find(filter)
            .Sort(Builders<Comment>.Sort.Descending(f => f.CreatedAt))
            .Limit(limit)
            .ToListAsync();

        return comments;
    }

    /// <inheritdoc />
    public async Task<Result<int>> GetCommentsCountByPostIdAsync(string postId, string requesterId)
    {
        var postService = serviceProvider.GetRequiredService<IPostService>();
        var accessResult = await postService.CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure) return accessResult.CastFailure<int>();

        var filter = Builders<Comment>.Filter.Eq(f => f.PostId, postId);
        if (requesterId != null)
        {
            var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();
            var requesterBannedFriendIdsResult = await friendshipService.GetBannedUserIdsAsync(requesterId);
            filter = Builders<Comment>.Filter.And(filter, Builders<Comment>.Filter.Nin(f => f.UserId, requesterBannedFriendIdsResult.Value));
        }
        var count = await _commentCollection.CountDocumentsAsync(filter);
        return (int)count;
    }

    /// <inheritdoc />
    public async Task<Result<Comment>> WriteCommentAsync(string postId, List<BaseContent> contents, string requesterId, IEnumerable<IFormFile> files)
    {
        var postService = serviceProvider.GetRequiredService<IPostService>();
        var userService = serviceProvider.GetRequiredService<IUserService>();

        if (requesterId == null) Result<Comment>.Failure(ErrorType.Unauthorized, "로그인이 필요합니다.");

        var mediaCount = contents.Count(x => x is UploadContent || x is MediaContent);
        if (mediaCount > 20) return (ErrorType.BadRequest, "미디어는 최대 20개까지 추가할 수 있습니다.");

        // Check access
        var accessResult = await postService.CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure) return accessResult.CastFailure<Comment>();

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

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Comment, comment.Id, requesterId, contents, files);
        if (uploadResult.IsFailure) return uploadResult.CastFailure<Comment>();

        var externalUrlContents = comment.Contents.OfType<ExternalUrlContent>();
        foreach (var externalUrlContent in externalUrlContents.ToList())
        {
            var fillResult = await postService.FillExternalUrlContentAsync(externalUrlContent);
            if (fillResult.IsFailure) comment.Contents.Remove(externalUrlContent);
        }

        // Insert the comment
        await _commentCollection.InsertOneAsync(comment);

        // Send Push Notification
        if (comment.Contents.OfType<ProfileContent>().Any()) await notificationService.SendNotificationsAsync(NotificationType.CommentMention, comment.Id);
        else await notificationService.SendNotificationsAsync(NotificationType.Comment, comment.Id);

        // return newly created comment
        return comment;
    }

    /// <inheritdoc />
    public async Task<Result> ModifyCommentAsync(string commentId, List<BaseContent> contents, string requesterId, IEnumerable<IFormFile> files)
    {
        var postService = serviceProvider.GetRequiredService<IPostService>();

        var permissionResult = await CheckPermissionAsync(commentId, requesterId);
        if (permissionResult.IsFailure) return permissionResult;

        var mediaCount = contents.Count(x => x is UploadContent || x is MediaContent);
        if (mediaCount > 20) return (ErrorType.BadRequest, "미디어는 최대 20개까지 추가할 수 있습니다.");

        // Fetch original comment before update
        var originalComment = await _commentCollection.Find(f => f.Id == commentId).FirstOrDefaultAsync();

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Comment, originalComment.Id, requesterId, contents, files);
        if (uploadResult.IsFailure) return uploadResult.CastFailure<Comment>();

        var externalUrlContents = contents.OfType<ExternalUrlContent>();
        foreach (var externalUrlContent in externalUrlContents.ToList())
        {
            var fillResult = await postService.FillExternalUrlContentAsync(externalUrlContent);
            if (fillResult.IsFailure) contents.Remove(externalUrlContent);
        }

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
        var postService = serviceProvider.GetRequiredService<IPostService>();
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var userResult = await userService.GetUserByIdAsync(requesterId);
        if (userResult.IsFailure) return userResult.CastFailure();

        var comment = await _commentCollection.Find(f => f.Id == commentId).FirstOrDefaultAsync();
        if (comment == null) return Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");

        var postResult = await postService.GetPostByIdAsync(comment.PostId);
        if (postResult.IsFailure) return postResult.CastFailure();

        var hasPermission = requesterId == comment.UserId || requesterId == postResult.Value.UserId || userResult.Value.Rank >= Rank.Moderator;
        if (!hasPermission) return Result.Failure(ErrorType.Forbidden, "권한이 없습니다.");

        // Delete Comment
        var result = await _commentCollection.DeleteOneAsync(f => f.Id == commentId);
        if (result.DeletedCount == 0) return Result.Failure(ErrorType.NotFound, "댓글을 찾을 수 없습니다.");

        // Delete Comment Likes
        await _commentLikeCollection.DeleteManyAsync(f => f.CommentId == commentId);

        // Delete Media
        await mediaService.DeleteMediaByAssociatedIdAsync(commentId);

        // Delete Notifications
        await notificationService.DeleteNotificationsAsync("Data.CommentId", commentId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> HandleLikeCommentAsync(string commentId, string requesterId)
    {
        var existingLike = await _commentLikeCollection.Find(f => f.CommentId == commentId && f.UserId == requesterId).FirstOrDefaultAsync();
        if (existingLike != null)
        {
            // Unlike
            var result = await _commentLikeCollection.DeleteOneAsync(f => f.CommentId == commentId && f.UserId == requesterId);

            // Delete Notifications
            await notificationService.DeleteNotificationsAsync("AssociatedId", existingLike.Id, NotificationType.CommentLike);

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

        await notificationService.SendNotificationsAsync(NotificationType.CommentLike, commentLike.Id);

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
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var userResult = await userService.GenerateUserResponseDtoAsync(comment.UserId, requesterId);
        if (userResult.IsFailure) return userResult.CastFailure<CommentResponseDto>();

        var likedUserIds = await _commentLikeCollection
            .Find(f => f.CommentId == comment.Id)
            .Project(f => f.UserId)
            .ToListAsync();

        var likedUserResults = await userService.GenerateUserResponseDtosAsync(likedUserIds.Distinct(), requesterId);


        var profileContents = comment.Contents.OfType<ProfileContent>();
        var profileContentUsersResult = await userService.GenerateUserResponseDtosAsync(profileContents.Select(x => x.UserId), requesterId);
        foreach (var profileContent in profileContents)
        {
            var user = profileContentUsersResult.Value.FirstOrDefault(x => x.UserId == profileContent.UserId);
            profileContent.UserId = user?.UserId;
            profileContent.Nickname = (user?.Nickname ?? "차단된 사용자") + ' ';
        }

        return new CommentResponseDto
        {
            Id = comment.Id,
            User = userResult.Value,
            Contents = comment.Contents,
            LikedUsers = likedUserResults.Value,
            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.ModifiedAt,
        };
    }

    /// <inheritdoc />
    public async Task<Result> HandleWithdrawAsync(string userId)
    {
        // Delete Comment Likes
        await _commentLikeCollection.DeleteManyAsync(f => f.UserId == userId);

        var commentIds = await _commentCollection
            .Find(f => f.UserId == userId)
            .Project(f => f.Id)
            .ToListAsync();

        // Delete Medias associated with comments
        await mediaService.DeleteMediasByAssociatedIdsAsync(commentIds);

        // Delete Comments
        await _commentCollection.DeleteManyAsync(f => f.UserId == userId);

        return Result.Success();
    }

    private async Task<Result> CheckPermissionAsync(string commentId, string requesterId)
    {
        var postService = serviceProvider.GetRequiredService<IPostService>();
        var userService = serviceProvider.GetRequiredService<IUserService>();

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
