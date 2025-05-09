using History.ApiService.DataTypes;
using ImageMagick;
using System.Diagnostics;

namespace History.ApiService;

public static class MediaConverter
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
                var stopwatch = Stopwatch.StartNew();
                images.Coalesce(); // Coalesce the images to ensure all frames are processed
                Console.WriteLine($"Coalescing took: {stopwatch.ElapsedMilliseconds} ms (total: {images.Count} frames)");

                stopwatch.Restart();

                // Save each frame as an image
                Parallel.ForEach(images, frame =>
                {
                    var i = images.IndexOf(frame);

                    // Adjust resolution to even numbers if odd (h264, h265 requirement)
                    uint newWidth = frame.Width;
                    uint newHeight = frame.Height;

                    // Resize if necessary
                    if (maxWidth.HasValue && frame.Width > maxWidth.Value)
                    {
                        newWidth = maxWidth.Value;
                        newHeight = (uint)Math.Round((double)frame.Height * maxWidth.Value / frame.Width, 0);
                    }

                    if (newWidth % 2 != 0) newWidth++;
                    if (newHeight % 2 != 0) newHeight++;

                    if (newWidth != frame.Width || newHeight != frame.Height)
                    {
                        var size = new MagickGeometry(newWidth, newHeight) { IgnoreAspectRatio = true };
                        frame.Resize(size);
                    }

                    frame.Strip();
                    frame.Write(Path.Combine(tempDir, $"frame_{i:000}.png"));
                });

                Console.WriteLine($"Saving frames took: {stopwatch.ElapsedMilliseconds} ms (total: {images.Count} frames)");

                stopwatch.Restart();

                // Convert PNG sequence to MP4 using FFmpeg
                var outputMp4Path = Path.Combine(tempDir, "output.mp4");
                var framerate = DetermineFramerate(images);

                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-framerate {framerate} -i \"{Path.Combine(tempDir, "frame_%03d.png")}\" " +
                                    $"-c:v libx264 -crf 28 -an \"{outputMp4Path}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                ffmpegProcess.Start();
                ffmpegProcess.StandardError.ReadToEnd(); // If ommitted, ffmpeg hangs.
                ffmpegProcess.WaitForExit();

                if (ffmpegProcess.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg conversion failed: {ffmpegProcess.StandardError.ReadToEnd()}");
                }
                else
                {
                    Console.WriteLine($"FFmpeg conversion took: {stopwatch.ElapsedMilliseconds} ms (total: {images.Count} frames)");
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
            var image = images.FirstOrDefault();
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
