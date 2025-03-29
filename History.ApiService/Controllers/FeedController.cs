using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedController(IFeedService feedService, IFriendshipService friendshipService) : ControllerBase
{
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserFeeds(string userId)
    {
        var rawLimit = HttpContext.Request.Query["limit"];
        var fromFeedId = HttpContext.Request.Query["from"];
        var limit = int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 10;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var requesterBlockedFriendIds = await friendshipService.GetBlockedUserIdsAsync(requesterId);
        var requesterIgnoredFriendIds = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
        var userBlockedFriendIds = await friendshipService.GetBlockedUserIdsAsync(userId);
        if (requesterBlockedFriendIds.Contains(userId) || requesterIgnoredFriendIds.Contains(userId)) return Unauthorized("차단 또는 무시한 사용자의 피드를 볼 수 없습니다.");
        else if (userBlockedFriendIds.Contains(requesterId)) return Unauthorized("이 사용자의 피드를 볼 수 없습니다.");

        var feedResponses = new List<FeedResponseDto>();
        while (feedResponses.Count == 0) // All of the feeds might not be added to feedResponses because of the filtering
        {
            var feeds = await feedService.GetUserFeedsAsync(requesterId, userId, fromFeedId, limit);
            if (feeds.Count == 0) break; // No more feeds to load. break the loop

            await AppendFeedResponsesAsync(feedResponses, feeds, requesterId);
            fromFeedId = feeds.LastOrDefault()?.Id;
        }
        return Ok(feedResponses);
    }

    [HttpGet("timeline")]
    [Authorize]
    public async Task<IActionResult> GetTimelineFeeds()
    {
        var rawLimit = HttpContext.Request.Query["limit"];
        var fromFeedId = HttpContext.Request.Query["from"];
        var limit = int.TryParse(rawLimit, out var parsedLimit) ? parsedLimit : 10;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var feedResponses = new List<FeedResponseDto>();
        while (feedResponses.Count == 0) // All of the feeds might not be added to feedResponses because of the filtering
        {
            var feeds = await feedService.GetTimelineFeedsAsync(requesterId, fromFeedId, limit);
            if (feeds.Count == 0) break; // No more feeds to load. break the loop

            await AppendFeedResponsesAsync(feedResponses, feeds, requesterId);
            fromFeedId = feeds.LastOrDefault()?.Id;
        }
        return Ok(feedResponses);
    }

    private async Task AppendFeedResponsesAsync(List<FeedResponseDto> feedResponses, List<Feed> feeds, string requesterId)
    {
        foreach (var feed in feeds)
        {
            if(requesterId != null)
            {
                var requesterBlockedFriendIds = await friendshipService.GetBlockedUserIdsAsync(requesterId);
                var requesterIgnoredFriendIds = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
                if (requesterBlockedFriendIds.Contains(feed.AuthorUserId) && requesterIgnoredFriendIds.Contains(feed.AuthorUserId)) continue;
                else
                {
                    var feedAuthorBlockedFriendIds = await friendshipService.GetBlockedUserIdsAsync(feed.AuthorUserId);
                    if (feedAuthorBlockedFriendIds.Contains(requesterId)) continue;
                }
            }

            var feedResponse = new FeedResponseDto
            {
                Id = feed.Id,
                AuthorUserId = feed.AuthorUserId,
                Contents = feed.Contents,
                CreatedAt = feed.CreatedAt,
                IsRepost = feed.IsRepost,
                ModifiedAt = feed.ModifiedAt
            };

            await SetParentFeedAsync(feedResponse, feed.ParentFeedId, requesterId);

            feedResponses.Add(feedResponse);
        }
    }

    private async Task SetParentFeedAsync(FeedResponseDto feedResponse, string parentFeedId, string requesterId)
    {
        if (parentFeedId == null) return;

        var parentFeed = await feedService.GetFeedByIdAsync(parentFeedId);
        if (parentFeed != null)
        {
            bool addParentFeed = true;
            if (requesterId != null)
            {
                var requesterBlockedFriendIds = await friendshipService.GetBlockedUserIdsAsync(requesterId);
                var requesterIgnoredFriendIds = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
                if (requesterBlockedFriendIds.Contains(parentFeed.AuthorUserId) && requesterIgnoredFriendIds.Contains(parentFeed.AuthorUserId)) addParentFeed = false;
                else
                {
                    var parentFeedAuthorBlockedFriendIds = await friendshipService.GetBlockedUserIdsAsync(parentFeed.AuthorUserId);
                    if (parentFeedAuthorBlockedFriendIds.Contains(requesterId)) addParentFeed = false;
                }
            }

            if (addParentFeed)
            {
                feedResponse.ParentFeed = new FeedResponseDto
                {
                    Id = parentFeed.Id,
                    AuthorUserId = parentFeed.AuthorUserId,
                    Contents = parentFeed.Contents,
                    CreatedAt = parentFeed.CreatedAt,
                    IsRepost = parentFeed.IsRepost,
                    ModifiedAt = parentFeed.ModifiedAt
                };
            }
        }
    }
}
