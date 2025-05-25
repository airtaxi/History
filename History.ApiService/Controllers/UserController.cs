using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Requests;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService, IFriendshipService friendshipService, IRefreshTokenService refreshTokenService, INotificationService notificationService) : ControllerBase
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
            Nickname = payload.Name ?? GenerateDefaultUserName(),
            SocialService = request.Provider,
            Email = payload.Email
            // Rank will be set in the service based on the number of users
            // (Admin for the first user, User for others)
        };

        while (true)
        {
            newUser.Handle = Guid.NewGuid().ToString("N")[..8];
            var existingHandleUserResult = await userService.GetUserByHandleAsync(newUser.Handle, false);
            if (existingHandleUserResult.IsFailure) break;
        }

        await userService.CreateUserAsync(newUser);

        var accessToken = GenerateJwt(newUser, false);
        var refreshToken = GenerateJwt(newUser, true);
        return Ok(new OAuthLoginResponseDto(accessToken, refreshToken));
    }

    /// <summary>
    /// Login with OAuth
    /// </summary>
    /// <param name="request">The DTO containing the JWT token from OAuth provider</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] OAuthLoginRequestDto request)
    {
        var payload = await VerifyIdTokenAsync(request);
        if (payload == null) return Unauthorized("ID 토큰이 유효하지 않습니다.");

        var userResult = await userService.GetUserByIdAsync(payload.Subject);
        if (userResult.IsFailure) return NotFound("사용자가 존재하지 않습니다.");

        if (userResult.Value.Rank == Rank.Unauthorized) return StatusCode(403, "가입 승인 대기 중입니다.");

        var accessToken = GenerateJwt(userResult, false);
        var refreshToken = GenerateJwt(userResult, true);
        return Ok(new OAuthLoginResponseDto(accessToken, refreshToken));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
    {
        var validationResult = await refreshTokenService.ValidateRefreshTokenAsync(model.RefreshToken);
        if (!validationResult.IsSuccess) return Unauthorized(validationResult.ErrorMessage);

        // Revoke the old refresh token
        await refreshTokenService.RevokeRefreshTokenAsync(model.RefreshToken);

        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(model.RefreshToken);

        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("사용자 ID를 찾을 수 없습니다.");

        var userResult = await userService.GetUserByIdAsync(userId);
        if (userResult.IsFailure) return NotFound("사용자가 존재하지 않습니다.");

        var newAccessToken = GenerateJwt(userResult.Value, false);
        var newRefreshToken = GenerateJwt(userResult.Value, true);
        return Ok(new OAuthLoginResponseDto(newAccessToken, newRefreshToken));
    }


    /// <summary>
    /// Get user profile
    /// </summary>
    /// <param name="userId">The ID of user to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of user profile</returns>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var userResult = await userService.GetUserByIdAsync(userId);
        if (userResult == null) return null;

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId != null)
        {
            var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
            var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
            var requesterBlockerFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);

            if (requesterBlockedFriendIdsResult.Value.Contains(userId)) return Unauthorized("차단한 사용자의 피드를 볼 수 없습니다.");
            else if (requesterIgnoredFriendIdsResult.Value.Contains(userId)) return Unauthorized("무시한 사용자의 피드를 볼 수 없습니다.");
            else if (requesterBlockerFriendIdsResult.Value.Contains(userId)) return Unauthorized("이 사용자의 피드를 볼 수 없습니다.");
        }

        var result = await userService.GenerateUserResponseDtoAsync(userResult, requesterId);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("handle/{handle}")]
    public async Task<IActionResult> GetUserByHandle(string handle)
    {
        var userResult = await userService.GetUserByHandleAsync(handle, true);
        if (userResult.IsFailure) return NotFound(userResult.ErrorMessage);

        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId != null)
        {
            var requesterBlockedFriendIdsResult = await friendshipService.GetBlockedUserIdsAsync(requesterId);
            var requesterIgnoredFriendIdsResult = await friendshipService.GetIgnoredUserIdsAsync(requesterId);
            var requesterBlockerFriendIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);

            if (requesterBlockedFriendIdsResult.Value.Contains(userResult.Value.Id)) return Unauthorized("차단한 사용자의 피드를 볼 수 없습니다.");
            else if (requesterIgnoredFriendIdsResult.Value.Contains(userResult.Value.Id)) return Unauthorized("무시한 사용자의 피드를 볼 수 없습니다.");
            else if (requesterBlockerFriendIdsResult.Value.Contains(userResult.Value.Id)) return Unauthorized("이 사용자의 피드를 볼 수 없습니다.");
        }

        var result = await userService.GenerateUserResponseDtoAsync(userResult, requesterId);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("nickname-search/{query}")]
    public async Task<IActionResult> FindUsersByNickname(string query)
    {
        var usersResult = await userService.FindUsersByNicknameAsync(query, true, 15);
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await userService.GenerateUserResponseDtosAsync(usersResult.Value, requesterId);
        if (result.IsSuccess) return Ok(result.Value);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var result = await userService.GetUserByIdAsync(userId);
        if (result.IsFailure) return NotFound(result.ErrorMessage);

        var dtosResult = await userService.GenerateUserResponseDtoAsync(result, userId);
        if (dtosResult.IsFailure) return StatusCode(500, dtosResult.FullErrorMessage);

        return Ok(dtosResult.Value);
    }

    [HttpPost("approve/{userId}")]
    [Authorize]
    public async Task<IActionResult> ApproveUnauthorizedUser(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return Unauthorized("권한이 없습니다.");

        var result = await userService.ApproveUnauthorizedUserAsync(userId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("unapprove/{userId}")]
    [Authorize]
    public async Task<IActionResult> UnapproveUnauthorizedUser(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank < Rank.Moderator) return Unauthorized("권한이 없습니다.");

        var result = await userService.UnapproveUnauthorizedUserAsync(userId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPost("make-moderator/{userId}")]
    [Authorize]
    public async Task<IActionResult> MakeUserModerator(string userId)
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var requester = await userService.GetUserByIdAsync(requesterId);
        if (requester?.Value.Rank != Rank.Admin) return Unauthorized("권한이 없습니다.");

        var result = await userService.MakeUserModeratorAsync(userId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("unauthorized-users")]
    [Authorize]
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
    [Authorize]
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

    [HttpPut("allowSearch/{allowSearch}")]
    [Authorize]
    public async Task<IActionResult> UpdateAllowSearch(bool allowSearch)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Call the service to update the AllowSearch property
        var result = await userService.UpdateAllowSearchAsync(userId, allowSearch);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPut("friend-list-discovery-option/{discoveryOption}")]
    [Authorize]
    public async Task<IActionResult> UpdateFriendListDiscoveryOption(DiscoveryOption discoveryOption)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Call the service to update the AllowSearch property
        var result = await userService.UpdateFriendListDiscoveryOptionAsync(userId, discoveryOption);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpPut("handle")]
    [Authorize]
    public async Task<IActionResult> UpdateHandle([FromBody] UpdateUserHandleRequestDto request)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Call the service to update the Handle property
        var result = await userService.UpdateHandleAsync(userId, request.Handle);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Conflict) return Conflict(result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    /// <summary>
    /// Updates the profile media of a user
    /// </summary>
    /// <param name="file">The media file to upload</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPut("profile-media")]
    [Authorize]
    public async Task<IActionResult> UpdateProfileMedia(IFormFile file)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

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
    /// Deletes the profile media of a user
    /// </summary>
    /// <returns>An action result indicating success or failure</returns>
    [HttpDelete("profile-media")]
    [Authorize]
    public async Task<IActionResult> DeleteProfileMedia()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var deleteResult = await userService.UpdateProfileMediaAsync(userId, null);

        if (deleteResult.IsSuccess) return Ok();
        else if (deleteResult.Error == ErrorType.NotFound) return NotFound(deleteResult.ErrorMessage);
        else return StatusCode(500, deleteResult.FullErrorMessage);
    }

    /// <summary>
    /// Updates the background media of a user
    /// </summary>
    /// <param name="file">The media file to upload</param>
    /// <returns>An action result indicating success or failure</returns>
    [HttpPut("background-media")]
    [Authorize]
    public async Task<IActionResult> UpdateBackgroundMedia(IFormFile file)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

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
    /// Deletes the background media of a user
    /// </summary>
    /// <returns>An action result indicating success or failure</returns>
    [HttpDelete("background-media")]
    [Authorize]
    public async Task<IActionResult> DeleteBackgroundMedia()
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var deleteResult = await userService.UpdateBackgroundMediaAsync(userId, null);

        if (deleteResult.IsSuccess) return Ok();
        else if (deleteResult.Error == ErrorType.NotFound) return NotFound(deleteResult.ErrorMessage);
        else return StatusCode(500, deleteResult.FullErrorMessage);
    }

    [HttpPut("pinned-post/{pinnedPostId}")]
    public async Task<IActionResult> UpdatePinnedPost(string pinnedPostId)
    {
        // Get the user ID from the authenticated user claim
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        // Call the service to update the pinned post
        var result = await userService.UpdatePinnedPostAsync(userId, pinnedPostId);
        if (result.IsSuccess) return Ok();
        else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
        else if (result.Error == ErrorType.Forbidden) return StatusCode(StatusCodes.Status403Forbidden, result.ErrorMessage);
        else return StatusCode(500, result.FullErrorMessage);
    }

    [HttpGet("notifications")]
    [Authorize]
    public async Task<IActionResult> GetNotifications()
    {
        var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (requesterId == null) return Unauthorized("로그인이 필요한 서비스입니다.");

        var from = Request.Query["from"];
        var rawLimit = Request.Query["limit"];
        if (!int.TryParse(rawLimit, out var limit)) limit = 30;
        limit = Math.Clamp(limit, 1, 100);

        var notificationsResult = await notificationService.GetNotificationsAsync(requesterId, from, limit);
        if (notificationsResult.IsFailure) return StatusCode(500, notificationsResult.FullErrorMessage);

        var notificationUserIds = notificationsResult.Value.Select(x => x.UserId);
        var userDtosResult = await userService.GenerateUserResponseDtosAsync(notificationUserIds, requesterId);

        var dtos = new List<NotificationResponseDto>();
        foreach (var notification in notificationsResult.Value)
        {
            var userId = notification.UserId;
            var userDto = userDtosResult.Value.FirstOrDefault(x => x.UserId == userId);
            if (userDto == null) continue;

            var dto = new NotificationResponseDto(notification) { User = userDto };
            dtos.Add(dto);
        }

        return Ok(dtos);
    }

    [HttpPost("register-firebase-token")]
    [Authorize]
    public async Task<IActionResult> RegisterToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return BadRequest("로그인이 필요한 서비스입니다.");

        var token = Request.Query["token"];
        if (string.IsNullOrEmpty(token)) return BadRequest("토큰은 필수입니다.");

        var userResult = await userService.GetUserByIdAsync(userId);
        if (userResult.Error == ErrorType.NotFound) return NotFound("사용자를 찾을 수 없습니다.");
        else if (userResult.IsFailure) return StatusCode(500, userResult.FullErrorMessage);

        await notificationService.RegisterFirebaseTokenAsync(userId, token);

        return Ok();
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
    /// <remarks>When isRefreshToken is true, the token will be a refresh token and automatically added to the refresh token service</remarks>
    /// <param name="user">The user to generate the token for</param>
    /// <param name="isRefreshToken">Indicates whether the token is a refresh token</param>
    /// <returns>A JWT token string</returns>
    private string GenerateJwt(User user, bool isRefreshToken)
    {
        // Create the claims for the JWT token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, user.Rank.ToString()),
            new Claim("nickname", user.Nickname),
            new Claim("provider", user.SocialService.ToString()),
            new Claim("token_type", isRefreshToken ? "refresh" : "access"),
            new Claim("refresh_token", isRefreshToken ? "true" : "false"),
        };

        // Create the key and credentials for the JWT token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Constants.JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create the JWT token
        var token = new JwtSecurityToken(
            issuer: Constants.JwtIssuer,
            audience: Constants.JwtAudience,
            claims: claims,
            expires: isRefreshToken ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddDays(3),
            signingCredentials: creds
        );

        // Return the token as a string
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        if (isRefreshToken) refreshTokenService.AddRefreshToken(tokenString);
        return tokenString;
    }

    /// <summary>
    /// Generate a default username
    /// </summary>
    /// <returns>A default username starting with "사용자-" and followed by a random 8-character string</returns>
    private static string GenerateDefaultUserName() => $"사용자-{Guid.NewGuid().ToString("N")[..8]}";
}
