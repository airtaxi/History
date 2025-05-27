using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace History.ApiService.Services;

public class RefreshTokenService(IMongoDatabase database) : IRefreshTokenService
{
    private readonly IMongoCollection<RefreshToken> _refreshTokenCollection = database.GetCollection<RefreshToken>("RefreshTokens");

    /// <inheritdoc />
    public async Task AddRefreshTokenAsync(string refreshToken)
    {
        // Parse the JWT token and get the userId and expiration date
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(refreshToken);

        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var expiresAt = jwtToken.ValidTo;

        var newRefreshToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
        };
        await _refreshTokenCollection.InsertOneAsync(newRefreshToken);

        // Delete expired refresh tokens
        var expiredFilter = Builders<RefreshToken>.Filter.Lt(rt => rt.ExpiresAt, DateTime.UtcNow);
        await _refreshTokenCollection.DeleteManyAsync(expiredFilter);
    }

    /// <inheritdoc />
    public void AddRefreshToken(string refreshToken)
    {
        // Parse the JWT token and get the userId and expiration date
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(refreshToken);

        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var expiresAt = jwtToken.ValidTo;

        var newRefreshToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
        };

        while (true)
        {
            newRefreshToken.Id = Guid.NewGuid().ToString("N");

            var existingToken = _refreshTokenCollection.Find(f => f.Id == newRefreshToken.Id).FirstOrDefault();
            if (existingToken == null) break;
        }

        _refreshTokenCollection.InsertOne(newRefreshToken);

        // Delete expired refresh tokens
        var expiredFilter = Builders<RefreshToken>.Filter.Lt(rt => rt.ExpiresAt, DateTime.UtcNow);
        _refreshTokenCollection.DeleteMany(expiredFilter);
    }

    /// <inheritdoc />
    public async Task RevokeRefreshTokenAsync(string refreshToken) => await _refreshTokenCollection.DeleteOneAsync(rt => rt.Token == refreshToken);

    /// <inheritdoc />
    public async Task<Result> ValidateRefreshTokenAsync(string refreshToken)
    {
        var existingToken = await _refreshTokenCollection.Find(rt => rt.Token == refreshToken).FirstOrDefaultAsync();
        if (existingToken == null || DateTime.UtcNow > existingToken.ExpiresAt) return Result.Failure(ErrorType.Unauthorized, "로그인 세션이 만료되었습니다. 다시 로그인 해주세요.");
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> HandleWithdrawAsync(string userId)
    {
        // Revoke all refresh tokens for the user
        var filter = Builders<RefreshToken>.Filter.Eq(rt => rt.UserId, userId);
        await _refreshTokenCollection.DeleteManyAsync(filter);

        return Result.Success();
    }
}
