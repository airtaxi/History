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
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.Forbidden) return Forbid(result.ErrorMessage);
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("{postId}")]
    [Authorize]
    public async Task<IActionResult> CreateComment(string postId, [FromBody] List<BaseContent> contents)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await commentService.CreateCommentAsync(postId, contents, requesterId);
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return Forbid(result.ErrorMessage);

        else return StatusCode(500, result.FullErrorMessage);
    }

}
