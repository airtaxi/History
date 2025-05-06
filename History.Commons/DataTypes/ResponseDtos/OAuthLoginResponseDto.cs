namespace History.Commons.DataTypes.ResponseDtos;

public class OAuthLoginResponseDto()
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public OAuthLoginResponseDto(string accessToken, string refreshToken) : this() 
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }
}
