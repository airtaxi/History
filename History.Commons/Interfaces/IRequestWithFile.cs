namespace History.Commons.Interfaces;

public interface IRequestWithFile : IBaseRequest
{
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }
}
