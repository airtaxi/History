using History.ApiService.DataTypes;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostController(IPostService postService, IFriendshipService friendshipService, INotificationService notificationService) : ControllerBase
{
    [HttpGet("user/{userId}")]
    [ProducesResponseType<List<PostResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetUserPosts(string userId, [FromQuery] int limit, [FromQuery] string from)
    {
        limit = Math.Clamp(limit, 1, 100);

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var postsResult = await postService.GetUserPostsAsync(userId, requesterId, from, limit);
        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);

        // Mark posts with unread notifications for self profile
        if (userId == requesterId)
        {
            var postIds = postResponses.Value.Select(p => p.Id);
            var unreadPostIdsResult = await notificationService.GetPostIdsWithUnreadNotificationsAsync(requesterId, postIds);
            if (unreadPostIdsResult.IsSuccess)
                foreach (var postResponse in postResponses.Value)
                    postResponse.HasUnreadNotification = unreadPostIdsResult.Value.Contains(postResponse.Id);
        }

        return Ok(postResponses.Value);
    }

    [HttpGet("timeline")]
    [Authorize]
    [ProducesResponseType<List<PostResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetTimelinePosts([FromQuery] int limit, [FromQuery] string from)
    {
        limit = Math.Clamp(limit, 1, 100);

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var postsResult = await postService.GetTimelinePostsAsync(requesterId, from, limit);
        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses.Value);
    }

    [HttpGet("user/{userId}/count")]
    [ProducesResponseType<GetUserPostsCountResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
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

    [HttpPut("bulk/discovery-option")]
    [Authorize]
    [ProducesResponseType<long>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> BulkChangeDiscoveryOption([FromBody] History.Commons.DataTypes.RequestDtos.BulkChangeDiscoveryOptionRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        if (request.To == DiscoveryOption.SelectedUsers || request.To == DiscoveryOption.UnselectedUsers)
            return BadRequest("특정 친구 (비)공개 공개 범위로는 일괄 전환할 수 없습니다.");

        var result = await postService.BulkChangeDiscoveryOptionAsync(requesterId, request.From, request.To);
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("bulk")]
    [Authorize]
    [ProducesResponseType<long>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> BulkDeletePosts([FromQuery] DiscoveryOption? discoveryOption)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.BulkDeletePostsAsync(requesterId, discoveryOption);
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPut("bulk/discovery-option/by-ids")]
    [Authorize]
    [ProducesResponseType<long>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> BulkChangeDiscoveryOptionByPostIds([FromBody] BulkChangeDiscoveryOptionByPostIdsRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        if (request.PostIds == null || request.PostIds.Count == 0) return BadRequest("게시글 ID가 제공되지 않았습니다.");

        if (request.To == DiscoveryOption.SelectedUsers || request.To == DiscoveryOption.UnselectedUsers)
            return BadRequest("특정 친구 (비)공개 공개 범위로는 일괄 전환할 수 없습니다.");

        var result = await postService.BulkChangeDiscoveryOptionAsync(requesterId, request.PostIds, request.To);
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("bulk/by-ids")]
    [Authorize]
    [ProducesResponseType<long>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> BulkDeletePostsByPostIds([FromBody] BulkDeletePostsByPostIdsRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        if (request.PostIds == null || request.PostIds.Count == 0) return BadRequest("게시글 ID가 제공되지 않았습니다.");

        var result = await postService.BulkDeletePostsAsync(requesterId, request.PostIds);
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
    
    [HttpPost("ignore/{postId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
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

    [HttpPost("{postId}/mute-notifications")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> MuteNotifications(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.MuteNotificationsAsync(postId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("{postId}/mute-notifications")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> UnmuteNotifications(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.UnmuteNotificationsAsync(postId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("bookmark/{postId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> BookmarkPost(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.BookmarkPostAsync(postId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("bookmark/{postId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> UnbookmarkPost(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.UnbookmarkPostAsync(postId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("bookmarks")]
    [Authorize]
    [ProducesResponseType<List<PostResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetBookmarkedPosts([FromQuery] int limit = 20, [FromQuery] string from = null)
    {
        limit = Math.Clamp(limit, 1, 100);

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var postsResult = await postService.GetBookmarkedPostsAsync(requesterId, from, limit);
        if (postsResult.IsFailure)
        {
            if (postsResult.Error == ErrorType.BadRequest) return BadRequest(postsResult.ErrorMessage);
            else return StatusCode(500, postsResult.FullErrorMessage);
        }

        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses.Value);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType<PostResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> WritePost([FromForm] DataWithFilesForm request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var data = JsonSerializer.Deserialize<WritePostRequestDto>(request.JsonData);
        var result = await postService.WritePostAsync(requesterId, data, request.Files);
        if (result.IsSuccess)
        {
            var postDtoResult = await postService.GeneratePostResponseDtoAsync(result.Value, requesterId);
            if (postDtoResult.IsSuccess) return Ok(postDtoResult.Value);
            return Ok();
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("{postId}")]
    [ProducesResponseType<PostResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
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
    [ProducesResponseType<PostResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
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
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
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

    [HttpPost("{postId}/repost")]
    [Authorize]
    [ProducesResponseType<PostResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> HandleRepost(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.HandleRepostAsync(postId, requesterId);
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

    [HttpPost("{postId}/reaction/{type}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> HandlePostReaction(string postId, ReactionType type)
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
    [ProducesResponseType<List<PostResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> SearchPosts([FromQuery] int limit, [FromQuery] string from, [FromQuery] string keyword)
    {
        limit = Math.Clamp(limit, 1, 100);

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var postsResult = await postService.SearchPostsAsync(keyword, requesterId, from, limit);
        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses.Value);
    }

    [HttpPut("{postId}/discovery-option")]
    [Authorize]
    [ProducesResponseType<List<PostResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> ChangeDiscoveryOption(string postId, [FromBody] ChangeDiscoveryOptionRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.ChangeDiscoveryOptionAsync(postId, requesterId, request.NewDiscoveryOption, request.SelectedUserIds);
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

    [HttpPost("fill-external-url-content")]
    [Authorize]
    [ProducesResponseType<ExternalUrlContent>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> FillExternalUrlContent([FromBody] ExternalUrlContent externalUrlContent)
    {
        var result = await postService.FillExternalUrlContentAsync(externalUrlContent);
        if (result.IsSuccess) return Ok(externalUrlContent);
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("public-post")]
    [ProducesResponseType<List<PostResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetPublicPosts([FromQuery] int limit, [FromQuery] string from)
    {
        limit = Math.Clamp(limit, 1, 100);
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var postsResult = await postService.GetPublicPostsAsync(requesterId, from, limit);
        if (postsResult.IsFailure)
        {
            if (postsResult.Error == ErrorType.NotFound) return NotFound(postsResult.ErrorMessage);
            else if (postsResult.Error == ErrorType.Unauthorized) return Unauthorized(postsResult.ErrorMessage);
            else if (postsResult.Error == ErrorType.BadRequest) return BadRequest(postsResult.ErrorMessage);
            else return StatusCode(500, postsResult.FullErrorMessage);
        }
        
        var postResponses = await postService.GeneratePostResponseDtosAsync(postsResult.Value, requesterId);
        return Ok(postResponses.Value);
    }

    [HttpPost("public-post/{postId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> WritePublicPost(string postId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.WritePublicPostAsync(postId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("{postId}/poll/{pollId}/vote")]
    [Authorize]
    [ProducesResponseType<PostResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> VotePoll(string postId, string pollId, [FromBody] VotePollRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.VotePollAsync(postId, pollId, requesterId, request);
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

    [HttpGet("{postId}/poll/{pollId}/voters")]
    [Authorize]
    [ProducesResponseType<List<PollVoterResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetPollVoters(string postId, string pollId, [FromQuery] int optionIndex)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await postService.GetPollVotersAsync(postId, pollId, optionIndex, requesterId);
        if (result.IsSuccess) return Ok(result.Value);
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }
}
