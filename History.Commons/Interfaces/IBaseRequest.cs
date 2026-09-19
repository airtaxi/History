using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Interfaces;


public interface IBaseRequest
{
    public string Path { get; }
    public HttpRequestMethod Method { get; }
}

public interface IBaseRequest<T> : IBaseRequest
{
    // Resolves the response metadata through the web-default context, which matches the
    // options the previous transport's JSON serializer deserialized responses with.
    public JsonTypeInfo<T> ResponseTypeInfo => (JsonTypeInfo<T>)ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(T));
}