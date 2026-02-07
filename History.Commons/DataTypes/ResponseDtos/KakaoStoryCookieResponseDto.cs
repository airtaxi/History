namespace History.Commons.DataTypes.ResponseDtos;

public class KakaoStoryCookieResponseDto()
{
    public string Cookie { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool FromCache { get; set; }

    public KakaoStoryCookieResponseDto(string cookie, DateTimeOffset expiresAt, bool fromCache) : this()
    {
        Cookie = cookie;
        ExpiresAt = expiresAt;
        FromCache = fromCache;
    }
}
