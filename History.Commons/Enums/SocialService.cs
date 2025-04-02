using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<SocialService>))]
public enum SocialService
{
    Google,
    Apple
}
