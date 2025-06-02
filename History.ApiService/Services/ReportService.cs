using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class ReportService(IMongoDatabase database, IServiceProvider serviceProvider, IModerationService moderationService) : IReportService
{
    private readonly IMongoCollection<ReportRecord> _reportRecordCollection = database.GetCollection<ReportRecord>("ReportRecords");

    public async Task<Result<ReportRecord>> GetReportRecordByIdAsync(string recordId)
    {
        var record = await _reportRecordCollection.Find(r => r.Id == recordId).FirstOrDefaultAsync();
        return record != null ? record : (ErrorType.NotFound, "신고 내역을 찾을 수 없습니다.");
    }

    public async Task<Result<List<ReportRecord>>> GetReportRecordsAsync(string fromRecordId = null, int limit = 10)
    {
        var filter = Builders<ReportRecord>.Filter.Empty;
        if (!string.IsNullOrEmpty(fromRecordId))
        {
            var fromRecord = await _reportRecordCollection.Find(r => r.Id == fromRecordId).FirstOrDefaultAsync();
            if (fromRecord == null) return (ErrorType.NotFound, "신고 내역을 찾을 수 없습니다.");
            filter = Builders<ReportRecord>.Filter.Gt(r => r.CreatedAt, fromRecord.CreatedAt);
        }

        var records = await _reportRecordCollection.Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Limit(limit)
            .ToListAsync();
        return records;
    }

    public async Task<Result> CreateReportRecordAsync(ReportType type, ReportTarget target, string associatedId, string reporterId)
    {
        var postService = serviceProvider.GetRequiredService<IPostService>();
        var commentService = serviceProvider.GetRequiredService<ICommentService>();

        var newRecord = new ReportRecord
        {
            Target = target,
            Type = type,
            AssociatedId = associatedId,
            ReporterId = reporterId,
            CreatedAt = DateTime.UtcNow
        };

        var existingRecord = await _reportRecordCollection.Find(r => r.AssociatedId == associatedId && r.Target == target && r.ReporterId == reporterId).FirstOrDefaultAsync();
        if (existingRecord != null) return (ErrorType.Conflict, $"이미 신고한 {target.ToDisplayString()}입니다.");

        if (target == ReportTarget.Post)
        {
            var postResult = await postService.GetPostByIdAsync(associatedId);
            if (postResult.IsFailure) return postResult.CastFailure();
            newRecord.UserId = postResult.Value.UserId;
            newRecord.AssociatedContents = postResult.Value.Contents;
        }
        else if (target == ReportTarget.Comment)
        {
            var commentResult = await commentService.GetCommentByIdAsync(associatedId);
            if (commentResult.IsFailure) return commentResult.CastFailure();
            newRecord.UserId = commentResult.Value.UserId;
            newRecord.AssociatedContents = commentResult.Value.Contents;
        }
        else return (ErrorType.BadRequest, "잘못된 신고 대상입니다.");

        while (true)
        {
            newRecord.Id = Guid.NewGuid().ToString("N");
            existingRecord = await _reportRecordCollection.Find(r => r.Id == newRecord.Id).FirstOrDefaultAsync();
            if (existingRecord == null) break;
        }

        await _reportRecordCollection.InsertOneAsync(newRecord);
        return Result.Success();
    }

    public async Task<Result> ProcessReportAsync(string recordId, string moderatorId, string reason)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
        if (moderatorResult.IsFailure) return moderatorResult.CastFailure();

        var moderator = moderatorResult.Value;
        if (moderator.Rank < Rank.Moderator) return (ErrorType.Forbidden, "괸리자만 신고를 처리할 수 있습니다.");

        var record = await _reportRecordCollection.Find(r => r.Id == recordId).FirstOrDefaultAsync();
        if (record == null) return (ErrorType.NotFound, "신고 내역을 찾을 수 없습니다.");

        if (record.Target == ReportTarget.Post)
            return await moderationService.DeletePostAsync(record.AssociatedId, moderatorId, reason, record.Type);
        else if (record.Target == ReportTarget.Comment)
            return await moderationService.DeleteCommentAsync(record.AssociatedId, moderatorId, reason, record.Type);
        else return (ErrorType.BadRequest, "잘못된 신고 대상입니다.");
    }

    public async Task<Result> DeleteReportRecordByIdAsync(string recordId)
    {
        var result = await _reportRecordCollection.DeleteOneAsync(r => r.Id == recordId);
        if (result.DeletedCount == 0) return (ErrorType.NotFound, "신고 내역을 찾을 수 없습니다.");

        return Result.Success();
    }

    public async Task<Result> DeleteReportRecordByPostIdAsync(string postId)
    {
        var result = await _reportRecordCollection.DeleteManyAsync(r => r.Target == ReportTarget.Post && r.AssociatedId == postId);
        return Result.Success();
    }

    public async Task<Result> DeleteReportRecordByCommentIdAsync(string commentId)
    {
        var result = await _reportRecordCollection.DeleteManyAsync(r => r.Target == ReportTarget.Comment && r.AssociatedId == commentId);
        return Result.Success();
    }

    public async Task<Result> DeleteReportRecordByUserIdAsync(string userId)
    {
        var result = await _reportRecordCollection.DeleteManyAsync(r => r.UserId == userId);
        return Result.Success();
    }

    public async Task<Result<ReportRecordResponseDto>> GenerateReportRecordResponseDtoAsync(ReportRecord record)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var reporterResult = await userService.GenerateUserResponseDtoAsync(record.ReporterId);
        var userResult = await userService.GenerateUserResponseDtoAsync(record.UserId);

        var dto = new ReportRecordResponseDto(record)
        {
            Reporter = reporterResult.IsSuccess ? reporterResult.Value : null,
            User = userResult.IsSuccess ? userResult.Value : null
        };

        return dto;
    }

    public async Task<Result<List<ReportRecordResponseDto>>> GenerateReportRecordResponseDtosAsync(List<ReportRecord> records)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var reporterIds = records.Select(r => r.ReporterId).Distinct().ToList();
        var userIds = records.Select(r => r.UserId).Distinct().ToList();

        var reporterResults = await userService.GenerateUserResponseDtosAsync(reporterIds);
        var userResults = await userService.GenerateUserResponseDtosAsync(userIds);

        var results = new List<ReportRecordResponseDto>();

        foreach (var record in records)
        {
            var reporter = reporterResults.Value.FirstOrDefault(m => m.UserId == record.ReporterId);
            var user = userResults.Value.FirstOrDefault(u => u.UserId == record.UserId);

            results.Add(new ReportRecordResponseDto(record)
            {
                User = user,
                Reporter = reporter
            });
        }

        return results;
    }
}