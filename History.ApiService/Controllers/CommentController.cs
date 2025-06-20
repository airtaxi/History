using DotNet.RateLimiter.ActionFilters;
using History.ApiService.DataTypes;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Security.Claims;
using System.Text.Json;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(Limit = 6, PeriodInSec = 1)]
public class CommentController(ICommentService commentService) : ControllerBase
{
    [HttpGet("{postId}")]
    [ProducesResponseType<List<CommentResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetCommentsByPostId(string postId, [FromQuery] int limit, [FromQuery] string from)
    {
        limit = Math.Clamp(limit, 1, 100);

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await commentService.GetCommentsByPostIdAsync(postId, requesterId, from, limit);
        if (result.IsFailure)
        {
            if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
            else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
            else return StatusCode(500, result.FullErrorMessage);
        }

        var dtosResult = await commentService.GenerateCommentResponseDtosAsync(result.Value, requesterId);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    [HttpPost("{postId}")]
    [Authorize]
    [ProducesResponseType<CommentResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> CreateComment(string postId, [FromForm] DataWithFilesForm request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var contents = JsonSerializer.Deserialize<List<BaseContent>>(request.JsonData);
        var result = await commentService.WriteCommentAsync(postId, contents, requesterId, request.Files);
        if (result.IsSuccess)
        {
            var dtoResut = await commentService.GenerateCommentResponseDtoAsync(result.Value, requesterId);
            return Ok(dtoResut.Value);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPut("{commentId}")]
    [Authorize]
    [ProducesResponseType<CommentResponseDto>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> ModifyComment(string commentId, [FromForm] DataWithFilesForm request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var contents = JsonSerializer.Deserialize<List<BaseContent>>(request.JsonData);
        var result = await commentService.ModifyCommentAsync(commentId, contents, requesterId, request.Files);
        if (result.IsSuccess)
        {
            var comment = await commentService.GetCommentByIdAsync(commentId);
            var dtoResult = await commentService.GenerateCommentResponseDtoAsync(comment.Value, requesterId);
            return Ok(dtoResult.Value);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("{commentId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> DeleteComment(string commentId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await commentService.DeleteCommentAsync(commentId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("{commentId}/like")]
    [Authorize]
    [ProducesResponseType<CommentResponseDto>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> HandleCommentLike(string commentId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await commentService.HandleLikeCommentAsync(commentId, requesterId);
        if (result.IsSuccess)
        {
            var comment = await commentService.GetCommentByIdAsync(commentId);
            var dtoResult = await commentService.GenerateCommentResponseDtoAsync(comment.Value, requesterId);
            return Ok(dtoResult.Value);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
}
