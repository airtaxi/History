using History.ApiService.Services;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController(IReportService reportService, IUserService userService) : ControllerBase
{
    [HttpGet("records")]
    [Authorize]
    public async Task<IActionResult> GetReportRecords([FromQuery] string from = null, [FromQuery] int limit = 10)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        else
        {
            var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
            if (moderatorResult.IsFailure) return StatusCode(500, moderatorResult.FullErrorMessage);
            if (moderatorResult.Value.Rank < Rank.Moderator) return StatusCode(403, "괸리자만 신고 내역을 조회할 수 있습니다.");
        }

        var results = await reportService.GetReportRecordsAsync(from, limit);
        var dtosResult = await reportService.GenerateReportRecordResponseDtosAsync(results.Value);
        return Ok(dtosResult.Value);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReportRecord([FromBody] CreateReportRecordRequestDto request)
    {
        var reporterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(reporterId)) return Unauthorized("로그인이 필요한 서비스입니다.");

        var result = await reportService.CreateReportRecordAsync(request.Type, request.Target, request.AssociatedId, reporterId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("process/{recordId}")]
    [Authorize]
    public async Task<IActionResult> ProcessReportRecord(string recordId, [FromQuery] string reason)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        if (string.IsNullOrEmpty(reason)) return BadRequest("처리 사유를 입력해주세요.");

        var result = await reportService.ProcessReportAsync(recordId, moderatorId, reason);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else if (result.Error == ErrorType.Unauthorized) return Unauthorized(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(403, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpDelete("{recordId}")]
    [Authorize]
    public async Task<IActionResult> DeleteReportRecord(string recordId)
    {
        var moderatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(moderatorId)) return Unauthorized("로그인이 필요한 서비스입니다.");
        else
        {
            var moderatorResult = await userService.GetUserByIdAsync(moderatorId);
            if (moderatorResult.IsFailure) return StatusCode(500, moderatorResult.FullErrorMessage);
            if (moderatorResult.Value.Rank < Rank.Moderator) return StatusCode(403, "괸리자만 신고 내역을 삭제할 수 있습니다.");
        }

        var result = await reportService.DeleteReportRecordByIdAsync(recordId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.BadRequest) return BadRequest(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

}
