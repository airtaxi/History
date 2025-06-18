using DotNet.RateLimiter.ActionFilters;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(Limit = 6, PeriodInSec = 1)]
public class ModerationController(IModerationService moderationService, IUserService userService) : ControllerBase
{
    [HttpGet("records")]
    [Authorize]
    [ProducesResponseType<List<ModerationRecordResponseDto>>(200)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetModerationRecords([FromQuery] string from = null, [FromQuery] int limit = 10)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        else
        {
            var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
            if (moderatorResult.IsFailure) return StatusCode(500, moderatorResult.FullErrorMessage);
            if (moderatorResult.Value.Rank < Rank.Moderator) return StatusCode(403, "괸리자만 제재 내역을 조회할 수 있습니다.");
        }

        var results = await moderationService.GetModerationRecordsAsync(from, limit);
        var dtosResult = await moderationService.GenerateModerationRecordResponseDtosAsync(results.Value);
        return Ok(dtosResult.Value);
    }

    [HttpPost("delete-post/{postId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> DeletePost(string postId, [FromQuery] string reason, [FromQuery] ReportType reportType)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        if (string.IsNullOrEmpty(reason)) return BadRequest("삭제 사유를 입력해주세요.");

        var result = await moderationService.DeletePostAsync(postId, moderatorId, reason, reportType);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("delete-comment/{commentId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> DeleteComment(string commentId, [FromQuery] string reason, [FromQuery] ReportType reportType)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        if (string.IsNullOrEmpty(reason)) return BadRequest("삭제 사유를 입력해주세요.");

        var result = await moderationService.DeleteCommentAsync(commentId, moderatorId, reason, reportType);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
}
