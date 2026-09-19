using System.Text.Json;
using System.Text.Json.Serialization;

namespace History.Commons.KakaoStory;

// Some Kakao payloads put numbers, booleans, or nested objects where the DTO declares a string,
// and Json.NET coerced those values during deserialization. Reading the raw JSON for anything
// that is not a string keeps those legacy payloads loadable through the STJ code path.
internal sealed class KakaoStoryLenientStringJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        JsonTokenType.String => reader.GetString(),
        JsonTokenType.Null => null,
        JsonTokenType.True => "true",
        JsonTokenType.False => "false",
        JsonTokenType.Number or JsonTokenType.StartObject or JsonTokenType.StartArray => ReadRawText(ref reader),
        _ => throw new JsonException($"Cannot read {reader.TokenType} as a string."),
    };

    private static string ReadRawText(ref Utf8JsonReader reader)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}
