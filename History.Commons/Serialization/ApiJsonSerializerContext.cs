using System.Text.Json;
using System.Text.Json.Serialization;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;

namespace History.Commons.Serialization;

// Source-generated metadata for the multipart "JsonData" form fields. The old transport
// serialized these with JsonSerializer.Serialize(body), which used the default options:
// PascalCase property names and null members included. Case-insensitive reads are enabled
// so the payload stays forgiving if it is ever deserialized.
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(WritePostRequestDto))]
[JsonSerializable(typeof(ModifyPostRequestDto))]
[JsonSerializable(typeof(SendMessageRequestDto))]
[JsonSerializable(typeof(List<BaseContent>))]
public partial class ApiJsonSerializerContext : JsonSerializerContext;
