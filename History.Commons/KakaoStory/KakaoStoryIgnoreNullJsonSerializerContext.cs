using System.Text.Json.Serialization;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.Commons.KakaoStory;

// Source-generated metadata for the Kakao Story payloads that must omit null members when
// written (the legacy Json.NET NullValueHandling.Ignore behavior): the WritePost decorators
// and the GetStringFromQuoteData fragments. Only the roots those call sites serialize are
// registered here; everything else keeps using KakaoStoryJsonSerializerContext.
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, IncludeFields = true, PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<QuoteData>))]
[JsonSerializable(typeof(QuoteData))]
internal partial class KakaoStoryIgnoreNullJsonSerializerContext : JsonSerializerContext;
