using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FriendshipController(IUserService userService, IFriendshipService friendshipService) : ControllerBase
{
    [HttpPost("request/{receiverId}")]
    [Authorize]
    public async Task<IActionResult> SendFriendRequest(string receiverId)
    {
        // Get the user ID from the authenticated user claim
        var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.SendFriendRequestAsync(senderId, receiverId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.SenderEqualsReceiver) return BadRequest("자기 자신에게 친구 요청을 보낼 수 없습니다.");
        else if (result.Error == ErrorType.Conflict) return BadRequest("차단한 또는 무시한 사용자거나 이미 친구 요청을 보낸 사용자입니다.");
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("request/{requesterId}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptFriendRequest(string requesterId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.AcceptFriendRequestAsync(userId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound("해당 사용자의 친구 요청을 찾을 수 없습니다.");
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("request/{requesterId}/decline")]
    [Authorize]
    public async Task<IActionResult> DeclineFriendRequest(string requesterId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.DeclineFriendRequestAsync(userId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound("해당 사용자의 친구 요청을 찾을 수 없습니다.");
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("block/{userToBlockId}")]
    [Authorize]
    public async Task<IActionResult> BlockUser(string userToBlockId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.BlockUserAsync(userId, userToBlockId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound("해당 사용자의 친구 요청을 찾을 수 없습니다.");
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("request/{requesterId}/ignore")]
    [Authorize]
    public async Task<IActionResult> IgnoreRequest(string requesterId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.IgnoreUserAsync(userId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound("해당 사용자의 친구 요청을 찾을 수 없습니다.");
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("remove/{friendId}")]
    [Authorize]
    public async Task<IActionResult> RemoveFriend(string friendId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.RemoveFriendAsync(userId, friendId);
        return result.IsSuccess ? Ok() : StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("block/{userToUnblockId}")]
    [Authorize]
    public async Task<IActionResult> UnblockUser(string userToUnblockId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.UnblockUserAsync(userId, userToUnblockId);
        return result.IsSuccess ? Ok() : StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("ignore/{requesterId}")]
    [Authorize]
    public async Task<IActionResult> UnignoreUser(string requesterId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.UnignoreUserAsync(userId, requesterId);
        return result.IsSuccess ? Ok() : StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPendingRequests()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var pendingRequestsResult = await friendshipService.GetPendingRequestsAsync(userId);
        if (pendingRequestsResult.IsFailure) return StatusCode(500, pendingRequestsResult.FullErrorMessage);

        return Ok(pendingRequestsResult.Value);
    }

    [HttpGet("sent")]
    [Authorize]
    public async Task<IActionResult> GetSentRequests()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var sentRequestsResult = await friendshipService.GetSentRequestsAsync(userId);
        if (sentRequestsResult.IsFailure) return StatusCode(500, sentRequestsResult.FullErrorMessage);

        return Ok(sentRequestsResult.Value);
    }

    [HttpGet("blocked")]
    [Authorize]
    public async Task<IActionResult> GetBlockedUsers()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var blockedUserIdsResult = await friendshipService.GetBlockedUserIdsAsync(userId);
        var blockedUsersResult = await userService.GetUsersByIdsAsync(blockedUserIdsResult.Value);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(blockedUsersResult.Value, userId);
        return Ok(dtosResult.Value);
    }

    [HttpGet("ignored")]
    [Authorize]
    public async Task<IActionResult> GetIgnoredUsers()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var ignoredUserIdsResult = await friendshipService.GetIgnoredUserIdsAsync(userId);
        var ignoredUsersResult = await userService.GetUsersByIdsAsync(ignoredUserIdsResult.Value);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(ignoredUsersResult.Value, userId);
        return Ok(dtosResult.Value);
    }

    [HttpGet("{userId}")]
    [Authorize]
    public async Task<IActionResult> GetFriends(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var userResult = await userService.GetUserByIdAsync(userId);
        if (userResult.Error == ErrorType.NotFound) return NotFound("해당 사용자를 찾을 수 없습니다.");

        var hasAccess = false;

        if (userResult.Value.FriendListDiscoveryOption == Commons.Enums.DiscoveryOption.Everyone) hasAccess = true;
        else if (requesterId == null) hasAccess = false;
        else if (userResult.Value.FriendListDiscoveryOption == Commons.Enums.DiscoveryOption.FriendsOfFriends)
        {
            var friendsOfFriendsResult = await friendshipService.GetFriendsOfFriendIdsAsync(requesterId);
            hasAccess = friendsOfFriendsResult.Value.Contains(requesterId);
        }
        else if (userResult.Value.FriendListDiscoveryOption == Commons.Enums.DiscoveryOption.Friends)
        {
            var areFriendsResult = await friendshipService.AreFriendsAsync(requesterId, userId);
            hasAccess = areFriendsResult.Value;
        }
        else if (userResult.Value.FriendListDiscoveryOption == Commons.Enums.DiscoveryOption.OnlyMe)
        {
            hasAccess = requesterId == userId;
        }

        if (!hasAccess) return Unauthorized("해당 사용자의 친구 목록을 볼 수 없습니다.");

        var friendsResult = await friendshipService.GetFriendIdsAsync(userId);
        var friendUsersResult = await userService.GetUsersByIdsAsync(friendsResult.Value);

        // Remove blocked, ignored friends, and friends who blocked the requester
        if (requesterId != null)
        {
            var bannedUserIds = await friendshipService.GetBannedUserIdsAsync(requesterId);

            friendUsersResult.Value.RemoveAll(x => bannedUserIds.Value.Contains(x.Id));
        }

        var dtosResult = await userService.GenerateUserResponseDtosAsync(friendUsersResult.Value, requesterId);
        return Ok(dtosResult.Value);
    }
}