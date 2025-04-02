using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Dto;
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

        var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
        var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
        var userBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(userId);
        if (requesterBlockedFriendIdsResult.Value.Contains(userId) || requesterIgnoredFriendIdsResult.Value.Contains(userId)) return Unauthorized("차단 또는 무시한 사용자의 피드를 볼 수 없습니다.");
        else if (userBlockedFriendIdsResult.Value.Contains(requesterId)) return Unauthorized("이 사용자의 피드를 볼 수 없습니다.");

        var postResponses = new List<PostResponseDto>();
        while (postResponses.Count == 0) // All of the posts might not be added to postResponses because of the filtering
        {
            var postsResult = await postService.GetUserPostsAsync(requesterId, userId, fromPostId, limit);
            if (postsResult.Value.Count == 0) break; // No more posts to load. break the loop

            await AppendPostResponsesAsync(postResponses, postsResult, requesterId);
            fromPostId = postsResult.Value.LastOrDefault()?.Id;
        }
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

        var postResponses = new List<PostResponseDto>();
        while (postResponses.Count == 0) // All of the posts might not be added to postResponses because of the filtering
        {
            var posts = await postService.GetTimelinePostsAsync(requesterId, fromPostId, limit);
            if (posts.Value.Count == 0) break; // No more posts to load. break the loop

            await AppendPostResponsesAsync(postResponses, posts, requesterId);
            fromPostId = posts.Value.LastOrDefault()?.Id;
        }
        return Ok(postResponses);
    }

    private async Task AppendPostResponsesAsync(List<PostResponseDto> postResponses, List<Post> posts, string requesterId)
    {
        foreach (var post in posts)
        {
            // Remove blocked, ignored, and friends who blocked the requester
            if (requesterId != null)
            {
                var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
                var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
                var requesterBlockerFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);

                if (requesterBlockedFriendIdsResult.Value.Contains(post.AuthorUserId) || requesterIgnoredFriendIdsResult.Value.Contains(post.AuthorUserId) || requesterBlockerFriendIdsResult.Value.Contains(post.AuthorUserId)) continue;
            }

            var postResponse = new PostResponseDto
            {
                Id = post.Id,
                AuthorUserId = post.AuthorUserId,
                Contents = post.Contents,
                CreatedAt = post.CreatedAt,
                IsRepost = post.IsRepost,
                ModifiedAt = post.ModifiedAt
            };

            await SetParentPostAsync(postResponse, post.ParentPostId, requesterId);

            postResponses.Add(postResponse);
        }
    }

    private async Task SetParentPostAsync(PostResponseDto postResponse, string parentPostId, string requesterId)
    {
        if (parentPostId == null) return;

        var parentPostResult = await postService.GetPostByIdAsync(parentPostId);
        if (parentPostResult != null)
        {
            // If the requester is blocked, ignored, or blocked by the parent post author, do not add the parent post
            if (requesterId != null)
            {
                var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
                var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
                var requesterBlockerFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);

                if (requesterBlockedFriendIdsResult.Value.Contains(parentPostResult.Value.AuthorUserId) || requesterIgnoredFriendIdsResult.Value.Contains(parentPostResult.Value.AuthorUserId) || requesterBlockerFriendIdsResult.Value.Contains(parentPostResult.Value.AuthorUserId)) return;
            }

            postResponse.ParentPost = new PostResponseDto
            {
                Id = parentPostResult.Value.Id,
                AuthorUserId = parentPostResult.Value.AuthorUserId,
                Contents = parentPostResult.Value.Contents,
                CreatedAt = parentPostResult.Value.CreatedAt,
                IsRepost = parentPostResult.Value.IsRepost,
                ModifiedAt = parentPostResult.Value.ModifiedAt
            };
        }
    }
}
