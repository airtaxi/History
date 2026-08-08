using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient;

public static class Shared
{
    public static ApiHandler ApiHandler { get; set; } = ApiHandler.Public;
    public static string UserId { get; set; }
    public static Rank MyRank { get; set; }
    public static List<UserResponseDto> Friends { get; set; }
    public static List<FriendData.Profile> KakaoFriends { get; set; }
    public static DiscoveryOption LastUsedPostDiscoveryOption { get; set; }
    public static string KakaoUserId { get; set; }
}
