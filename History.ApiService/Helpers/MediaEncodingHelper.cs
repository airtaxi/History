using History.ApiService.DataTypes;
using System.Diagnostics;
using System.Text;

namespace History.ApiService.Helpers;

public static class MediaEncodingHelper
{
    private const string WebPAnimationFlag = "VP8X";
    private const string GifAnimationExtension = "NETSCAPE2.0";
    private const string ApngAnimationChunk = "acTL";

    public static MediaConvertResult ConvertImage(byte[] imageBytes, bool convertAnimatedImageToMp4, bool noAlpha = false, uint? maxWidth = null, uint? maxHeight = null)
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var inputPath = Path.Combine(tempDir, "input_image");
            File.WriteAllBytes(inputPath, imageBytes);

            var (width, height) = ProbeImage(inputPath);
            var isAnimated = IsAnimatedImage(imageBytes);

            if (isAnimated && convertAnimatedImageToMp4)
            {
                return ConvertAnimatedToMp4(inputPath, tempDir, width, height, maxWidth, maxHeight);
            }
            else
            {
                var outputPath = Path.Combine(tempDir, "output.webp");
                var scaleFilter = BuildScaleFilter(width, height, maxWidth, maxHeight, roundToMultipleOf8: false, noAlpha);
                var arguments = $"-i \"{inputPath}\" {scaleFilter}-frames:v 1 -c:v libwebp -quality 50 -map_metadata -1 \"{outputPath}\"";

                RunProcess("ffmpeg", arguments);

                return new MediaConvertResult(false, File.ReadAllBytes(outputPath));
            }
        }
        finally { DeleteTempDirectory(tempDir); }
    }

    public static MediaConvertResult ConvertAnimatedWebP(byte[] imageBytes, bool noAlpha = false, uint? maxWidth = null, uint? maxHeight = null)
    {
        // Not animated — fall back to static conversion
        if (!IsAnimatedWebP(imageBytes)) return ConvertImage(imageBytes, false, noAlpha, maxWidth, maxHeight);

        return ConvertAnimatedImage(imageBytes, noAlpha, maxWidth, maxHeight);
    }

    public static MediaConvertResult ConvertAnimatedImage(byte[] imageBytes, bool noAlpha = false, uint? maxWidth = null, uint? maxHeight = null)
    {
        // Not animated — fall back to static conversion
        if (!IsAnimatedImage(imageBytes)) return ConvertImage(imageBytes, false, noAlpha, maxWidth, maxHeight);

        var tempDir = CreateTempDirectory();

        try
        {
            var inputPath = Path.Combine(tempDir, "input_image");
            File.WriteAllBytes(inputPath, imageBytes);

            var (width, height) = ProbeImage(inputPath);
            var outputPath = Path.Combine(tempDir, "output.webp");
            var scaleFilter = BuildScaleFilter(width, height, maxWidth, maxHeight, roundToMultipleOf8: false, noAlpha);
            var arguments = $"-i \"{inputPath}\" {scaleFilter}-c:v libwebp_anim -quality 50 -loop 0 -map_metadata -1 \"{outputPath}\"";

            RunProcess("ffmpeg", arguments);

            return new MediaConvertResult(false, File.ReadAllBytes(outputPath), isAnimated: true);
        }
        finally { DeleteTempDirectory(tempDir); }
    }

    public static MediaConvertResult ConvertVideo(byte[] videoBytes, uint? maxDimension = null)
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Save input video to temporary file
            var inputVideoPath = Path.Combine(tempDir, "input_video");
            File.WriteAllBytes(inputVideoPath, videoBytes);

            // Get video information using ffprobe
            var (originalWidth, originalHeight) = ProbeImage(inputVideoPath);

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

                if (newWidth != originalWidth || newHeight != originalHeight) scaleFilter = $"-vf scale={newWidth}:{newHeight} ";
            }

            Console.WriteLine($"Probing video took: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            // Convert to MP4 using same settings as ConvertImage
            var outputMp4Path = Path.Combine(tempDir, "output.mp4");
            var arguments = $"-i \"{inputVideoPath}\" " +
                           scaleFilter +
                           "-c:v libx264 -profile:v high -level:v 4.0 -pix_fmt yuv420p -preset fast -crf 28 " +
                           "-movflags +faststart " +
                           "-c:a aac -b:a 128k " +  // Add audio codec if present
                           $"\"{outputMp4Path}\"";

            RunProcess("ffmpeg", arguments);

            Console.WriteLine($"FFmpeg conversion took: {stopwatch.ElapsedMilliseconds} ms");

            // Read the generated MP4 file
            var mp4Bytes = File.ReadAllBytes(outputMp4Path);

            return new MediaConvertResult(true, mp4Bytes);
        }
        finally { DeleteTempDirectory(tempDir); }
    }

    public static MediaConvertResult GenerateThumbnailFromVideo(byte[] videoBytes, uint? maxWidth = null)
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Save input video to temporary file
            var inputVideoPath = Path.Combine(tempDir, "input_video");
            File.WriteAllBytes(inputVideoPath, videoBytes);

            // Extract first frame and convert to WebP using FFmpeg
            var outputImagePath = Path.Combine(tempDir, "thumbnail.webp");
            var scaleFilter = "";
            if (maxWidth.HasValue)
            {
                var (width, height) = ProbeImage(inputVideoPath);
                if (width > maxWidth.Value)
                {
                    var newHeight = (uint)Math.Round((double)height * maxWidth.Value / width, 0);
                    scaleFilter = $"-vf scale={maxWidth.Value}:{newHeight} ";
                }
            }

            var arguments = $"-i \"{inputVideoPath}\" -vframes 1 {scaleFilter}-c:v libwebp -quality 50 -map_metadata -1 \"{outputImagePath}\"";
            RunProcess("ffmpeg", arguments);

            Console.WriteLine($"Thumbnail extraction took: {stopwatch.ElapsedMilliseconds} ms");

            return new MediaConvertResult(false, File.ReadAllBytes(outputImagePath));
        }
        finally { DeleteTempDirectory(tempDir); }
    }

    private static MediaConvertResult ConvertAnimatedToMp4(string inputPath, string tempDir, uint width, uint height, uint? maxWidth, uint? maxHeight)
    {
        var stopwatch = Stopwatch.StartNew();

        // The webp_anim demuxer loops forever regardless of the loop count, so limit the frame count explicitly.
        var frameCount = ProbeFrameCount(inputPath);
        var framerate = ProbeFramerate(inputPath);

        // Save each frame as an image
        var framePattern = Path.Combine(tempDir, "frame_%03d.png");
        var scaleFilter = BuildScaleFilter(width, height, maxWidth, maxHeight, roundToMultipleOf8: true);
        var extractArguments = $"-i \"{inputPath}\" -frames:v {frameCount} {scaleFilter}\"{framePattern}\"";
        RunProcess("ffmpeg", extractArguments);

        Console.WriteLine($"Saving frames took: {stopwatch.ElapsedMilliseconds} ms (total: {frameCount} frames)");

        stopwatch.Restart();

        // Convert PNG sequence to MP4 using FFmpeg
        var outputMp4Path = Path.Combine(tempDir, "output.mp4");
        var convertArguments = $"-framerate {framerate} -i \"{framePattern}\" " +
                               "-c:v libx264 -profile:v high -level:v 4.0 -pix_fmt yuv420p -preset fast -crf 28 -movflags +faststart " +
                               $"\"{outputMp4Path}\"";
        RunProcess("ffmpeg", convertArguments);

        Console.WriteLine($"FFmpeg conversion took: {stopwatch.ElapsedMilliseconds} ms (total: {frameCount} frames)");

        // Read the generated MP4 file
        var mp4Bytes = File.ReadAllBytes(outputMp4Path);

        return new MediaConvertResult(true, mp4Bytes);
    }

    private static (uint Width, uint Height) ProbeImage(string path)
    {
        var probeProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height -of default=noprint_wrappers=1 \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        probeProcess.Start();
        var probeOutput = probeProcess.StandardOutput.ReadToEnd();
        probeProcess.WaitForExit();

        uint width = 0;
        uint height = 0;

        foreach (var line in probeOutput.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("width=")) uint.TryParse(trimmedLine["width=".Length..], out width);
            else if (trimmedLine.StartsWith("height=")) uint.TryParse(trimmedLine["height=".Length..], out height);
        }

        return (width, height);
    }

    private static int ProbeFrameCount(string path)
    {
        var probeProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of default=noprint_wrappers=1:nokey=1 \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        probeProcess.Start();
        var frameCountText = probeProcess.StandardOutput.ReadToEnd().Trim();
        probeProcess.WaitForExit();

        return int.TryParse(frameCountText, out var frameCount) ? frameCount : 1;
    }

    // Helper method to determine the frame rate of animated images
    private static double ProbeFramerate(string path)
    {
        var probeProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -select_streams v:0 -show_entries frame=duration_time -of csv=p=0 \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        probeProcess.Start();
        var durationsText = probeProcess.StandardOutput.ReadToEnd().Trim();
        probeProcess.WaitForExit();

        double totalDuration = 0;
        int frameCount = 0;

        foreach (var durationText in durationsText.Split('\n'))
        {
            if (!double.TryParse(durationText.Trim(), out var duration) || duration <= 0) continue;

            // Usually, a delay of 0 is invalid or too fast to render correctly.
            // Assign a minimum fallback delay if needed.
            if (duration == 0) duration = 0.1; // Fallback to minimum 0.1s

            totalDuration += duration;
            frameCount++;
        }

        if (frameCount == 0) return 0;

        // Calculate average delay and convert it to FPS.
        double averageDuration = totalDuration / frameCount;
        double fps = 1.0 / averageDuration;

        return fps;
    }

    private static bool IsAnimatedImage(byte[] imageBytes) => IsAnimatedWebP(imageBytes) || IsAnimatedGif(imageBytes) || IsAnimatedApng(imageBytes);

    private static bool IsAnimatedWebP(byte[] imageBytes)
    {
        // RIFF + WEBP + VP8X header with the animation flag bit (0x02) set at offset 20.
        if (imageBytes.Length < 21) return false;
        if (Encoding.ASCII.GetString(imageBytes, 0, 4) != "RIFF") return false;
        if (Encoding.ASCII.GetString(imageBytes, 8, 4) != "WEBP") return false;
        if (Encoding.ASCII.GetString(imageBytes, 12, 4) != WebPAnimationFlag) return false;

        return (imageBytes[20] & 0x02) != 0;
    }

    private static bool IsAnimatedGif(byte[] imageBytes)
    {
        // GIF89a header followed by a NETSCAPE2.0 application extension.
        if (imageBytes.Length < 6) return false;
        if (Encoding.ASCII.GetString(imageBytes, 0, 6) != "GIF89a") return false;

        return IndexOf(imageBytes, Encoding.ASCII.GetBytes(GifAnimationExtension)) >= 0;
    }

    private static bool IsAnimatedApng(byte[] imageBytes)
    {
        // PNG signature followed by an acTL animation control chunk.
        if (imageBytes.Length < 8) return false;
        if (imageBytes[0] != 0x89 || imageBytes[1] != 0x50 || imageBytes[2] != 0x4E || imageBytes[3] != 0x47) return false;

        return IndexOf(imageBytes, Encoding.ASCII.GetBytes(ApngAnimationChunk)) >= 0;
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var index = 0; index <= haystack.Length - needle.Length; index++)
        {
            var match = true;
            for (var needleIndex = 0; needleIndex < needle.Length; needleIndex++)
            {
                if (haystack[index + needleIndex] != needle[needleIndex])
                {
                    match = false;
                    break;
                }
            }
            if (match) return index;
        }

        return -1;
    }

    private static string BuildScaleFilter(uint width, uint height, uint? maxWidth, uint? maxHeight, bool roundToMultipleOf8, bool noAlpha = false)
    {
        if (width == 0 || height == 0) return noAlpha ? "-vf format=rgb24 " : "";

        uint newWidth = width;
        uint newHeight = height;

        if (maxWidth.HasValue && width > maxWidth.Value)
        {
            newWidth = maxWidth.Value;
            newHeight = (uint)Math.Round((double)height * maxWidth.Value / width, 0);
        }
        if (maxHeight.HasValue && newHeight > maxHeight.Value)
        {
            newHeight = maxHeight.Value;
            newWidth = (uint)Math.Round((double)width * maxHeight.Value / height, 0);
        }

        if (roundToMultipleOf8)
        {
            if (newWidth % 8 != 0) newWidth -= newWidth % 8;
            if (newHeight % 8 != 0) newHeight -= newHeight % 8;
        }

        if (newWidth == width && newHeight == height) return noAlpha ? "-vf format=rgb24 " : "";

        return noAlpha ? $"-vf scale={newWidth}:{newHeight},format=rgb24 " : $"-vf scale={newWidth}:{newHeight} ";
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void DeleteTempDirectory(string tempDir)
    {
        try { Directory.Delete(tempDir, true); }
        catch { /* Ignore cleanup failures */ }
    }

    private static void RunProcess(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var error = process.StandardError.ReadToEnd(); // If ommitted, ffmpeg hangs.
        process.WaitForExit();

        if (process.ExitCode != 0) throw new Exception($"{fileName} failed with exit code {process.ExitCode}: {error}");
    }
}
