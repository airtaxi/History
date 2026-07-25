namespace History.ApiService.DataTypes;

public class MediaConvertResult
{
    public bool IsVideo { get; set; }
    public bool IsAnimated { get; set; }
    public byte[] Data { get; set; }
    public string MimeType => IsVideo ? "video/mp4" : "image/webp";

    public MediaConvertResult(bool isVideo, byte[] data, bool isAnimated = false)
    {
        IsVideo = isVideo;
        Data = data;
        IsAnimated = isAnimated;
    }
}
