using System.Security.Claims;
using System.Text.Json;
using DotNet.RateLimiter.ActionFilters;
using History.ApiService.DataTypes;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(Limit = 6, PeriodInSec = 1)]
public class MessageController(IMessageService messageService) : ControllerBase
{
    [HttpGet("{messageId}")]
    [Authorize]
    [ProducesResponseType<MessageResponseDto>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetMessage(string messageId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("로그인이 필요한 서비스입니다.");

        var result = await messageService.GetMessageByIdAsync(messageId);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtoResult = await messageService.GenerateMessageResponseDtoAsync(result.Value, userId);
        if (dtoResult.IsFailure) return StatusCode(500, dtoResult.FullErrorMessage);
        return Ok(dtoResult.Value);
    }

    [HttpGet("received")]
    [Authorize]
    [ProducesResponseType<List<MessageResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetReceivedMessages([FromQuery] string from = null, [FromQuery] int limit = 50)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("로그인이 필요한 서비스입니다.");

        var result = await messageService.GetReceivedMessagesAsync(userId, from, limit);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtosResult = await messageService.GenerateMessageResponseDtosAsync(result.Value, userId);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    [HttpGet("sent")]
    [Authorize]
    [ProducesResponseType<List<MessageResponseDto>>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetSentMessages([FromQuery] string from = null, [FromQuery] int limit = 50)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("로그인이 필요한 서비스입니다.");

        var result = await messageService.GetSentMessagesAsync(userId, from, limit);
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtosResult = await messageService.GenerateMessageResponseDtosAsync(result.Value, userId);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    [HttpPost("send")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> SendMessage([FromForm] DataWithFilesForm request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("로그인이 필요한 서비스입니다.");

        var data = JsonSerializer.Deserialize<SendMessageRequestDto>(request.JsonData);
        var result = await messageService.SendMessageAsync(userId, data, request.Files);
        
        if (result.IsSuccess) return Ok("메시지가 전송되었습니다.");
        
        if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        
        return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("{messageId}/read")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> MarkMessageAsRead(string messageId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("로그인이 필요한 서비스입니다.");

        var result = await messageService.MarkMessageAsReadAsync(messageId, userId);
        
        if (result.IsSuccess) return Ok("메시지가 읽음 처리되었습니다.");
        
        if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        
        return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("check-permission")]
    [Authorize]
    [ProducesResponseType<string>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(403)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(429)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> CheckMessagePermission([FromQuery] string receiverId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized("로그인이 필요한 서비스입니다.");

        if (string.IsNullOrEmpty(receiverId)) return BadRequest("수신자 ID가 필요합니다.");

        var result = await messageService.CheckMessagePermissionAsync(userId, receiverId);
        
        if (result.IsSuccess) return Ok("메시지 전송이 가능합니다.");
        
        if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        
        return StatusCode(500, result.FullErrorMessage);
    }
}