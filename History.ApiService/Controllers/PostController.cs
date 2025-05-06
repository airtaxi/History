using History.ApiService.DataTypes;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(IPostService postService, IFriendshipService friendshipService) : ControllerBase
{
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPosts(string userId)
    {
        var rawLimit = HttpContext.Request.Query["limit"];
        var fromPostId = HttpContext.Request.Query["from"];
        var limit = int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 10;
        if (limit > 100) limit = 100;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var postsResult = await postService.GetUserPostsAsync(userId, requesterId, fromPostId, limit);
        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses.Value);
    }

    [HttpGet("timeline")]
    [Authorize]
    public async Task<IActionResult> GetTimelinePosts()
    {
        var rawLimit = HttpContext.Request.Query["limit"];
        var fromPostId = HttpContext.Request.Query["from"];
        var limit = int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 10;
        if (limit > 100) limit = 100;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var postsResult = await postService.GetTimelinePostsAsync(requesterId, fromPostId, limit);
        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses.Value);
    }

    [HttpGet("user/{userId}/count")]
    public async Task<IActionResult> GetUserPostsCount(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (requesterId != null)
        {
            var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
            var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
            var requesterBlockerFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);

            if (requesterBlockedFriendIdsResult.Value.Contains(userId)) return Unauthorized("차단한 사용자의 피드를 볼 수 없습니다.");
            else if (requesterIgnoredFriendIdsResult.Value.Contains(userId)) return Unauthorized("무시한 사용자의 피드를 볼 수 없습니다.");
            else if (requesterBlockerFriendIdsResult.Value.Contains(userId)) return Unauthorized("이 사용자의 피드를 볼 수 없습니다.");
        }

        var result = await postService.GetUserPostsCountAsync(userId, requesterId);
        if (result.IsSuccess) return Ok(new GetUserPostsCountResponseDto(result.Value));
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
    
    [HttpPost("ignore/{postId}")]
    [Authorize]
    public async Task<IActionResult> IgnorePost(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.IgnorePostAsync(postId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> WritePost([FromForm] DataWithFilesForm request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var data = JsonSerializer.Deserialize<WritePostRequestDto>(request.JsonData);
        var result = await postService.WritePostAsync(requesterId, data, request.Files);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("{postId}")]
    public async Task<IActionResult> GetPost(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var accessResult = await postService.CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure)
        {
            if (accessResult.Error == ErrorType.Forbidden) return StatusCode(403, accessResult.ErrorMessage);
            else if (accessResult.Error == ErrorType.NotFound) return NotFound(accessResult.ErrorMessage);
            else return StatusCode(500, accessResult.FullErrorMessage);
        }

        var result = await postService.GetPostByIdAsync(postId);
        if (result.IsSuccess)
        {
            var postResponse = await postService.GeneratePostResponseDtoAsync(result.Value, requesterId);
            return Ok(postResponse.Value);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPut("{postId}")]
    [Authorize]
    public async Task<IActionResult> ModifyPost(string postId, [FromForm] DataWithFilesForm request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var data = JsonSerializer.Deserialize<ModifyPostRequestDto>(request.JsonData);
        var result = await postService.ModifyPostAsync(postId, requesterId, data, request.Files);
        if (result.IsSuccess)
        {
            var post = await postService.GetPostByIdAsync(postId);
            var dtoResult = await postService.GeneratePostResponseDtoAsync(post.Value, requesterId);
            return Ok(dtoResult.Value);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("{postId}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.DeletePostAsync(postId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("{postId}/reaction/{type}")]
    [Authorize]
    public async Task<IActionResult> HandlePostReaction(string postId, PostReactionType type)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.HandlePostReactionAsync(postId, requesterId, type);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchPosts()
    {
        var rawLimit = HttpContext.Request.Query["limit"];
        var fromPostId = HttpContext.Request.Query["from"];
        var limit = int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 10;
        if (limit > 100) limit = 100;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var keyword = HttpContext.Request.Query["keyword"].ToString();
        if (string.IsNullOrEmpty(keyword)) return BadRequest("검색어를 입력해주세요.");

        var postsResult = await postService.SearchPostsAsync(requesterId, keyword, fromPostId, limit);
        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses);
    }
}
