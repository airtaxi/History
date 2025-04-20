namespace History.Commons.Interfaces;

public interface IRequestWithFiles : IBaseRequest
{
    public Dictionary<string, byte[]> Files { get; set; }
}
