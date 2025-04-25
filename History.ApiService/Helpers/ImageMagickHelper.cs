using History.ApiService.DataTypes;
using ImageMagick;
using ImageMagick.Formats;
using System.Diagnostics;

namespace History.ApiService.Helpers;

public static class ImageMagickHelper
{
    public static ImageConvertResult ConvertAndSave(byte[] imageBytes, bool convertAnimatedImageToMp4)
    {
        using var images = new MagickImageCollection();
        images.Read(imageBytes);

        var isAnimated = images.Count > 1;

        if (isAnimated && convertAnimatedImageToMp4)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "anim2vid_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            try
            {
                var format = images[0].Format.ToString().ToLower(); // e.g., "gif" or "webp"
                string inputPath = Path.Combine(tempDir, $"input.{format}");
                string outputPath = Path.Combine(tempDir, "output.mp4");

                // Save imageBytes to temp file
                File.WriteAllBytes(inputPath, imageBytes);

                // Let ffmpeg handle the conversion, ensuring even dimensions
                var ffmpegArgs = $"-y -i \"{inputPath}\" -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" -movflags faststart -pix_fmt yuv420p -c:v libx264 \"{outputPath}\"";
                RunFFmpeg(ffmpegArgs);

                byte[] mp4Bytes = File.ReadAllBytes(outputPath);

                return new ImageConvertResult(true, mp4Bytes);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        else
        {
            using var image = (MagickImage)images.FirstOrDefault();
            image.Format = MagickFormat.WebP;
            image.Quality = 50;

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

    private static void RunFFmpeg(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"FFmpeg failed:\n{stderr}");
        }
    }
}
