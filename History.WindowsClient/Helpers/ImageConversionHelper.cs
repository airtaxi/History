using ImageMagick;

namespace History.WindowsClient.Helpers;

// Image format conversion for upload targets that accept only specific formats
// (Kakao Story does not accept webp, heic, heif, or avif). Conversions are
// best-effort: failures return null and the caller decides how to skip the image.
public static partial class ImageConversionHelper
{
    public static byte[] ConvertToPng(byte[] imageData) => ConvertToPngCore(new MagickImage(imageData));

    public static byte[] ConvertToPng(string filePath) => ConvertToPngCore(new MagickImage(filePath));

    private static byte[] ConvertToPngCore(MagickImage image)
    {
        using var disposableImage = image;
        try
        {
            image.Format = MagickFormat.Png;
            return image.ToByteArray();
        }
        catch { return null; }
    }
}