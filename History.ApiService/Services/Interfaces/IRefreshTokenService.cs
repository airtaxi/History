using History.Commons;

namespace History.ApiService.Services.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>
    /// Adds a new refresh token to the system.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Adds a new refresh token to the system.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add.</param>
    void AddRefreshToken(string refreshToken);

    /// <summary>
    /// Validates the specified refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the validation.</returns>
    Task<Result> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes the specified refresh token, invalidating it for future use.
    /// </summary>
    /// <remarks>Once a refresh token is revoked, it can no longer be used to obtain new access tokens. Ensure
    /// that the provided token is valid and associated with the current user or session.</remarks>
    /// <param name="refreshToken">The refresh token to be revoked. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RevokeRefreshTokenAsync(string refreshToken);
}
