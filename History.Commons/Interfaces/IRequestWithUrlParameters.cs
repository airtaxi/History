namespace History.Commons.Interfaces;

public interface IRequestWithUrlParameters : IBaseRequest
{
    public Dictionary<string, string> UrlParameters { get; }
}
