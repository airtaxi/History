using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class UpdateBackgroundMedia : IAuthRequiredRequest, IRequestWithFile
{
    public string Path => "/api/user/background-media";
    public Method Method => Method.Put;
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }

    public UpdateBackgroundMedia(string fileName, byte[] fileContent)
    {
        FileName = fileName;
        FileContent = fileContent;
    }
}
