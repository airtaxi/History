using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FriendshipController(IUserService userService, IFriendshipService friendshipService) : ControllerBase
{
    [HttpPost("request/{receiverId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(409)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> SendFriendRequest(string receiverId)
    {
        // Get the user ID from the authenticated user claim
        var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.SendFriendRequestAsync(senderId, receiverId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Conflict) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("request/{userIdToAccept}/accept")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> AcceptFriendRequest(string userIdToAccept)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.AcceptFriendRequestAsync(userId, userIdToAccept);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("request/{userIdToDecline}/decline")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> DeclineFriendRequest(string userIdToDecline)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.DeclineFriendRequestAsync(userId, userIdToDecline);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("request/{userIdToCancel}/cancel")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> CancelFriendRequest(string userIdToCancel)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.CancelFriendRequestAsync(userId, userIdToCancel);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("block/{userIdToBlock}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> BlockUser(string userIdToBlock)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.BlockUserAsync(userId, userIdToBlock);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("ignore/{userIdToIgnore}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> IgnoreUser(string userIdToIgnore)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.IgnoreUserAsync(userId, userIdToIgnore);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("remove/{userIdToRemove}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> RemoveFriend(string userIdToRemove)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.RemoveFriendAsync(userId, userIdToRemove);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("block/{blockedUserId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> UnblockUser(string blockedUserId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.UnblockUserAsync(userId, blockedUserId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("ignore/{ignoredUserId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> UnignoreUser(string ignoredUserId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await friendshipService.UnignoreUserAsync(userId, ignoredUserId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("pending")]
    [Authorize]
    [ProducesResponseType<List<UserResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetPendingRequests()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var pendingRequestsResult = await friendshipService.GetPendingRequestsAsync(userId);
        if (pendingRequestsResult.IsFailure) return StatusCode(500, pendingRequestsResult.FullErrorMessage);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(pendingRequestsResult.Value.Select(x => x.FriendId), userId);
        return Ok(dtosResult.Value);
    }

    [HttpGet("waiting")]
    [Authorize]
    [ProducesResponseType<List<UserResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetWaitingRequests()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var waitingRequestsResult = await friendshipService.GetWaitingRequestsAsync(userId);
        if (waitingRequestsResult.IsFailure) return StatusCode(500, waitingRequestsResult.FullErrorMessage);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(waitingRequestsResult.Value.Select(x => x.FriendId), userId);
        return Ok(dtosResult.Value);
    }

    [HttpGet("blocked")]
    [Authorize]
    [ProducesResponseType<List<UserResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetBlockedUsers()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var blockedUserIdsResult = await friendshipService.GetBlockedUserIdsAsync(userId);
        if (blockedUserIdsResult.IsFailure) return StatusCode(500, blockedUserIdsResult.FullErrorMessage);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(blockedUserIdsResult.Value, userId);
        return Ok(dtosResult.Value);
    }

    [HttpGet("ignored")]
    [Authorize]
    [ProducesResponseType<List<UserResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetIgnoredUsers()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var ignoredUserIdsResult = await friendshipService.GetIgnoredUserIdsAsync(userId);
        if (ignoredUserIdsResult.IsFailure) return StatusCode(500, ignoredUserIdsResult.FullErrorMessage);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(ignoredUserIdsResult.Value, userId);
        return Ok(dtosResult.Value);
    }

    [HttpGet("{userId}")]
    [Authorize]
    [ProducesResponseType<List<UserResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetFriends(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var friendIdsResult = await friendshipService.GetUserFriendIdsAsync(userId, requesterId);
        if (friendIdsResult.IsSuccess)
        {
            var dtosResult = await userService.GenerateUserResponseDtosAsync(friendIdsResult.Value, requesterId);
            return Ok(dtosResult.Value);
        }
        else if (friendIdsResult.Error == ErrorType.NotFound) return NotFound(friendIdsResult.ErrorMessage);
        else if (friendIdsResult.Error == ErrorType.Forbidden) return StatusCode(403, friendIdsResult.ErrorMessage);
        else return StatusCode(500, friendIdsResult.FullErrorMessage);
    }

    [HttpPost("toggle-favorite/{userId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(409)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> ToggleFavorite(string userId)
    {
        // Get the user ID from the authenticated user claim
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var areFriendsResult = await friendshipService.AreFriendsAsync(requesterId, userId);
        if (areFriendsResult.IsFailure) return StatusCode(500, areFriendsResult.FullErrorMessage);

        if (!areFriendsResult.Value) return BadRequest("친구만 관심 친구로 설정할 수 있습니다.");

        var isFavoriteResult = await friendshipService.IsFavoriteFriendAsync(requesterId, userId);
        if (isFavoriteResult.IsFailure) return StatusCode(500, isFavoriteResult.FullErrorMessage);

        if (isFavoriteResult.Value)
        {
            var result = await friendshipService.AddFavoriteFriendAsync(requesterId, userId);
            if (result.IsSuccess) return Ok();
            else if (result.Error == ErrorType.Conflict) return Conflict(result.ErrorMessage);
            else return StatusCode(500, result.FullErrorMessage);
        }
        else
        {
            var result = await friendshipService.RemoveFavoriteFriendAsync(requesterId, userId);
            if (result.IsSuccess) return Ok();
            else if (result.Error == ErrorType.Conflict) return Conflict(result.ErrorMessage);
            else return StatusCode(500, result.FullErrorMessage);
        }
    }
}