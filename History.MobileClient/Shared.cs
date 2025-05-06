using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.MobileClient;

public static class Shared
{
    public static ApiHandler ApiHandler { get; set; } = ApiHandler.Public;
    public static string UserId { get; set; }
    public static List<UserResponseDto> Friends { get; set; }
    public static DiscoveryOption LastUsedPostDiscoveryOption { get; set; }
}
