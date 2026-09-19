using System.Text.Json.Serialization.Metadata;

namespace History.Commons.Interfaces;

public interface IRequestWithBody : IBaseRequest
{
    public object Body { get; }
    public JsonTypeInfo BodyTypeInfo { get; }
}
