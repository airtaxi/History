using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes.Contents;

namespace History.ApiService;

public static class Utils
{
    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
    }

    public static string GenerateThumbnailUrlFromContents(IEnumerable<BaseContent> contents)
    {
        string imageUrl = null;
        var mediaId = contents.OfType<MediaContent>().Select(x => x.ThumbnailMediaId).FirstOrDefault();
        if (mediaId != null) imageUrl = GenerateMediaUri(mediaId);

        return imageUrl;
    }
}
