using DotNet.RateLimiter.ActionFilters;
using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.AspNetCore.Mvc;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(Limit = 3, PeriodInSec = 1)]
public class KakaoStoryController(IKakaoStoryCookieService kakaoStoryCookieService) : ControllerBase
{
    [HttpPost("cookie")]
    [ProducesResponseType<KakaoStoryCookieResponseDto>(200)]
    [ProducesResponseType<string>(400)]
    [ProducesResponseType<string>(401)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetCookie([FromBody] KakaoStoryCookieRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null) return BadRequest("요청 데이터가 없습니다.");
        if (string.IsNullOrWhiteSpace(request.LoginId)) return BadRequest("LoginId가 필요합니다.");
        if (string.IsNullOrWhiteSpace(request.EncryptedPassword)) return BadRequest("EncryptedPassword가 필요합니다.");

        var (key, iv) = GetAesConfig();
        if (key == null || key.Length == 0) return StatusCode(500, "AES 키가 설정되지 않았습니다.");

        string password;
        try
        {
            password = AesCryptoHelper.DecryptBase64(request.EncryptedPassword, key, iv);
        }
        catch (Exception ex)
        {
            return BadRequest($"암호 해독 실패: {ex.Message}");
        }

        var (cookie, expiresAt, fromCache) = await kakaoStoryCookieService.GetCookieAsync(request.LoginId, password, request.ForceRefresh, cancellationToken);
        return Ok(new KakaoStoryCookieResponseDto(cookie, expiresAt, fromCache));
    }

    private static (byte[] Key, byte[] Iv) GetAesConfig()
    {
        var keyBase64 = CommonsConstants.KakaoStoryAesKeyBase64;
        var ivBase64 = CommonsConstants.KakaoStoryAesIvBase64;

        if (string.IsNullOrWhiteSpace(keyBase64)) return (null, null);

        var key = Convert.FromBase64String(keyBase64);
        var iv = string.IsNullOrWhiteSpace(ivBase64) ? null : Convert.FromBase64String(ivBase64);
        return (key, iv);
    }
}
