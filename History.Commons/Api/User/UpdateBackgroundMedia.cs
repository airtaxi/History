using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class UpdateBackgroundMedia : IAuthRequiredRequest, IRequestWithFile
{
    public string Path => "/api/user/background-media";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }

    public UpdateBackgroundMedia(string fileName, byte[] fileContent)
    {
        FileName = fileName;
        FileContent = fileContent;
    }
}
