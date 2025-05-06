using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class UpdateProfileMedia : IAuthRequiredRequest, IRequestWithFile
{
    public string Path => "/api/user/profile-media";
    public Method Method => Method.Put;
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }

    public UpdateProfileMedia(string fileName, byte[] fileContent)
    {
        FileName = fileName;
        FileContent = fileContent;
    }
}
