using History.ApiService.DataTypes;
using ImageMagick;
using ImageMagick.Formats;

namespace History.ApiService.Helpers;

public static class ImageMagickHelper
{
    public static ImageConvertResult ConvertAndSave(byte[] imageBytes)
    {
        using var images = new MagickImageCollection();
        images.Read(imageBytes);
        MagickFormat format = GetFormatFromBytes(imageBytes);

        var isAnimated = images.Count > 1;

        if (isAnimated)
        {
            images.Coalesce();

            using var video = new MagickImageCollection();
            foreach (var frame in images) video.Add(frame.Clone());

            using var ms = new MemoryStream();
            video.Write(ms, MagickFormat.Mp4);
            return new ImageConvertResult(true, ms.ToArray());
        }
        else
        {
            using var image = (MagickImage)images.FirstOrDefault();
            image.Format = MagickFormat.WebP;
            image.Quality = 75;

            var defines = new WebPWriteDefines
            {
                Lossless = false,
                Method = 6
            };

            using var ms = new MemoryStream();
            image.Write(ms);
            return new ImageConvertResult(false, ms.ToArray());
        }
    }

    private static MagickFormat GetFormatFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        var info = new MagickImageInfo(ms);
        return info.Format;
    }
}
