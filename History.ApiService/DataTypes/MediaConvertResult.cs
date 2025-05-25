namespace History.ApiService.DataTypes;

public class MediaConvertResult
{
    public bool IsVideo { get; set; }
    public byte[] Data { get; set; }
    public string MimeType => IsVideo ? "video/mp4" : "image/webp";

    public MediaConvertResult(bool isMp4, byte[] data)
    {
        IsVideo = isMp4;
        Data = data;
    }
}
