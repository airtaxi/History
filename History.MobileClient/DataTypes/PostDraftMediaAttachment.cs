namespace History.MobileClient.DataTypes;

/// <summary>
/// Serializable representation of a media attachment in a draft.
/// </summary>
public class PostDraftMediaAttachment
{
    /// <summary>
    /// Path to the temporary file on disk.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Original file name for upload.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Whether this attachment is a video.
    /// </summary>
    public bool IsVideo { get; set; }

    /// <summary>
    /// Media description text.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Whether the media is marked as spoiler.
    /// </summary>
    public bool IsSpoiler { get; set; }
}
