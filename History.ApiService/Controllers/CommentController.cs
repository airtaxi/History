using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentController(ICommentService commentService) : ControllerBase
{
    [HttpGet("{postId}")]
    public async Task<IActionResult> GetCommentsByPostId(string postId)
    {
        var rawLimit = HttpContext.Request.Query["limit"];
        var fromCommentId = HttpContext.Request.Query["from"];
        var limit = int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 10;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await commentService.GetCommentsByPostIdAsync(postId, requesterId, fromCommentId, limit);
        if (result.IsFailure)
        {
            if (result.Error == ErrorType.Forbidden) return Forbid(result.ErrorMessage);
            else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
            else return StatusCode(500, result.FullErrorMessage);
        }

        var dtosResult = await commentService.GenerateCommentResponseDtosAsync(result.Value, requesterId);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    [HttpPost("{postId}")]
    [Authorize]
    public async Task<IActionResult> CreateComment(string postId, [FromBody] List<BaseContent> contents, IEnumerable<IFormFile> files)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await commentService.CreateCommentAsync(postId, contents, requesterId, files);
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return Forbid(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPut("{commentId}")]
    [Authorize]
    public async Task<IActionResult> ModifyComment(string commentId, [FromBody] List<BaseContent> contents, IEnumerable<IFormFile> files)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await commentService.ModifyCommentAsync(commentId, contents, requesterId, files);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return Forbid(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(string commentId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await commentService.DeleteCommentAsync(commentId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return Forbid(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("{commentId}/like")]
    [Authorize]
    public async Task<IActionResult> HandleLikeComment(string commentId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await commentService.HandleLikeCommentAsync(commentId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return Forbid(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
}
