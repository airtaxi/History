using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<Rank>))]
public enum Rank
{
    Unauthorized,
    User,
    Moderator,
    Admin
}
