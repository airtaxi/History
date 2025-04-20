namespace History.ApiService.DataTypes;

public class ImageConvertResult
{
    public bool IsMp4 { get; set; }
    public byte[] Data { get; set; }
    public string MimeType => IsMp4 ? "video/mp4" : "image/webp";

    public ImageConvertResult(bool isMp4, byte[] data)
    {
        IsMp4 = isMp4;
        Data = data;
    }
}
