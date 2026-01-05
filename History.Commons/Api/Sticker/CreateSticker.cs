using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

public class CreateSticker : IBaseRequest<StickerResponseDto>, IAuthRequiredRequest, IRequestWithFormData
{
    public string Path => "/api/sticker";
    public Method Method => Method.Post;

    public Dictionary<string, string> FormData { get; set; } = [];
    public Dictionary<string, byte[]> Files { get; set; } = [];

    public CreateSticker(string name, string category, string description, bool isPrivate, byte[] iconFile, string iconFileName, Dictionary<string, byte[]> assetFiles)
    {
        FormData["name"] = name;
        FormData["category"] = category;
        FormData["description"] = description ?? "";
        FormData["isPrivate"] = isPrivate.ToString().ToLower();

        // Icon file with specific name
        Files[$"iconFile|{iconFileName}"] = iconFile;

        // Asset files
        foreach (var (fileName, content) in assetFiles)
        {
            Files[$"assetFiles|{fileName}"] = content;
        }
    }
}
