using History.ApiService.DataTypes;
using ImageMagick;
using Org.BouncyCastle.Asn1.X509;
using System.Diagnostics;

namespace History.ApiService.Helpers;

public static class MediaEncodingHelper
{
    public static MediaConvertResult ConvertImage(byte[] imageBytes, bool convertAnimatedImageToMp4, bool noAlpha = false, uint? maxWidth = null, uint? maxHeight = null)
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
                    frame.Alpha(AlphaOption.Off);

                    // Adjust resolution to even numbers if odd (h264, h265 requirement)
                    uint newWidth = frame.Width;
                    uint newHeight = frame.Height;

                    // Resize if necessary
                    if (maxWidth.HasValue && frame.Width > maxWidth.Value)
                    {
                        newWidth = maxWidth.Value;
                        newHeight = (uint)Math.Round((double)frame.Height * maxWidth.Value / frame.Width, 0);
                    }
                    if (maxHeight.HasValue && newHeight > maxHeight.Value)
                    {
                        newHeight = maxHeight.Value;
                        newWidth = (uint)Math.Round((double)frame.Width * maxHeight.Value / frame.Height, 0);
                    }

                    if (newWidth % 8 != 0) newWidth -= newWidth % 8;
                    if (newHeight % 8 != 0) newHeight -= newHeight % 8;

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
                    throw new Exception($"FFmpeg conversion failed.");
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
            if (noAlpha) image.Alpha(AlphaOption.Off); // Disable alpha channel for WebP

            uint newWidth = image.Width;
            uint newHeight = image.Height;

            if (maxWidth.HasValue && image.Width > maxWidth.Value)
            {
                newWidth = maxWidth.Value;
                newHeight = (uint)Math.Round((double)image.Height * maxWidth.Value / image.Width, 0);
            }
            if (maxHeight.HasValue && newHeight > maxHeight.Value)
            {
                newHeight = maxHeight.Value;
                newWidth = (uint)Math.Round((double)image.Width * maxHeight.Value / image.Height, 0);
            }

            if (newWidth != image.Width || newHeight != image.Height)
            {
                var size = new MagickGeometry(newWidth, newHeight) { IgnoreAspectRatio = true };
                image.Resize(size);
            }

            image.Strip();
            using var ms = new MemoryStream();
            image.Write(ms);
            return new MediaConvertResult(false, ms.ToArray());
        }
    }

    public static MediaConvertResult ConvertAnimatedWebP(byte[] imageBytes, bool noAlpha = false, uint? maxWidth = null, uint? maxHeight = null)
    {
        using var images = new MagickImageCollection();
        images.Read(imageBytes);
        var isAnimated = images.Count > 1;

        // Not animated — fall back to static conversion
        if (!isAnimated) return ConvertImage(imageBytes, false, noAlpha, maxWidth, maxHeight);

        images.Coalesce();

        foreach (var frame in images)
        {
            frame.AutoOrient();
            if (noAlpha) frame.Alpha(AlphaOption.Off);

            uint newWidth = frame.Width;
            uint newHeight = frame.Height;

            if (maxWidth.HasValue && frame.Width > maxWidth.Value)
            {
                newWidth = maxWidth.Value;
                newHeight = (uint)Math.Round((double)frame.Height * maxWidth.Value / frame.Width, 0);
            }
            if (maxHeight.HasValue && newHeight > maxHeight.Value)
            {
                newHeight = maxHeight.Value;
                newWidth = (uint)Math.Round((double)frame.Width * maxHeight.Value / frame.Height, 0);
            }

            if (newWidth != frame.Width || newHeight != frame.Height)
            {
                var size = new MagickGeometry(newWidth, newHeight) { IgnoreAspectRatio = false };
                frame.Resize(size);
            }

            frame.Format = MagickFormat.WebP;
            frame.Quality = 50;
            frame.Strip();
        }

        using var memoryStream = new MemoryStream();
        images.Write(memoryStream, MagickFormat.WebP);
        return new MediaConvertResult(false, memoryStream.ToArray(), isAnimated: true);
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

    public static MediaConvertResult ConvertVideo(byte[] videoBytes, uint? maxDimension = null)
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

            var shorterDimension = Math.Min(originalWidth, originalHeight);
            var isLandscape = originalHeight > originalWidth;

            // Calculate new dimensions if maxWidth is specified
            string scaleFilter = "";
            if (maxDimension.HasValue && shorterDimension > 0 && shorterDimension > maxDimension.Value)
            {
                uint newWidth, newHeight;
                if (isLandscape)
                {
                    newHeight = maxDimension.Value;
                    newWidth = (uint)Math.Round((double)originalWidth * maxDimension.Value / originalHeight, 0);
                }
                else
                {
                    newWidth = maxDimension.Value;
                    newHeight = (uint)Math.Round((double)originalHeight * maxDimension.Value / originalWidth, 0);
                }

                // Ensure dimensions are even (h264 requirement)
                if (newWidth % 8 != 0) newWidth -= newWidth % 8;
                if (newHeight % 8 != 0) newHeight -= newHeight % 8;

                scaleFilter = $"-vf scale={newWidth}:{newHeight} ";
            }
            else if (originalWidth > 0 && originalHeight > 0)
            {
                // Ensure original dimensions are even
                uint newWidth = originalWidth;
                uint newHeight = originalHeight;

                if (newWidth % 8 != 0) newWidth -= newWidth % 8;
                if (newHeight % 8 != 0) newHeight -= newHeight % 8;

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
                throw new Exception($"FFmpeg conversion failed.");
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

    public static MediaConvertResult GenerateThumbnailFromVideo(byte[] videoBytes, uint? maxWidth = null)
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

            // Extract first frame using FFmpeg
            var outputImagePath = Path.Combine(tempDir, "thumbnail.jpg");

            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{inputVideoPath}\" -vframes 1 -q:v 2 \"{outputImagePath}\"",
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
                throw new Exception($"FFmpeg thumbnail generation failed.");
            }

            Console.WriteLine($"Thumbnail extraction took: {stopwatch.ElapsedMilliseconds} ms");

            // Read the generated image and process with ImageMagick
            var jpgBytes = File.ReadAllBytes(outputImagePath);

            using var image = new MagickImage(jpgBytes);
            image.Format = MagickFormat.WebP;
            image.Quality = 50;
            image.AutoOrient();

            // Resize if necessary
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
