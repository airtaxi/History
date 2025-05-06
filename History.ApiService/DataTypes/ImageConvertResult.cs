namespace History.ApiService.DataTypes;

public class ImageConvertResult
{
    public bool IsVideo { get; set; }
    public byte[] Data { get; set; }
    public string MimeType => IsVideo ? "video/mp4" : "image/webp";

    public ImageConvertResult(bool isMp4, byte[] data)
    {
        IsVideo = isMp4;
        Data = data;
    }
}
