using DotNet.RateLimiter.ActionFilters;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(Limit = 6, PeriodInSec = 1)]
public class InviteCodeController(IInviteCodeService inviteCodeService, IUserService userService) : ControllerBase
{
    /// <summary>
    /// Get my invite codes (active and used).
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<List<History.Commons.DataTypes.ResponseDtos.InviteCodeResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetMyInviteCodes([FromQuery] string from = null, [FromQuery] int limit = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var codesResult = await inviteCodeService.GetMyInviteCodesAsync(userId, from, limit);
        if (codesResult.IsFailure) return StatusCode(500, codesResult.FullErrorMessage);

        var dtosResult = await inviteCodeService.GenerateInviteCodeResponseDtosAsync(codesResult.Value);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    /// <summary>
    /// Get the count of active invite codes for a user.
    /// </summary>
    [HttpGet("active-count")]
    [Authorize]
    [ProducesResponseType<int>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetActiveInviteCodeCount([FromQuery] string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        if (string.IsNullOrEmpty(userId)) userId = requesterId;

        // Only allow querying own count or when requester is moderator+
        if (userId != requesterId)
        {
            var requester = await userService.GetUserByIdAsync(requesterId);
            if (requester?.Value.Rank < Rank.Moderator) return StatusCode(403, "권한이 없습니다.");
        }

        var result = await inviteCodeService.GetActiveInviteCodeCountAsync(userId);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        return Ok(result.Value);
    }

    /// <summary>
    /// Request invite codes (only when user has zero active codes).
    /// Moderators and above bypass the request queue and receive generated codes immediately.
    /// </summary>
    [HttpPost("request")]
    [Authorize]
    [ProducesResponseType<List<History.Commons.DataTypes.ResponseDtos.InviteCodeResponseDto>>(200)]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(409)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> RequestInviteCodes([FromBody] CreateInviteCodeRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Moderators and above generate invite codes immediately instead of creating a request
        var requester = await userService.GetUserByIdAsync(userId);
        if (requester?.Value.Rank >= Rank.Moderator)
        {
            var createResult = await inviteCodeService.CreateInviteCodesAsync(userId, request.Count, userId);
            if (createResult.IsFailure)
            {
                if (createResult.Error == ErrorType.NotFound) return NotFound(createResult.ErrorMessage);
                else if (createResult.Error == ErrorType.BadRequest) return BadRequest(createResult.ErrorMessage);
                else return StatusCode(500, createResult.FullErrorMessage);
            }

            var dtosResult = await inviteCodeService.GenerateInviteCodeResponseDtosAsync(createResult.Value);
            if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

            return Ok(dtosResult.Value);
        }

        var result = await inviteCodeService.CreateInviteCodeRequestAsync(userId, request.Reason, request.Count);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Conflict) return Conflict(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Get my invite code requests.
    /// </summary>
    [HttpGet("request/mine")]
    [Authorize]
    [ProducesResponseType<List<History.Commons.DataTypes.ResponseDtos.InviteCodeRequestResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetMyInviteCodeRequests([FromQuery] string from = null, [FromQuery] int limit = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var requestsResult = await inviteCodeService.GetMyInviteCodeRequestsAsync(userId, from, limit);
        if (requestsResult.IsFailure) return StatusCode(500, requestsResult.FullErrorMessage);

        var dtosResult = await inviteCodeService.GenerateInviteCodeRequestResponseDtosAsync(requestsResult.Value, userId);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    /// <summary>
    /// Create invite codes for a user (moderator+ only).
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType<List<History.Commons.DataTypes.ResponseDtos.InviteCodeResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> CreateInviteCodes([FromBody] CreateInviteCodeByAdminRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return StatusCode(403, "권한이 없습니다.");

        var result = await inviteCodeService.CreateInviteCodesAsync(request.OwnerId, request.Count, requesterId);
        if (result.IsFailure)
        {
            if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
            else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
            else return StatusCode(500, result.FullErrorMessage);
        }

        var dtosResult = await inviteCodeService.GenerateInviteCodeResponseDtosAsync(result.Value);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    /// <summary>
    /// Get all invite code requests, Pending first (moderator+ only).
    /// </summary>
    [HttpGet("requests")]
    [Authorize]
    [ProducesResponseType<List<History.Commons.DataTypes.ResponseDtos.InviteCodeRequestResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetInviteCodeRequests([FromQuery] string from = null, [FromQuery] int limit = 20)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return StatusCode(403, "권한이 없습니다.");

        var requestsResult = await inviteCodeService.GetInviteCodeRequestsAsync(requesterId, from, limit);
        if (requestsResult.IsFailure) return StatusCode(500, requestsResult.FullErrorMessage);

        var dtosResult = await inviteCodeService.GenerateInviteCodeRequestResponseDtosAsync(requestsResult.Value, requesterId);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    /// <summary>
    /// Accept an invite code request, auto-generating codes (moderator+ only).
    /// </summary>
    [HttpPost("requests/{requestId}/accept")]
    [Authorize]
    [ProducesResponseType<History.Commons.DataTypes.ResponseDtos.InviteCodeRequestResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(409)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> AcceptInviteCodeRequest(string requestId, [FromBody] ProcessInviteCodeRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return StatusCode(403, "권한이 없습니다.");

        var result = await inviteCodeService.AcceptInviteCodeRequestAsync(requestId, requesterId, request?.Message);
        if (result.IsSuccess)
        {
            var dtoResult = await inviteCodeService.GenerateInviteCodeRequestResponseDtoAsync(result.Value, requesterId);
            if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
            return StatusCode(500, dtoResult.FullErrorMessage);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Conflict) return Conflict(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Reject an invite code request (moderator+ only).
    /// </summary>
    [HttpPost("requests/{requestId}/reject")]
    [Authorize]
    [ProducesResponseType<History.Commons.DataTypes.ResponseDtos.InviteCodeRequestResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(409)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> RejectInviteCodeRequest(string requestId, [FromBody] ProcessInviteCodeRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return StatusCode(403, "권한이 없습니다.");

        var result = await inviteCodeService.RejectInviteCodeRequestAsync(requestId, requesterId, request?.Message);
        if (result.IsSuccess)
        {
            var dtoResult = await inviteCodeService.GenerateInviteCodeRequestResponseDtoAsync(result.Value, requesterId);
            if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
            return StatusCode(500, dtoResult.FullErrorMessage);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Conflict) return Conflict(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
}