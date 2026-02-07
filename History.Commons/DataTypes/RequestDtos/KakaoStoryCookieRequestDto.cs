namespace History.Commons.DataTypes.RequestDtos;

public class KakaoStoryCookieRequestDto
{
    public string LoginId { get; set; }
    public string EncryptedPassword { get; set; }
    public bool ForceRefresh { get; set; }
}
