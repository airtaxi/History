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

    // Last selected History/Kakao Story pill mode, persisted across app restarts.
    public static bool LastUsedKakaoStoryMode
    {
        get => _lastUsedKakaoStoryMode ??= Configuration.GetValue<bool?>("LastUsedKakaoStoryMode") ?? false;
        set
        {
            _lastUsedKakaoStoryMode = value;
            Configuration.SetValue("LastUsedKakaoStoryMode", value);
        }
    }
    private static bool? _lastUsedKakaoStoryMode;
}
