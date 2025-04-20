namespace History.Commons.Interfaces;

public interface IRequestWithBody : IBaseRequest
{
    public object Body { get; }
}
