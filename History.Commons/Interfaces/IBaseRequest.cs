using RestSharp;

namespace History.Commons.Interfaces;


public interface IBaseRequest
{
    public string Path { get; }
    public Method Method { get; }
}

public interface IBaseRequest<T> : IBaseRequest;