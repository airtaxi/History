using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.RequestDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var postsResult = await postService.GetUserPostsAsync(requesterId, userId, fromPostId, limit);
        if (postsResult.IsFailure) return StatusCode(500, postsResult.FullErrorMessage);

        var postResponses = await postService.GeneratePostResponsesDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses);
    }

    [HttpGet("timeline")]
    [Authorize]
    public async Task<IActionResult> GetTimelinePosts()
    {
        var rawLimit = HttpContext.Request.Query["limit"];
        var fromPostId = HttpContext.Request.Query["from"];
        var limit = int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 10;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var postsResult = await postService.GetTimelinePostsAsync(requesterId, fromPostId, limit);
        if (postsResult.IsFailure) return StatusCode(500, postsResult.FullErrorMessage);

        var postResponses = await postService.GeneratePostResponsesDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses);
    }

    [HttpGet("user/{userId}/count")]
    public async Task<IActionResult> GetUserPostsCount(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
        var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
        var userBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(userId);

        if (requesterBlockedFriendIdsResult.Value.Contains(userId) || requesterIgnoredFriendIdsResult.Value.Contains(userId)) return Unauthorized("차단 또는 무시한 사용자의 피드를 볼 수 없습니다.");
        else if (userBlockedFriendIdsResult.Value.Contains(requesterId)) return Unauthorized("이 사용자의 피드를 볼 수 없습니다.");

        var count = await postService.GetUserPostsCountAsync(userId);
        if (count.IsFailure) return StatusCode(500, count.FullErrorMessage);

        return Ok(new GetUserPostsCountResponseDto(count.Value));
    }
    
    [HttpPost("ignore")]
    [Authorize]
    public async Task<IActionResult> IgnorePost([FromBody] IgnorePostRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.IgnorePostAsync(requesterId, request.PostId);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        return Ok();
    }
}
