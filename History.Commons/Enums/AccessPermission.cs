using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AccessPermission>))]
public enum AccessPermission
{
    OnlyMe,
    Friends,
    FriendsOfFriends,
    Everyone,
}

public static class AccessPermissionExtensions
{
    public static string ToDisplayString(this AccessPermission option)
    {
        return option switch
        {
            AccessPermission.OnlyMe => "나만",
            AccessPermission.Friends => "친구만",
            AccessPermission.FriendsOfFriends => "친구의 친구까지",
            AccessPermission.Everyone => "모든 사람",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
        };
    }

    public static DiscoveryOption ToDiscoveryOption(this AccessPermission permission)
    {
        return permission switch
        {
            AccessPermission.OnlyMe => DiscoveryOption.OnlyMe,
            AccessPermission.Friends => DiscoveryOption.Friends,
            AccessPermission.FriendsOfFriends => DiscoveryOption.FriendsOfFriends,
            AccessPermission.Everyone => DiscoveryOption.Everyone,
            _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, null)
        };
    }

    public static AccessPermission FromDisplayString(string displayString)
    {
        return displayString switch
        {
            "나만" => AccessPermission.OnlyMe,
            "친구만" => AccessPermission.Friends,
            "친구의 친구까지" => AccessPermission.FriendsOfFriends,
            "모든 사람" => AccessPermission.Everyone,
            _ => throw new ArgumentOutOfRangeException(nameof(displayString), displayString, null)
        };
    }
}