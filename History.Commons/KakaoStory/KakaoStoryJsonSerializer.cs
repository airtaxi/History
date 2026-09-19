using System.Text.Json.Serialization.Metadata;

namespace History.Commons.KakaoStory;

// Resolves the source-generated metadata for the Kakao Story payloads. Every (de)serialization
// call site goes through these JsonTypeInfo overloads instead of the options-based overloads,
// because those still carry RequiresUnreferencedCode/RequiresDynamicCode and would emit
// IL2026/IL3050 warnings in the trimmed/AOT build.
internal static class KakaoStoryJsonSerializer
{
    internal static JsonTypeInfo<T> TypeInfo<T>() => (JsonTypeInfo<T>)KakaoStoryJsonSerializerContext.Default.GetTypeInfo(typeof(T));

    internal static JsonTypeInfo<T> IgnoreNullTypeInfo<T>() => (JsonTypeInfo<T>)KakaoStoryIgnoreNullJsonSerializerContext.Default.GetTypeInfo(typeof(T));
}
