using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModerationController(IModerationService moderationService) : ControllerBase
{
    [HttpPost("delete-post/{postId}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(string postId, [FromQuery] string reason)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        if (string.IsNullOrEmpty(reason)) return BadRequest("삭제 사유를 입력해주세요.");

        var result = await moderationService.DeletePostAsync(postId, moderatorId, reason);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("delete-comment/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(string commentId, [FromQuery] string reason)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        if (string.IsNullOrEmpty(reason)) return BadRequest("삭제 사유를 입력해주세요.");

        var result = await moderationService.DeleteCommentAsync(commentId, moderatorId, reason);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
}
