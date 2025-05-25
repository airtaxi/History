using History.ApiService.DataTypes;
using ImageMagick;
using System.Diagnostics;

namespace History.ApiService.Helpers;

public static class MediaEncodingHelper
{
    public static MediaConvertResult ConvertImage(byte[] imageBytes, bool convertAnimatedImageToMp4, uint? maxWidth = null)
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
                                    $"-c:v libx264 -profile:v high -level:v 4.0 -pix_fmt yuv420p -preset fast -crf 28 -movflags +faststart \"{outputMp4Path}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                ffmpegProcess.Start();
                var error = ffmpegProcess.StandardError.ReadToEnd(); // If ommitted, ffmpeg hangs.
                ffmpegProcess.WaitForExit();

                if (ffmpegProcess.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg conversion failed: {error + ffmpegProcess.StandardError.ReadToEnd()}");
                }
                else
                {
                    Console.WriteLine($"FFmpeg conversion took: {stopwatch.ElapsedMilliseconds} ms (total: {images.Count} frames)");
                }

                // Read the generated MP4 file
                var mp4Bytes = File.ReadAllBytes(outputMp4Path);

                return new MediaConvertResult(true, mp4Bytes);
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
            image.AutoOrient(); // Pre-apply rotation


            if (maxWidth.HasValue && image.Width > maxWidth.Value)
            {
                var newHeight = (uint)Math.Round((double)image.Height * maxWidth.Value / image.Width, 0);
                var size = new MagickGeometry(maxWidth.Value, newHeight) { IgnoreAspectRatio = true };
                image.Resize(size);
            }
            image.Strip();
            using var ms = new MemoryStream();
            image.Write(ms);
            return new MediaConvertResult(false, ms.ToArray());
        }
    }

    // Helper method to determine the frame rate of animated images
    private static double DetermineFramerate(MagickImageCollection images)
    {
        if (images == null || images.Count == 0)
        {
            return 0;
        }

        double totalDelay = 0;
        int frameCount = 0;

        foreach (var frame in images)
        {
            // Usually, a delay of 0 is invalid or too fast to render correctly.
            // Assign a minimum fallback delay if needed.
            uint delay = frame.AnimationDelay;
            if (delay == 0)
            {
                delay = 10; // Fallback to minimum 0.1s (10/100s)
            }

            totalDelay += delay;
            frameCount++;
        }

        // Calculate average delay and convert it to FPS.
        // Delay is in 1/100ths of a second, so multiply by 100 to get FPS.
        double averageDelay = totalDelay / frameCount;
        double fps = 100.0 / averageDelay;

        return fps;
    }

    public static MediaConvertResult ConvertVideo(byte[] videoBytes, uint? maxWidth = null)
    {
        // Create temporary directory
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Save input video to temporary file
            var inputVideoPath = Path.Combine(tempDir, "input_video");
            File.WriteAllBytes(inputVideoPath, videoBytes);

            // Get video information using ffprobe
            var probeProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 \"{inputVideoPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            probeProcess.Start();
            var dimensions = probeProcess.StandardOutput.ReadToEnd().Trim();
            probeProcess.WaitForExit();

            uint originalWidth = 0;
            uint originalHeight = 0;

            if (!string.IsNullOrEmpty(dimensions) && dimensions.Contains('x'))
            {
                var parts = dimensions.Split('x');
                if (parts.Length == 2)
                {
                    uint.TryParse(parts[0], out originalWidth);
                    uint.TryParse(parts[1], out originalHeight);
                }
            }

            // Calculate new dimensions if maxWidth is specified
            string scaleFilter = "";
            if (maxWidth.HasValue && originalWidth > 0 && originalWidth > maxWidth.Value)
            {
                uint newWidth = maxWidth.Value;
                uint newHeight = (uint)Math.Round((double)originalHeight * maxWidth.Value / originalWidth, 0);

                // Ensure dimensions are even (h264 requirement)
                if (newWidth % 2 != 0) newWidth++;
                if (newHeight % 2 != 0) newHeight++;

                scaleFilter = $"-vf scale={newWidth}:{newHeight} ";
            }
            else if (originalWidth > 0 && originalHeight > 0)
            {
                // Ensure original dimensions are even
                uint newWidth = originalWidth;
                uint newHeight = originalHeight;

                if (newWidth % 2 != 0) newWidth++;
                if (newHeight % 2 != 0) newHeight++;

                if (newWidth != originalWidth || newHeight != originalHeight)
                {
                    scaleFilter = $"-vf scale={newWidth}:{newHeight} ";
                }
            }

            Console.WriteLine($"Probing video took: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            // Convert to MP4 using same settings as ConvertImage
            var outputMp4Path = Path.Combine(tempDir, "output.mp4");

            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{inputVideoPath}\" " +
                               scaleFilter +
                               "-c:v libx264 -profile:v high -level:v 4.0 -pix_fmt yuv420p -preset fast -crf 28 " +
                               "-movflags +faststart " +
                               "-c:a aac -b:a 128k " +  // Add audio codec if present
                               $"\"{outputMp4Path}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            ffmpegProcess.Start();
            var error = ffmpegProcess.StandardError.ReadToEnd();
            ffmpegProcess.WaitForExit();

            if (ffmpegProcess.ExitCode != 0)
            {
                throw new Exception($"FFmpeg conversion failed: {error}");
            }

            Console.WriteLine($"FFmpeg conversion took: {stopwatch.ElapsedMilliseconds} ms");

            // Read the generated MP4 file
            var mp4Bytes = File.ReadAllBytes(outputMp4Path);

            return new MediaConvertResult(true, mp4Bytes);
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
}
