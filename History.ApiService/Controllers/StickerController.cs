using DotNet.RateLimiter.ActionFilters;
using History.ApiService.DataTypes;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(Limit = 6, PeriodInSec = 1)]
public class StickerController(IStickerService stickerService) : ControllerBase
{
    /// <summary>
    /// Creates a sticker.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType<StickerResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> CreateSticker(
        [FromForm] string name,
        [FromForm] string category,
        [FromForm] string description,
        [FromForm] bool isPrivate,
        [FromForm] IFormFile iconFile,
        [FromForm] List<IFormFile> assetFiles)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await stickerService.CreateStickerAsync(requesterId, name, category, description, isPrivate, iconFile, assetFiles);
        if (result.IsSuccess)
        {
            var dtoResult = await stickerService.GenerateStickerResponseDtoAsync(result.Value, requesterId);
            if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
            return StatusCode(500, dtoResult.FullErrorMessage);
        }
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Gets a sticker by ID.
    /// </summary>
    [HttpGet("{stickerId}")]
    [Authorize]
    [ProducesResponseType<StickerResponseDto>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetSticker(string stickerId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await stickerService.GetStickerByIdAsync(stickerId);
        if (result.IsFailure)
        {
            if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
            return StatusCode(500, result.FullErrorMessage);
        }

        var sticker = result.Value;
        if (sticker.IsPrivate && sticker.AuthorId != requesterId)
        {
            return StatusCode(403, "비공개 스티커를 조회할 권한이 없습니다.");
        }

        var dtoResult = await stickerService.GenerateStickerResponseDtoAsync(sticker, requesterId);
        if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
        return StatusCode(500, dtoResult.FullErrorMessage);
    }

    /// <summary>
    /// Gets a list of stickers.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<List<StickerResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetStickers([FromQuery] int limit = 20, [FromQuery] string from = null)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        limit = Math.Clamp(limit, 1, 100);

        var result = await stickerService.GetStickersAsync(requesterId, from, limit);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtoResult = await stickerService.GenerateStickerResponseDtosAsync(result.Value, requesterId);
        if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
        return StatusCode(500, dtoResult.FullErrorMessage);
    }

    /// <summary>
    /// Gets a list of stickers created by a user.
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize]
    [ProducesResponseType<List<StickerResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetStickersByUserId(string userId, [FromQuery] int limit = 20, [FromQuery] string from = null)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        limit = Math.Clamp(limit, 1, 100);

        var result = await stickerService.GetStickersByUserIdAsync(userId, requesterId, from, limit);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtoResult = await stickerService.GenerateStickerResponseDtosAsync(result.Value, requesterId);
        if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
        return StatusCode(500, dtoResult.FullErrorMessage);
    }

    /// <summary>
    /// Searches stickers.
    /// </summary>
    [HttpGet("search")]
    [Authorize]
    [ProducesResponseType<List<StickerResponseDto>>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> SearchStickers([FromQuery] string query, [FromQuery] int limit = 20, [FromQuery] string from = null)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        limit = Math.Clamp(limit, 1, 100);

        var result = await stickerService.SearchStickersAsync(query, requesterId, from, limit);
        if (result.IsFailure)
        {
            if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
            return StatusCode(500, result.FullErrorMessage);
        }

        var dtoResult = await stickerService.GenerateStickerResponseDtosAsync(result.Value, requesterId);
        if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
        return StatusCode(500, dtoResult.FullErrorMessage);
    }

    /// <summary>
    /// Deletes a sticker.
    /// </summary>
    [HttpDelete("{stickerId}")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> DeleteSticker(string stickerId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await stickerService.DeleteStickerAsync(stickerId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Gets a list of sticker assets.
    /// </summary>
    [HttpGet("{stickerId}/assets")]
    [Authorize]
    [ProducesResponseType<List<StickerAssetResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetStickerAssets(string stickerId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await stickerService.GetStickerAssetsAsync(stickerId, requesterId);
        if (result.IsSuccess)
        {
            var dtos = result.Value.Select(a => new StickerAssetResponseDto
            {
                Id = a.Id,
                StickerId = a.StickerId,
                MediaId = a.MediaId
            }).ToList();
            return Ok(dtos);
        }
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Subscribes to a sticker.
    /// </summary>
    [HttpPost("{stickerId}/subscribe")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> SubscribeSticker(string stickerId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await stickerService.SubscribeStickerAsync(stickerId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Unsubscribes from a sticker.
    /// </summary>
    [HttpDelete("{stickerId}/subscribe")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> UnsubscribeSticker(string stickerId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await stickerService.UnsubscribeStickerAsync(stickerId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Gets a list of stickers I subscribed to.
    /// </summary>
    [HttpGet("subscribed")]
    [Authorize]
    [ProducesResponseType<List<StickerResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetSubscribedStickers([FromQuery] int limit = 20, [FromQuery] string from = null)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        limit = Math.Clamp(limit, 1, 100);

        var result = await stickerService.GetSubscribedStickersAsync(requesterId, from, limit);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtoResult = await stickerService.GenerateStickerResponseDtosAsync(result.Value, requesterId);
        if (dtoResult.IsSuccess) return Ok(dtoResult.Value);
        return StatusCode(500, dtoResult.FullErrorMessage);
    }

    /// <summary>
    /// Records sticker asset usage.
    /// </summary>
    [HttpPost("{stickerId}/assets/{assetId}/use")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> RecordStickerUsage(string stickerId, string assetId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        var result = await stickerService.RecordStickerUsageAsync(stickerId, assetId, requesterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Gets recently used sticker assets.
    /// </summary>
    [HttpGet("recent")]
    [Authorize]
    [ProducesResponseType<List<StickerAssetResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetRecentStickerAssets([FromQuery] int limit = 20)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요합니다.");

        limit = Math.Clamp(limit, 1, 50);

        var result = await stickerService.GetRecentStickerAssetsAsync(requesterId, limit);
        if (result.IsSuccess)
        {
            var dtos = result.Value.Select(a => new StickerAssetResponseDto
            {
                Id = a.Id,
                StickerId = a.StickerId,
                MediaId = a.MediaId
            }).ToList();
            return Ok(dtos);
        }
        return StatusCode(500, result.FullErrorMessage);
    }
}
