using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class ModerationService(IMongoDatabase database, INotificationService notificationService, IServiceProvider serviceProvider) : IModerationService
{
    private readonly IMongoCollection<ModerationRecord> _moderationRecordCollection = database.GetCollection<ModerationRecord>("ModerationRecords");

    public async Task<Result<ModerationRecord>> GetModerationRecordByIdAsync(string recordId)
    {
        var record = await _moderationRecordCollection.Find(r => r.Id == recordId).FirstOrDefaultAsync();
        return record != null ? record : (ErrorType.NotFound, "제재 내역을 찾을 수 없습니다.");
    }

    public async Task<Result<List<ModerationRecord>>> GetModerationRecordsAsync(string fromRecordId = null, int limit = 10)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var stickerService = serviceProvider.GetRequiredService<IStickerService>();

        var filter = Builders<ModerationRecord>.Filter.Empty;
        if (!string.IsNullOrEmpty(fromRecordId))
        {
            var fromRecord = _moderationRecordCollection.Find(r => r.Id == fromRecordId).FirstOrDefaultAsync();
            if (fromRecord == null) return (ErrorType.NotFound, "제재 내역을 찾을 수 없습니다.");
            filter = Builders<ModerationRecord>.Filter.Gt(r => r.CreatedAt, fromRecord.Result.CreatedAt);
        }

        var records = await _moderationRecordCollection.Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        foreach(var record in records)
        {
            // Fill profile content user info
            var profileContents = record.AssociatedContents.OfType<ProfileContent>();
            var profileContentUsersResult = await userService.GenerateUserResponseDtosAsync(profileContents.Select(x => x.UserId));
            foreach (var profileContent in profileContents)
            {
                var user = profileContentUsersResult.Value.FirstOrDefault(x => x.UserId == profileContent.UserId);
                profileContent.UserId = user?.UserId;
                profileContent.Nickname = (user?.Nickname ?? "탈퇴한 사용자") + ' ';
            }

            // Fill in missing sticker media IDs
            var emptyStickerContents = record.AssociatedContents.OfType<StickerContent>().Where(x => x.StickerMediaId == null);
            foreach (var emptyStickerContent in emptyStickerContents)
            {
                var assetResult = await stickerService.GetStickerAssetByIdAsync(emptyStickerContent.StickerContentId);
                if (assetResult.IsSuccess) emptyStickerContent.StickerMediaId = assetResult.Value.MediaId;
            }
        }

        return records;
    }

    public async Task<Result> DeleteModerationRecordByIdAsync(string recordId)
    {
        var result = await _moderationRecordCollection.DeleteOneAsync(r => r.Id == recordId);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "제재 내역을 찾을 수 없습니다.");

        // Delete associated notifications
        await notificationService.DeleteNotificationsAsync("AssociatedId", recordId, NotificationType.Restriction);

        return Result.Success();
    }

    public async Task<Result> DeleteModerationRecordByUserIdAsync(string postId)
    {
        var result = await _moderationRecordCollection.DeleteManyAsync(r => r.UserId == postId);
        await notificationService.DeleteNotificationsAsync("RestrictedUserId", postId, NotificationType.Restriction);
        return Result.Success();
    }

    public async Task<Result> DeletePostAsync(string postId, string moderatorId, string reason, ReportType reportType)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var postService = serviceProvider.GetRequiredService<IPostService>();
        var reportService = serviceProvider.GetRequiredService<IReportService>();

        var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
        if (!moderatorResult.IsSuccess) return moderatorResult.CastFailure();

        var moderator = moderatorResult.Value;
        if (moderator.Rank < Rank.Moderator) return (ErrorType.Forbidden, "괸리자만 게시글에 대한 삭제 조치를 할 수 있습니다.");

        var postResult = await postService.GetPostByIdAsync(postId);
        if (!postResult.IsSuccess) return postResult.CastFailure();
        var post = postResult.Value;

        // Delete the post
        var result = await postService.DeletePostAsync(postId, moderatorId);
        if (result.IsFailure) return result;

        // Delete associated reports
        await reportService.DeleteReportRecordByPostIdAsync(postId);

        // Create a restriction record for the deleted post
        var record = new ModerationRecord
        {
            UserId = post.UserId,
            AssociatedId = post.Id,
            AssociatedContents = post.Contents,
            AssociatedCreatedAt = post.CreatedAt,
            AssociatedModifiedAt = post.ModifiedAt,
            ModeratorId = moderator.Id,
            Reason = reason,
            ReportType = reportType,
            RestrictionType = RestrictionType.PostDeletion,
            CreatedAt = DateTime.UtcNow
        };

        while (true)
        {
            record.Id = Guid.NewGuid().ToString("N");
            var existingRecord = await _moderationRecordCollection.Find(r => r.Id == record.Id).FirstOrDefaultAsync();
            if (existingRecord == null) break;
        }

        await _moderationRecordCollection.InsertOneAsync(record);

        // Send notifications
        await notificationService.SendNotificationsAsync(NotificationType.Restriction, record.Id);

        return Result.Success();
    }

    public async Task<Result> DeleteCommentAsync(string commentId, string moderatorId, string reason, ReportType reportType)
    {
        var reportService = serviceProvider.GetRequiredService<IReportService>();

        var userService = serviceProvider.GetRequiredService<IUserService>();
        var commentService = serviceProvider.GetRequiredService<ICommentService>();

        var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
        if (!moderatorResult.IsSuccess) return moderatorResult.CastFailure();

        var moderator = moderatorResult.Value;
        if (moderator.Rank < Rank.Moderator) return (ErrorType.Forbidden, "괸리자만 댓글에 대한 삭제 조치를 할 수 있습니다.");

        var commentResult = await commentService.GetCommentByIdAsync(commentId);
        if (!commentResult.IsSuccess) return commentResult.CastFailure();
        var comment = commentResult.Value;

        // Delete the comment
        var result = await commentService.DeleteCommentAsync(commentId, moderatorId);
        if (result.IsFailure) return result;

        // Delete associated reports
        await reportService.DeleteReportRecordByCommentIdAsync(commentId);

        // Create a restriction record for the deleted comment
        var record = new ModerationRecord
        {
            UserId = comment.UserId,
            AssociatedId = comment.Id,
            AssociatedContents = comment.Contents,
            AssociatedCreatedAt = comment.CreatedAt,
            AssociatedModifiedAt = comment.ModifiedAt,
            ModeratorId = moderator.Id,
            Reason = reason,
            ReportType = reportType,
            RestrictionType = RestrictionType.CommentDeletion,
            CreatedAt = DateTime.UtcNow
        };

        while (true)
        {
            record.Id = Guid.NewGuid().ToString("N");
            var existingRecord = await _moderationRecordCollection.Find(r => r.Id == record.Id).FirstOrDefaultAsync();
            if (existingRecord == null) break;
        }

        await _moderationRecordCollection.InsertOneAsync(record);

        // Send notifications
        await notificationService.SendNotificationsAsync(NotificationType.Restriction, record.Id);

        return Result.Success();
    }

    public async Task<Result<ModerationRecordResponseDto>> GenerateModerationRecordResponseDtoAsync(ModerationRecord record)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var moderatorResult = await userService.GenerateUserResponseDtoAsync(record.ModeratorId);
        var userResult = await userService.GenerateUserResponseDtoAsync(record.UserId);

        var responseDto = new ModerationRecordResponseDto(record)
        {
            User = userResult.IsSuccess ? userResult.Value : null,
            Moderator = moderatorResult.IsSuccess ? moderatorResult.Value : null,
        };

        return responseDto;
    }

    public async Task<Result<List<ModerationRecordResponseDto>>> GenerateModerationRecordResponseDtosAsync(List<ModerationRecord> records)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var moderatorIds = records.Select(r => r.ModeratorId).Distinct().ToList();
        var userIds = records.Select(r => r.UserId).Distinct().ToList();

        var moderatorResults = await userService.GenerateUserResponseDtosAsync(moderatorIds);
        var userResults = await userService.GenerateUserResponseDtosAsync(userIds);

        var results = new List<ModerationRecordResponseDto>();

        foreach(var record in records)
        {
            var moderator = moderatorResults.Value.FirstOrDefault(m => m.UserId == record.ModeratorId);
            var user = userResults.Value.FirstOrDefault(u => u.UserId == record.UserId);

            results.Add(new ModerationRecordResponseDto(record)
            {
                User = user,
                Moderator = moderator
            });
        }

        return results;
    }
}
