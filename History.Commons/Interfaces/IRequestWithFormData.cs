namespace History.Commons.Interfaces;

public interface IRequestWithFormData : IBaseRequest
{
    public Dictionary<string, string> FormData { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }
}
