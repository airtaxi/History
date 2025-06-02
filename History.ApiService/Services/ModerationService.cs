using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class ModerationService(IMongoDatabase database, INotificationService notificationService, IServiceProvider serviceProvider) : IModerationService
{
    private readonly IMongoCollection<RestrictionRecord> _restrictionRecordCollection = database.GetCollection<RestrictionRecord>("RestrictionRecords");

    public async Task<Result<RestrictionRecord>> GetRestrictionRecordByIdAsync(string recordId)
    {
        var record = await _restrictionRecordCollection.Find(r => r.Id == recordId).FirstOrDefaultAsync();
        return record != null ? record : (ErrorType.NotFound, "제재 내역을 찾을 수 없습니다.");
    }

    public async Task<Result> DeleteRestrictionRecordByIdAsync(string recordId)
    {
        var result = await _restrictionRecordCollection.DeleteOneAsync(r => r.Id == recordId);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "제재 내역을 찾을 수 없습니다.");

        // Delete associated notifications
        await notificationService.DeleteNotificationsAsync("AssociatedId", recordId);

        return Result.Success();
    }

    public async Task<Result> DeletePostAsync(string postId, string moderatorId, string reason)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var postService = serviceProvider.GetRequiredService<IPostService>();

        var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
        if (!moderatorResult.IsSuccess) return moderatorResult.CastFailure();

        var moderator = moderatorResult.Value;
        if(moderator.Rank < Rank.Moderator) return (ErrorType.Forbidden, "괸리자만 게시글에 대한 삭제 조치를 할 수 있습니다.");

        var postResult = await postService.GetPostByIdAsync(postId);
        if (!postResult.IsSuccess) return postResult.CastFailure();
        var post = postResult.Value;

        // Delete the post
        var result = await postService.DeletePostAsync(postId, moderatorId);
        if (result.IsFailure) return result;

        // Create a restriction record for the deleted post
        var record = new RestrictionRecord
        {
            UserId = post.UserId,
            AssociatedId = post.Id,
            AssociatedContents = post.Contents,
            AssociatedCreatedAt = post.CreatedAt,
            AssociatedModifiedAt = post.ModifiedAt,
            ModeratorId = moderator.Id,
            Reason = reason,
            Type = RestrictionType.PostDeletion,
            CreatedAt = DateTime.UtcNow
        };

        while(true)
        {
            record.Id = Guid.NewGuid().ToString("N");
            var existingRecord = await _restrictionRecordCollection.Find(r => r.Id == record.Id).FirstOrDefaultAsync();
            if (existingRecord == null) break;
        }

        await _restrictionRecordCollection.InsertOneAsync(record);

        // Send notifications
        await notificationService.SendNotificationsAsync(NotificationType.Restriction, record.Id);

        return Result.Success();
    }

    public async Task<Result> DeleteCommentAsync(string commentId, string moderatorId, string reason)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var commentService = serviceProvider.GetRequiredService<ICommentService>();

        var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
        if (!moderatorResult.IsSuccess) return moderatorResult.CastFailure();

        var moderator = moderatorResult.Value;
        if(moderator.Rank < Rank.Moderator) return (ErrorType.Forbidden, "괸리자만 댓글에 대한 삭제 조치를 할 수 있습니다.");

        var commentResult = await commentService.GetCommentByIdAsync(commentId);
        if (!commentResult.IsSuccess) return commentResult.CastFailure();
        var comment = commentResult.Value;

        // Delete the comment
        var result = await commentService.DeleteCommentAsync(commentId, moderatorId);
        if (result.IsFailure) return result;

        // Create a restriction record for the deleted comment
        var record = new RestrictionRecord
        {
            UserId = comment.UserId,
            AssociatedId = comment.Id,
            AssociatedContents = comment.Contents,
            AssociatedCreatedAt = comment.CreatedAt,
            AssociatedModifiedAt = comment.ModifiedAt,
            ModeratorId = moderator.Id,
            Reason = reason,
            Type = RestrictionType.CommentDeletion,
            CreatedAt = DateTime.UtcNow
        };

        while(true)
        {
            record.Id = Guid.NewGuid().ToString("N");
            var existingRecord = await _restrictionRecordCollection.Find(r => r.Id == record.Id).FirstOrDefaultAsync();
            if (existingRecord == null) break;
        }

        await _restrictionRecordCollection.InsertOneAsync(record);

        // Send notifications
        await notificationService.SendNotificationsAsync(NotificationType.Restriction, record.Id);

        return Result.Success();
    }
}