using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Streams;

namespace History.WindowsClient.Helpers;

// Extracts a thumbnail frame from a local video file through the built-in Windows media
// pipeline (MediaComposition, MP4/H.264). Returns null when the file cannot be opened or
// has an unsupported format so callers can fall back to the video placeholder image
// (Assets/App/Video.png).
public static class VideoThumbnailExtractor
{
    public static async Task<IRandomAccessStream> GetThumbnailAsync(string videoPath, int width = 0)
    {
        try
        {
            var mediaComposition = new MediaComposition();
            var videoFile = await StorageFile.GetFileFromPathAsync(videoPath);
            var videoClip = await MediaClip.CreateFromFileAsync(videoFile);
            mediaComposition.Clips.Add(videoClip);
            return await mediaComposition.GetThumbnailAsync(TimeSpan.Zero, width, 0, VideoFramePrecision.NearestFrame);
        }
        catch (Exception) { return null; }
    }
}