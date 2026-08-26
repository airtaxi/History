using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.Commons;

public partial class CommonShared
{
    public static ApiHandler ApiHandler { get; set; } = ApiHandler.Public;
    public static string UserId { get; set; }
    public static Rank MyRank { get; set; }
    public static List<UserResponseDto> Friends { get; set; }
    public static List<FriendData.Profile> KakaoFriends { get; set; }
    public static DiscoveryOption LastUsedPostDiscoveryOption { get; set; }
    public static string KakaoUserId { get; set; }
}
