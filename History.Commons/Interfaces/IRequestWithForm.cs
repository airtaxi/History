using System.Text.Json.Serialization.Metadata;

namespace History.Commons.Interfaces;

public interface IRequestWithForm : IBaseRequest
{
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo { get; }
}
