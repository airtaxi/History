namespace History.Commons.DataTypes.RequestDtos;

// Local replacement for the Google OAuth refresh-token request type. The old request body was
// that Google type serialized by the previous transport's System.Text.Json web serializer,
// which ignored the Google [RequestParameter] attributes and wrote the camelCase public
// properties including nulls: scope, grantType, clientId, clientSecret, refreshToken.
// GrantType defaults to "refresh_token" the same way the Google constructor set it.
public class RefreshTokenRequestDto
{
    public string Scope { get; set; }
    public string GrantType { get; set; } = "refresh_token";
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RefreshToken { get; set; }
}
