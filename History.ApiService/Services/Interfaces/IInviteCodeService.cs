using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;

namespace History.ApiService.Services.Interfaces;

public interface IInviteCodeService
{
    /// <summary>
    /// Gets invite codes owned by the specified user (only active codes for self, all for moderator).
    /// </summary>
    public Task<Result<List<InviteCode>>> GetMyInviteCodesAsync(string userId, string from, int limit);

    /// <summary>
    /// Gets all invite codes owned by the specified user. Caller is responsible for access control.
    /// </summary>
    public Task<Result<List<InviteCode>>> GetInviteCodesByOwnerIdAsync(string ownerId, string requesterId, string from, int limit);

    /// <summary>
    /// Creates invite codes and assigns them to the owner. Called by moderator+ or on request acceptance.
    /// </summary>
    public Task<Result<List<InviteCode>>> CreateInviteCodesAsync(string ownerId, int count, string moderatorId);

    /// <summary>
    /// Issues the initial 7 invite codes to a newly registered user.
    /// </summary>
    public Task<Result> IssueInitialInviteCodesAsync(string userId);

    /// <summary>
    /// Validates an invite code during registration without consuming it. Returns failure if invalid or already used.
    /// </summary>
    public Task<Result> ValidateInviteCodeAsync(string code);

    /// <summary>
    /// Atomically consumes (deactivates) an invite code during registration. Returns failure if the code was already used.
    /// </summary>
    public Task<Result> ConsumeInviteCodeAsync(string code, string newUserId);

    /// <summary>
    /// Creates a new invite code request from a user. Only allowed when the user has zero active codes.
    /// </summary>
    public Task<Result<InviteCodeRequest>> CreateInviteCodeRequestAsync(string requesterId, string reason, int count);

    /// <summary>
    /// Gets all invite code requests (moderator+ only). Pending requests are sorted first, then by newest.
    /// </summary>
    public Task<Result<List<InviteCodeRequest>>> GetInviteCodeRequestsAsync(string moderatorId, string from, int limit);

    /// <summary>
    /// Gets invite code requests submitted by the specified user.
    /// </summary>
    public Task<Result<List<InviteCodeRequest>>> GetMyInviteCodeRequestsAsync(string userId, string from, int limit);

    /// <summary>
    /// Gets a single invite code request by ID.
    /// </summary>
    public Task<Result<InviteCodeRequest>> GetInviteCodeRequestByIdAsync(string requestId);

    /// <summary>
    /// Accepts an invite code request, auto-generates codes, and notifies the requester.
    /// </summary>
    public Task<Result<InviteCodeRequest>> AcceptInviteCodeRequestAsync(string requestId, string moderatorId, string message);

    /// <summary>
    /// Rejects an invite code request and notifies the requester.
    /// </summary>
    public Task<Result<InviteCodeRequest>> RejectInviteCodeRequestAsync(string requestId, string moderatorId, string message);

    /// <summary>
    /// Gets the count of active (unused) invite codes for a user.
    /// </summary>
    public Task<Result<int>> GetActiveInviteCodeCountAsync(string userId);

    /// <summary>
    /// Generates response DTOs for invite codes.
    /// </summary>
    public Task<Result<List<InviteCodeResponseDto>>> GenerateInviteCodeResponseDtosAsync(List<InviteCode> codes);

    /// <summary>
    /// Generates a response DTO for a single invite code request.
    /// </summary>
    public Task<Result<InviteCodeRequestResponseDto>> GenerateInviteCodeRequestResponseDtoAsync(InviteCodeRequest request, string requesterId);

    /// <summary>
    /// Generates response DTOs for a list of invite code requests.
    /// </summary>
    public Task<Result<List<InviteCodeRequestResponseDto>>> GenerateInviteCodeRequestResponseDtosAsync(List<InviteCodeRequest> requests, string requesterId);

    /// <summary>
    /// Deactivates all active invite codes owned by the user on withdrawal.
    /// </summary>
    public Task<Result> HandleWithdrawAsync(string userId);
}