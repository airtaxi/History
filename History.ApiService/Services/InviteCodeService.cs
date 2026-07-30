using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace History.ApiService.Services;

public class InviteCodeService(IMongoDatabase database, IServiceProvider serviceProvider) : IInviteCodeService
{
    private readonly IMongoCollection<InviteCode> _inviteCodeCollection = database.GetCollection<InviteCode>("InviteCodes");
    private readonly IMongoCollection<InviteCodeRequest> _inviteCodeRequestCollection = database.GetCollection<InviteCodeRequest>("InviteCodeRequests");
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");

    // Ambiguous characters (I, O, 0, 1) excluded to avoid confusion
    private const string CodeCharset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;
    private const int MaxRequestCount = 50;

    // Hardcoded invite code for store review — always valid, never consumed
    private const string StoreReviewCode = "HISTORY7K";
    private const int MaxAdminCreateCount = 100;

    /// <inheritdoc />
    public async Task<Result<List<InviteCode>>> GetMyInviteCodesAsync(string userId, string from, int limit)
    {
        limit = Math.Clamp(limit, 1, 100);

        var filter = Builders<InviteCode>.Filter.Eq(x => x.OwnerId, userId);

        if (!string.IsNullOrEmpty(from))
        {
            var fromCode = await _inviteCodeCollection.Find(x => x.Id == from).FirstOrDefaultAsync();
            if (fromCode != null) filter &= Builders<InviteCode>.Filter.Lt(x => x.CreatedAt, fromCode.CreatedAt);
        }

        return await _inviteCodeCollection.Find(filter)
            .SortByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<List<InviteCode>>> GetInviteCodesByOwnerIdAsync(string ownerId, string requesterId, string from, int limit)
    {
        limit = Math.Clamp(limit, 1, 100);

        var filter = Builders<InviteCode>.Filter.Eq(x => x.OwnerId, ownerId);

        if (!string.IsNullOrEmpty(from))
        {
            var fromCode = await _inviteCodeCollection.Find(x => x.Id == from).FirstOrDefaultAsync();
            if (fromCode != null) filter &= Builders<InviteCode>.Filter.Lt(x => x.CreatedAt, fromCode.CreatedAt);
        }

        return await _inviteCodeCollection.Find(filter)
            .SortByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<List<InviteCode>>> CreateInviteCodesAsync(string ownerId, int count, string moderatorId)
    {
        var userResult = await _userCollection.Find(u => u.Id == ownerId).FirstOrDefaultAsync();
        if (userResult == null) return (ErrorType.NotFound, "사용자를 찾을 수 없습니다.");

        count = Math.Clamp(count, 1, MaxAdminCreateCount);

        var codes = new List<InviteCode>();
        for (int i = 0; i < count; i++)
        {
            var code = await GenerateUniqueCodeAsync();
            var inviteCode = new InviteCode
            {
                Id = await GenerateUniqueInvitationIdAsync(),
                Code = code,
                OwnerId = ownerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            codes.Add(inviteCode);
        }

        await _inviteCodeCollection.InsertManyAsync(codes);

        return codes;
    }

    /// <inheritdoc />
    public async Task<Result> ValidateInviteCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return (ErrorType.BadRequest, "초대 코드를 입력해주세요.");

        code = code.Trim().ToUpper();

        if (code == StoreReviewCode) return Result.Success();

        var existing = await _inviteCodeCollection.Find(x => x.Code == code).FirstOrDefaultAsync();
        if (existing == null) return (ErrorType.NotFound, "초대 코드를 찾을 수 없습니다.");
        if (!existing.IsActive) return (ErrorType.Conflict, "이미 사용된 초대 코드입니다.");

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ConsumeInviteCodeAsync(string code, string newUserId)
    {
        if (string.IsNullOrWhiteSpace(code)) return (ErrorType.BadRequest, "초대 코드를 입력해주세요.");

        code = code.Trim().ToUpper();

        // Store review code is never consumed — always remains valid
        if (code == StoreReviewCode) return Result.Success();

        var filter = Builders<InviteCode>.Filter.Eq(x => x.Code, code) & Builders<InviteCode>.Filter.Eq(x => x.IsActive, true);
        var update = Builders<InviteCode>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UsedByUserId, newUserId)
            .Set(x => x.UsedAt, DateTime.UtcNow);

        var result = await _inviteCodeCollection.UpdateOneAsync(filter, update);

        if (result.MatchedCount == 0) return (ErrorType.Conflict, "이미 사용된 초대 코드입니다.");

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<InviteCodeRequest>> CreateInviteCodeRequestAsync(string requesterId, string reason, int count)
    {
        count = Math.Clamp(count, 1, MaxRequestCount);

        // Only allow request when the user has zero active codes
        var activeCount = await GetActiveInviteCodeCountAsync(requesterId);
        if (activeCount.Value > 0) return (ErrorType.BadRequest, "유효한 초대 코드가 남아있을 때는 요청할 수 없습니다.");

        // Check for existing pending request
        var existingPending = await _inviteCodeRequestCollection
            .Find(x => x.RequesterId == requesterId && x.Status == InviteCodeRequestStatus.Pending)
            .FirstOrDefaultAsync();
        if (existingPending != null) return (ErrorType.Conflict, "이미 대기 중인 초대 코드 요청이 있습니다.");

        var request = new InviteCodeRequest
        {
            Id = await GenerateUniqueRequestIdAsync(),
            RequesterId = requesterId,
            Reason = Utils.SanitizeText(reason),
            RequestedCount = count,
            Status = InviteCodeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        try { await _inviteCodeRequestCollection.InsertOneAsync(request); }
        catch (MongoWriteException) { return (ErrorType.Conflict, "이미 대기 중인 초대 코드 요청이 있습니다."); }

        // Notify moderators (push included)
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        await notificationService.SendNotificationsAsync(NotificationType.InviteCodeRequest, request.Id);

        return request;
    }

    /// <inheritdoc />
    public async Task<Result<List<InviteCodeRequest>>> GetInviteCodeRequestsAsync(string moderatorId, string from, int limit)
    {
        limit = Math.Clamp(limit, 1, 100);

        if (string.IsNullOrEmpty(from))
        {
            // No cursor: fetch Pending first (newest), then non-Pending (newest), concatenated
            var pending = await _inviteCodeRequestCollection
                .Find(x => x.Status == InviteCodeRequestStatus.Pending)
                .SortByDescending(x => x.CreatedAt)
                .Limit(limit)
                .ToListAsync();

            if (pending.Count >= limit) return pending;

            var remaining = limit - pending.Count;
            var nonPending = await _inviteCodeRequestCollection
                .Find(x => x.Status != InviteCodeRequestStatus.Pending)
                .SortByDescending(x => x.CreatedAt)
                .Limit(remaining)
                .ToListAsync();

            var result = new List<InviteCodeRequest>(pending);
            result.AddRange(nonPending);
            return result;
        }

        // Cursor-based pagination: determine the cursor's status, then fetch accordingly
        var fromRequest = await _inviteCodeRequestCollection.Find(x => x.Id == from).FirstOrDefaultAsync();
        if (fromRequest == null) return new List<InviteCodeRequest>();

        if (fromRequest.Status == InviteCodeRequestStatus.Pending)
        {
            // Still in the Pending section: fetch older Pending, then non-Pending if space remains
            var pending = await _inviteCodeRequestCollection
                .Find(x => x.Status == InviteCodeRequestStatus.Pending && x.CreatedAt < fromRequest.CreatedAt)
                .SortByDescending(x => x.CreatedAt)
                .Limit(limit)
                .ToListAsync();

            if (pending.Count >= limit) return pending;

            var remaining = limit - pending.Count;
            var nonPending = await _inviteCodeRequestCollection
                .Find(x => x.Status != InviteCodeRequestStatus.Pending)
                .SortByDescending(x => x.CreatedAt)
                .Limit(remaining)
                .ToListAsync();

            var result = new List<InviteCodeRequest>(pending);
            result.AddRange(nonPending);
            return result;
        }
        else
        {
            // Already in the non-Pending section: only fetch older non-Pending
            return await _inviteCodeRequestCollection
                .Find(x => x.Status != InviteCodeRequestStatus.Pending && x.CreatedAt < fromRequest.CreatedAt)
                .SortByDescending(x => x.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<InviteCodeRequest>>> GetMyInviteCodeRequestsAsync(string userId, string from, int limit)
    {
        limit = Math.Clamp(limit, 1, 100);

        var filter = Builders<InviteCodeRequest>.Filter.Eq(x => x.RequesterId, userId);

        if (!string.IsNullOrEmpty(from))
        {
            var fromRequest = await _inviteCodeRequestCollection.Find(x => x.Id == from).FirstOrDefaultAsync();
            if (fromRequest != null) filter &= Builders<InviteCodeRequest>.Filter.Lt(x => x.CreatedAt, fromRequest.CreatedAt);
        }

        return await _inviteCodeRequestCollection.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<InviteCodeRequest>> GetInviteCodeRequestByIdAsync(string requestId)
    {
        var request = await _inviteCodeRequestCollection.Find(x => x.Id == requestId).FirstOrDefaultAsync();
        if (request == null) return (ErrorType.NotFound, "초대 코드 요청을 찾을 수 없습니다.");
        return request;
    }

    /// <inheritdoc />
    public async Task<Result<InviteCodeRequest>> AcceptInviteCodeRequestAsync(string requestId, string moderatorId, string message)
    {
        var request = await _inviteCodeRequestCollection.Find(x => x.Id == requestId).FirstOrDefaultAsync();
        if (request == null) return (ErrorType.NotFound, "초대 코드 요청을 찾을 수 없습니다.");

        if (request.Status != InviteCodeRequestStatus.Pending) return (ErrorType.Conflict, "이미 처리된 요청입니다.");

        if (request.RequesterId == moderatorId) return (ErrorType.Forbidden, "본인의 요청은 처리할 수 없습니다.");

        // Atomically transition the request to Accepted to prevent double-processing
        var transitionFilter = Builders<InviteCodeRequest>.Filter.Eq(x => x.Id, requestId) & Builders<InviteCodeRequest>.Filter.Eq(x => x.Status, InviteCodeRequestStatus.Pending);
        var transitionUpdate = Builders<InviteCodeRequest>.Update
            .Set(x => x.Status, InviteCodeRequestStatus.Accepted)
            .Set(x => x.ModeratorId, moderatorId)
            .Set(x => x.ModeratorMessage, Utils.SanitizeText(message))
            .Set(x => x.ProcessedAt, DateTime.UtcNow);
        var transitionResult = await _inviteCodeRequestCollection.UpdateOneAsync(transitionFilter, transitionUpdate);

        if (transitionResult.MatchedCount == 0) return (ErrorType.Conflict, "이미 처리된 요청입니다.");

        // Generate invite codes for the requester
        var createResult = await CreateInviteCodesAsync(request.RequesterId, request.RequestedCount, moderatorId);
        if (createResult.IsFailure)
        {
            // Rollback the status transition
            var rollbackUpdate = Builders<InviteCodeRequest>.Update
                .Set(x => x.Status, InviteCodeRequestStatus.Pending)
                .Unset(x => x.ModeratorId)
                .Unset(x => x.ModeratorMessage)
                .Unset(x => x.ProcessedAt);
            var rollbackResult = await _inviteCodeRequestCollection.UpdateOneAsync(x => x.Id == requestId, rollbackUpdate);
            if (rollbackResult.MatchedCount == 0)
                return (ErrorType.ProgramError, "초대 코드 생성 실패 및 롤백 실패가 발생했습니다. 관리자에게 문의하세요.");
            return createResult.CastFailure<InviteCodeRequest>();
        }

        // Record the granted count
        var grantedCountUpdate = Builders<InviteCodeRequest>.Update.Set(x => x.GrantedCount, createResult.Value.Count);
        await _inviteCodeRequestCollection.UpdateOneAsync(x => x.Id == requestId, grantedCountUpdate);

        // Reload the updated request
        request = await _inviteCodeRequestCollection.Find(x => x.Id == requestId).FirstOrDefaultAsync();

        // Notify the requester (push included)
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        await notificationService.SendNotificationsAsync(NotificationType.InviteCodeRequestResult, requestId);

        return request;
    }

    /// <inheritdoc />
    public async Task<Result<InviteCodeRequest>> RejectInviteCodeRequestAsync(string requestId, string moderatorId, string message)
    {
        var request = await _inviteCodeRequestCollection.Find(x => x.Id == requestId).FirstOrDefaultAsync();
        if (request == null) return (ErrorType.NotFound, "초대 코드 요청을 찾을 수 없습니다.");

        if (request.Status != InviteCodeRequestStatus.Pending) return (ErrorType.Conflict, "이미 처리된 요청입니다.");

        if (request.RequesterId == moderatorId) return (ErrorType.Forbidden, "본인의 요청은 처리할 수 없습니다.");

        // Atomically transition the request to Rejected to prevent double-processing
        var transitionFilter = Builders<InviteCodeRequest>.Filter.Eq(x => x.Id, requestId) & Builders<InviteCodeRequest>.Filter.Eq(x => x.Status, InviteCodeRequestStatus.Pending);
        var update = Builders<InviteCodeRequest>.Update
            .Set(x => x.Status, InviteCodeRequestStatus.Rejected)
            .Set(x => x.ModeratorId, moderatorId)
            .Set(x => x.ModeratorMessage, Utils.SanitizeText(message))
            .Set(x => x.ProcessedAt, DateTime.UtcNow);

        var transitionResult = await _inviteCodeRequestCollection.UpdateOneAsync(transitionFilter, update);

        if (transitionResult.MatchedCount == 0) return (ErrorType.Conflict, "이미 처리된 요청입니다.");

        // Reload the updated request
        request = await _inviteCodeRequestCollection.Find(x => x.Id == requestId).FirstOrDefaultAsync();

        // Notify the requester (push included)
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        await notificationService.SendNotificationsAsync(NotificationType.InviteCodeRequestResult, requestId);

        return request;
    }

    /// <inheritdoc />
    public async Task<Result<int>> GetActiveInviteCodeCountAsync(string userId)
    {
        var count = await _inviteCodeCollection.CountDocumentsAsync(x => x.OwnerId == userId && x.IsActive == true);
        return (int)count;
    }

    /// <inheritdoc />
    public async Task<Result<List<InviteCodeResponseDto>>> GenerateInviteCodeResponseDtosAsync(List<InviteCode> codes)
    {
        var usedByUserIds = codes.Where(x => !string.IsNullOrEmpty(x.UsedByUserId)).Select(x => x.UsedByUserId).Distinct().ToList();
        var usedByUsers = new List<UserResponseDto>();

        if (usedByUserIds.Count > 0)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var usersResult = await userService.GenerateUserResponseDtosAsync(usedByUserIds);
            if (usersResult.IsSuccess) usedByUsers = usersResult.Value;
        }

        var results = codes.Select(code =>
        {
            var dto = new InviteCodeResponseDto(code);
            if (!string.IsNullOrEmpty(code.UsedByUserId)) dto.UsedBy = usedByUsers.FirstOrDefault(u => u.UserId == code.UsedByUserId);
            return dto;
        }).ToList();

        return results;
    }

    /// <inheritdoc />
    public async Task<Result<InviteCodeRequestResponseDto>> GenerateInviteCodeRequestResponseDtoAsync(InviteCodeRequest request, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var dto = new InviteCodeRequestResponseDto(request);

        var requesterUserResult = await userService.GenerateUserResponseDtoAsync(request.RequesterId, requesterId);
        if (requesterUserResult.IsSuccess) dto.Requester = requesterUserResult.Value;

        var activeCountResult = await GetActiveInviteCodeCountAsync(request.RequesterId);
        if (activeCountResult.IsSuccess) dto.ActiveCodeCount = activeCountResult.Value;

        return dto;
    }

    /// <inheritdoc />
    public async Task<Result<List<InviteCodeRequestResponseDto>>> GenerateInviteCodeRequestResponseDtosAsync(List<InviteCodeRequest> requests, string requesterId)
    {
        var results = new List<InviteCodeRequestResponseDto>();
        foreach (var request in requests)
        {
            var dtoResult = await GenerateInviteCodeRequestResponseDtoAsync(request, requesterId);
            if (dtoResult.IsFailure) return dtoResult.CastFailure<List<InviteCodeRequestResponseDto>>();
            results.Add(dtoResult.Value);
        }
        return results;
    }

    /// <inheritdoc />
    public async Task<Result> HandleWithdrawAsync(string userId)
    {
        var filter = Builders<InviteCode>.Filter.Eq(x => x.OwnerId, userId) & Builders<InviteCode>.Filter.Eq(x => x.IsActive, true);
        var update = Builders<InviteCode>.Update.Set(x => x.IsActive, false);
        await _inviteCodeCollection.UpdateManyAsync(filter, update);

        return Result.Success();
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        while (true)
        {
            var bytes = RandomNumberGenerator.GetBytes(CodeLength);
            var chars = new char[CodeLength];
            for (int i = 0; i < CodeLength; i++) chars[i] = CodeCharset[bytes[i] % CodeCharset.Length];

            var code = new string(chars);
            var existing = await _inviteCodeCollection.Find(x => x.Code == code).FirstOrDefaultAsync();
            if (existing == null) return code;
        }
    }

    private async Task<string> GenerateUniqueInvitationIdAsync()
    {
        while (true)
        {
            var id = Guid.NewGuid().ToString("N");
            var existing = await _inviteCodeCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existing == null) return id;
        }
    }

    private async Task<string> GenerateUniqueRequestIdAsync()
    {
        while (true)
        {
            var id = Guid.NewGuid().ToString("N");
            var existing = await _inviteCodeRequestCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (existing == null) return id;
        }
    }
}