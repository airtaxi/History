using History.ApiService.DataTypes;
using ImageMagick;
using ImageMagick.Formats;
using System.Diagnostics;

namespace History.ApiService.Helpers;

public static class ImageMagickHelper
{
    public static ImageConvertResult ConvertAndSave(byte[] imageBytes, bool convertAnimatedImageToMp4, uint? maxWidth = null)
    {
        using var images = new MagickImageCollection();
        images.Read(imageBytes);
        var isAnimated = images.Count > 1;

        if (isAnimated && convertAnimatedImageToMp4)
        {
            // Create temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                images.Coalesce(); // Coalesce the images to ensure all frames are processed

                // Save each frame as an image
                Parallel.ForEach(images, frame =>
                {
                    var i = images.IndexOf(frame);

                    // Adjust resolution to even numbers if odd (h264 requirement)
                    if (frame.Width % 2 != 0)
                    {
                        frame.Resize(frame.Width + 1, frame.Height);
                    }
                    if (frame.Height % 2 != 0)
                    {
                        frame.Resize(frame.Width, frame.Height + 1);
                    }

                    // Resize if necessary
                    if (maxWidth.HasValue && frame.Width > maxWidth.Value)
                    {
                        var newHeight = (uint)Math.Round((double)frame.Height * maxWidth.Value / frame.Width, 0);
                        // Adjust to even height for h264 encoding
                        if (newHeight % 2 != 0) newHeight++;

                        var size = new MagickGeometry(maxWidth.Value, newHeight) { IgnoreAspectRatio = true };
                        frame.Resize(size);
                    }

                    frame.Strip();
                    frame.Write(Path.Combine(tempDir, $"frame_{i:000}.png"));
                });

                // Convert PNG sequence to MP4 using FFmpeg
                var outputMp4Path = Path.Combine(tempDir, "output.mp4");
                var framerate = DetermineFramerate(images);

                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-framerate {framerate} -i \"{Path.Combine(tempDir, "frame_%03d.png")}\" " +
                                    $"-c:v libx265 -pix_fmt yuv420p -crf 23 \"{outputMp4Path}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();

                if (ffmpegProcess.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg conversion failed: {ffmpegProcess.StandardError.ReadToEnd()}");
                }

                // Read the generated MP4 file
                var mp4Bytes = File.ReadAllBytes(outputMp4Path);

                return new ImageConvertResult(true, mp4Bytes);
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }
        }
        else
        {
            using var image = new MagickImage(imageBytes);
            image.Format = MagickFormat.WebP;
            image.Quality = 50;
            if (maxWidth.HasValue && image.Width > maxWidth.Value)
            {
                var newHeight = (uint)Math.Round((double)image.Height * maxWidth.Value / image.Width, 0);
                var size = new MagickGeometry(maxWidth.Value, newHeight) { IgnoreAspectRatio = true };
                image.Resize(size);
            }
            image.Strip();
            using var ms = new MemoryStream();
            image.Write(ms);
            return new ImageConvertResult(false, ms.ToArray());
        }
    }

    // Helper method to determine the frame rate of animated images
    private static double DetermineFramerate(MagickImageCollection images)
    {
        // Calculate frame rate based on the first frame's delay time
        // Delay is specified in hundredths of a second
        uint delay = images[0].AnimationDelay;

        // Use default frame rate if delay is 0
        if (delay == 0)
        {
            return 10; // Default to 10 FPS
        }

        // Convert delay time to frame rate (hundredths of a second -> frames per second)
        return 100.0 / delay;
    }
}
