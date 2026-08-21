using DotNet.RateLimiter.ActionFilters;
using History.ApiService.Helpers;
using History.ApiService.Services;
using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes.RequestDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(Limit = 6, PeriodInSec = 1)]
public class KakaoStoryController(IUserService userService, KakaoStoryPollingService pollingService, KakaoStoryWorkerClient workerClient) : ControllerBase
{
    /// <summary>
    /// Uploads the user's Kakao Story KAuth tokens so the server-side polling
    /// service can fetch notifications on their behalf. Tokens are validated
    /// against Kakao (via the worker proxy) and kept in memory only.
    /// </summary>
    [HttpPost("token")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> UpdateToken([FromBody] UpdateKakaoStoryTokenRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        if (request == null || string.IsNullOrEmpty(request.IdToken)) return BadRequest("토큰이 비어있습니다.");
        if (!workerClient.IsConfigured) return StatusCode(500, "카카오스토리 폴링 서비스가 구성되지 않았습니다.");

        var userResult = await userService.GetUserByIdAsync(userId);
        if (userResult.IsFailure) return StatusCode(500, userResult.FullErrorMessage);

        // Validate the tokens against Kakao before registering the session.
        var validationResult = await ValidateTokenAsync(request.IdToken);
        if (validationResult == null) return BadRequest("토큰이 만료되었습니다. 다시 로그인해주세요.");

        pollingService.RegisterSession(userId, request.IdToken, request.IsFavoriteFriendNotificationEnabled, request.IsEmotionNotificationEnabled);
        return Ok();
    }

    /// <summary>
    /// Removes the user's Kakao Story session from the polling service.
    /// </summary>
    [HttpDelete("token")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public IActionResult DeleteToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        pollingService.RemoveSession(userId);
        return Ok();
    }

    private async Task<string> ValidateTokenAsync(string idToken)
    {
        var request = new DataTypes.KakaoStoryWorkerBatchRequest
        {
            Requests = [new DataTypes.KakaoStoryWorkerRequest
            {
                Url = "https://story.kakao.com/a/settings/profile",
                IdToken = idToken
            }]
        };

        try
        {
            var response = await workerClient.PostBatchAsync(request);
            var workerResponse = response.Responses.FirstOrDefault();
            if (workerResponse == null || workerResponse.Status != 200) return null;

            // The profile response carries the user id; parse it to confirm the session is valid.
            using var document = System.Text.Json.JsonDocument.Parse(workerResponse.Body);
            return document.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Kakao Story token validation failed: {exception.Message}");
            return null;
        }
    }
}
