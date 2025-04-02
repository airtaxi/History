using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<Rank>))]
public enum Rank
{
    User,
    Manager,
    Admin
}
