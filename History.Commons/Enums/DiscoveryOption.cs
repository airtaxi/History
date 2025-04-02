using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<DiscoveryOption>))]
public enum DiscoveryOption
{
    OnlyMe,
    SelectedUsers,
    Friends,
    FriendsOfFriends,
    Everyone,
}
