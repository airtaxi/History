using History.Commons;

namespace History.ApiService.Services.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>
    /// Adds a new refresh token to the system.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Adds a new refresh token to the system.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    public void AddRefreshToken(string refreshToken);

    /// <summary>
    /// Revokes the specified refresh token, invalidating it for future use.
    /// </summary>
    /// <remarks>Once a refresh token is revoked, it can no longer be used to obtain new access tokens. Ensure
    /// that the provided token is valid and associated with the current user or session.</remarks>
    /// <param name="refreshToken">The refresh token to be revoked. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RevokeRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes the specified refresh token when it belongs to the given user, invalidating it for future use.
    /// </summary>
    /// <param name="refreshToken">The refresh token to be revoked. Cannot be null or empty.</param>
    /// <param name="userId">The ID of the user who owns the refresh token.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the revocation.</returns>
    public Task<Result> RevokeRefreshTokenForUserAsync(string refreshToken, string userId);

    /// <summary>
    /// Validates the specified refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the validation.</returns>
    public Task<Result> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Handles the withdrawal of a user, revoking their refresh token and any associated sessions.
    /// </summary>
    /// <param name="userId">The ID of the user whose refresh token is to be withdrawn.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the withdrawal.</returns>
    public Task<Result> HandleWithdrawAsync(string userId);
}
