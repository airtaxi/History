using History.Commons.Enums;

namespace History.Commons.DataTypes.Dto;

public class OAuthLoginRequestDto
{
    public string IdToken { get; set; }
    public SocialService Provider { get; set; }
}