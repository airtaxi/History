namespace History.Uno;

public static class Shared
{
    public static ApiHandler ApiHandler { get; set; } = ApiHandler.Public;
    public static string UserId { get; set; }
    public static Rank MyRank { get; set; }
    public static List<UserResponseDto> Friends { get; set; }
    public static DiscoveryOption LastUsedPostDiscoveryOption { get; set; }
}