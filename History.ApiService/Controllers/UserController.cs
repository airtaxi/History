using Google.Apis.Auth;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService, IFriendshipService friendshipService) : ControllerBase
{
    /// <summary>
    /// Register with OAuth
    /// </summary>
    /// <param name="request">The JWT token from OAuth provider</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] OAuthLoginRequestDto request)
    {
        var payload = await VerifyIdTokenAsync(request);
        if (payload == null) return Unauthorized("ID 토큰이 유효하지 않습니다.");

        var existingUserResult = await userService.GetUserByIdAsync(payload.Subject);
        if (existingUserResult.IsSuccess) return Conflict("이미 등록된 사용자입니다.");

        var newUser = new User
        {
            Id = payload.Subject,
            Nickname = payload.Name ?? GenerateDeafultUserName(),
            SocialService = request.Provider,
            Handle = Guid.NewGuid().ToString("N")[..8]
            // Rank will be set in the service based on the number of users
            // (Admin for the first user, User for others)
        };

        await userService.CreateUserAsync(newUser);

        var token = GenerateJwt(newUser);
        return Ok(new OAuthLoginResponseDto(token));
    }

    /// <summary>
    /// Login with OAuth
    /// </summary>
    /// <param name="request">The JWT token from OAuth provider</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] OAuthLoginRequestDto request)
    {
        var payload = await VerifyIdTokenAsync(request);
        if (payload == null) return Unauthorized("ID 토큰이 유효하지 않습니다.");

        var userResult = await userService.GetUserByIdAsync(payload.Subject);
        if (userResult == null) return NotFound("사용자가 존재하지 않습니다.");

        if (userResult.Value.Rank == Rank.Unauthorized) return Unauthorized("가입 승인 대기 중입니다.");

        var token = GenerateJwt(userResult);
        return Ok(new OAuthLoginResponseDto(token));
    }

    /// <summary>
    /// Get user profile
    /// </summary>
    /// <param name="userId">The ID of user to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of user profile</returns>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var userResult = await userService.GetUserByIdAsync(userId);
        if (userResult == null) return null;

        if (requesterId != null)
        {
            var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
            var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
            var requesterBlockerFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);

            if (requesterBlockedFriendIdsResult.Value.Contains(userId) || requesterIgnoredFriendIdsResult.Value.Contains(userId)) return Unauthorized("차단 또는 무시한 사용자의 피드를 볼 수 없습니다.");
            else if (requesterBlockerFriendIdsResult.Value.Contains(userId)) return Unauthorized("이 사용자의 피드를 볼 수 없습니다.");
        }

        var dto = await userService.GenerateUserResponseDtoAsync(userResult, requesterId);
        if (dto.IsSuccess) return Ok(dto.Value);
        if (dto.Error == ErrorType.NotFound) return NotFound(dto.ErrorMessage);
        else return StatusCode(500, dto.FullErrorMessage);
    }

    [HttpPost("approve")]
    public async Task<IActionResult> ApproveUnauthorizedUser([FromBody] ApproveUnauthorizedUserRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return Unauthorized("권한이 없습니다.");

        var result = await userService.ApproveUnauthorizedUserAsync(request.UserId);
        if (result.IsSuccess) return Ok();

        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("unapprove")]
    public async Task<IActionResult> UnapproveUnauthorizedUser([FromBody] UnapproveUnauthorizedUserRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return Unauthorized("권한이 없습니다.");

        var result = await userService.UnapproveUnauthorizedUserAsync(request.UserId);
        if (result.IsSuccess) return Ok();

        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("make-moderator")]
    public async Task<IActionResult> MakeUserModerator([FromBody] MakeUserModeratorRequestDto request)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank != Rank.Admin) return Unauthorized("권한이 없습니다.");

        var result = await userService.MakeUserModeratorAsync(request.UserId);
        if (result.IsSuccess) return Ok();

        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("unauthorized-users")]
    public async Task<IActionResult> GetUnauthorizedUsers()
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return Unauthorized("권한이 없습니다.");

        var result = await userService.GetUnauthorizedUsersAsync();
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(result.Value);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    [HttpGet("moderators")]
    public async Task<IActionResult> GetModerators()
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank != Rank.Admin) return Unauthorized("권한이 없습니다.");

        var result = await userService.GetModeratorsAsync();
        if (result.IsFailure) return StatusCode(500, result.FullErrorMessage);

        var dtosResult = await userService.GenerateUserResponseDtosAsync(result.Value);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    /// <summary>
    /// Updates the description of a user
    /// </summary>
    /// <param name="request">Request containing the description to update</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPut("description")]
    [Authorize]
    public async Task<IActionResult> UpdateDescription([FromBody] UpdateUserDescriptionRequestDto request)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Call the service to update the description
        var result = await userService.UpdateDescriptionAsync(userId, request.Description);

        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Updates the birthday of a user
    /// </summary>
    /// <param name="request">Request containing the birthday to update</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPut("birthday")]
    [Authorize]
    public async Task<IActionResult> UpdateBirthday([FromBody] UpdateUserBirthdayRequestDto request)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Call the service to update the birthday
        var result = await userService.UpdateBirthdayAsync(userId, request.Birthday);

        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Updates the nickname of a user
    /// </summary>
    /// <param name="request">Request containing the nickname to update</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPut("nickname")]
    [Authorize]
    public async Task<IActionResult> UpdateNickname([FromBody] UpdateUserNicknameRequestDto request)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Call the service to update the nickname
        var result = await userService.UpdateNicknameAsync(userId, request.Nickname);

        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Updates the profile image of a user
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPut("profile-media")]
    [Authorize]
    public async Task<IActionResult> UpdateProfileMedia(IFormFile file)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Handle the case of removing the profile image
        if (file == null)
        {
            var deleteResult = await userService.UpdateProfileMediaAsync(userId, null);

            if (deleteResult.IsSuccess) return Ok();
            else if (deleteResult.Error == ErrorType.NotFound) return NotFound(deleteResult.ErrorMessage);
            else return StatusCode(500, deleteResult.FullErrorMessage);
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower())) return BadRequest("올바르지 않은 파일 형식입니다. 이미지 파일을 업로드해주세요.");

        // Validate file size (e.g., 20MB max)
        var maxSize = 20 * 1024 * 1024; // 20MB
        if (file.Length > maxSize) return BadRequest("파일 크기가 너무 큽니다. 20MB 이하의 이미지 파일을 업로드해주세요.");

        // Read the file into a byte array
        byte[] imageData;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            imageData = memoryStream.ToArray();
        }

        // Call the service to update the profile media
        var result = await userService.UpdateProfileMediaAsync(userId, imageData);

        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Updates the background image of a user
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPut("background-media")]
    [Authorize]
    public async Task<IActionResult> UpdateBackgroundMedia(IFormFile file)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Handle the case of removing the background image
        if (file == null)
        {
            var deleteResult = await userService.UpdateBackgroundMediaAsync(userId, null);

            if (deleteResult.IsSuccess) return Ok();
            else if (deleteResult.Error == ErrorType.NotFound) return NotFound(deleteResult.ErrorMessage);
            else return StatusCode(500, deleteResult.FullErrorMessage);
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower())) return BadRequest("올바르지 않은 파일 형식입니다. 이미지 파일을 업로드해주세요.");

        // Validate file size (e.g., 20MB max)
        var maxSize = 20 * 1024 * 1024; // 20MB
        if (file.Length > maxSize) return BadRequest("파일 크기가 너무 큽니다. 20MB 이하의 이미지 파일을 업로드해주세요.");

        // Read the file into a byte array
        byte[] imageData;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            imageData = memoryStream.ToArray();
        }

        // Call the service to update the background media
        var result = await userService.UpdateBackgroundMediaAsync(userId, imageData);

        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Verify the ID token from the OAuth provider
    /// </summary>
    /// <param name="request">The request containing the ID token</param>
    /// <returns>A task that represents the asynchronous operation. with the payload of the ID token</returns>
    private static async Task<GoogleJsonWebSignature.Payload> VerifyIdTokenAsync(OAuthLoginRequestDto request)
    {
        // Verify the ID token based on the provider
        if (request.Provider == SocialService.Google)
        {
            // Verify the Google ID token
            try { return await GoogleJsonWebSignature.ValidateAsync(request.IdToken); }
            catch { return null; }
        }

        // TODO: Apple ID Token needs to be verified here
        return null;
    }

    /// <summary>
    /// Generate a JWT token for the user
    /// </summary>
    /// <param name="user">The user to generate the token for</param>
    /// <returns>A JWT token</returns>
    private static string GenerateJwt(User user)
    {
        // Create the claims for the JWT token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, user.Rank.ToString()),
            new Claim("nickname", user.Nickname),
            new Claim("provider", user.SocialService.ToString())
        };

        // Create the key and credentials for the JWT token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create the JWT token
        var token = new JwtSecurityToken(
            issuer: Constants.JwtIssuer,
            audience: Constants.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddYears(7),
            signingCredentials: creds
        );

        // Return the token as a string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate a default username
    /// </summary>
    /// <returns>A default username starting with "사용자-" and followed by a random 8-character string</returns>
    private static string GenerateDeafultUserName() => $"사용자-{Guid.NewGuid().ToString("N")[..8]}";
}
