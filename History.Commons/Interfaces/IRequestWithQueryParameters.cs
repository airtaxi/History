namespace History.Commons.Interfaces;

public interface IRequestWithQueryParameters : IBaseRequest
{
    public Dictionary<string, string> QueryParameters { get; }
}
