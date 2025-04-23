using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<DiscoveryOption>))]
public enum DiscoveryOption
{
    OnlyMe,
    SelectedUsers,
    UnselectedUsers,
    Friends,
    FriendsOfFriends,
    Everyone,
}

public static class DiscoveryOptionExtensions
{
    public static string ToDisplayString(this DiscoveryOption option)
    {
        return option switch
        {
            DiscoveryOption.OnlyMe => "나만 보기",
            DiscoveryOption.SelectedUsers => "특정 친구 공개",
            DiscoveryOption.UnselectedUsers => "특정 친구 비공개",
            DiscoveryOption.Friends => "친구 공개",
            DiscoveryOption.FriendsOfFriends => "친구의 친구 공개",
            DiscoveryOption.Everyone => "전체 공개",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
        };
    }

    public static DiscoveryOption FromDisplayString(string displayString)
    {
        return displayString switch
        {
            "나만 보기" => DiscoveryOption.OnlyMe,
            "특정 친구 공개" => DiscoveryOption.SelectedUsers,
            "특정 친구 비공개" => DiscoveryOption.UnselectedUsers,
            "친구 공개" => DiscoveryOption.Friends,
            "친구의 친구 공개" => DiscoveryOption.FriendsOfFriends,
            "전체 공개" => DiscoveryOption.Everyone,
            _ => throw new ArgumentOutOfRangeException(nameof(displayString), displayString, null)
        };
    }
}