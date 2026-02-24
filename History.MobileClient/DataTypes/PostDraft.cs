using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using System.Text.Json;

namespace History.MobileClient.DataTypes;

/// <summary>
/// Represents a saved post draft for later restoration.
/// </summary>
public class PostDraft
{
    private static readonly string DraftDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "History", "Drafts");
    private static readonly string DraftFilePath = Path.Combine(DraftDirectoryPath, "post_draft.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Text and profile/sticker content parts of the draft.
    /// </summary>
    public List<BaseContent> TextContents { get; set; } = [];

    /// <summary>
    /// Serializable media attachment info (file paths on disk).
    /// </summary>
    public List<PostDraftMediaAttachment> MediaAttachments { get; set; } = [];

    /// <summary>
    /// External URL content, if any.
    /// </summary>
    public ExternalUrlContent ExternalUrlContent { get; set; }

    /// <summary>
    /// Poll content, if any.
    /// </summary>
    public PollContent PollContent { get; set; }

    /// <summary>
    /// Hashtags attached to the draft.
    /// </summary>
    public List<string> Hashtags { get; set; } = [];

    /// <summary>
    /// Selected discovery option index.
    /// </summary>
    public int DiscoveryOptionIndex { get; set; }

    /// <summary>
    /// Comment permission, if set.
    /// </summary>
    public AccessPermission? CommentPermission { get; set; }

    /// <summary>
    /// Whether sharing/reposting is disallowed.
    /// </summary>
    public bool DisallowShare { get; set; }

    /// <summary>
    /// Timestamp when the draft was saved (UTC).
    /// </summary>
    public DateTime SavedAtUtc { get; set; }

    /// <summary>
    /// Saves the draft to disk.
    /// </summary>
    public static void Save(PostDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (!Directory.Exists(DraftDirectoryPath)) Directory.CreateDirectory(DraftDirectoryPath);

        var json = JsonSerializer.Serialize(draft, JsonOptions);
        File.WriteAllText(DraftFilePath, json);
    }

    /// <summary>
    /// Loads the draft from disk. Returns null if no draft exists or deserialization fails.
    /// </summary>
    public static PostDraft Load()
    {
        if (!File.Exists(DraftFilePath)) return null;

        try
        {
            var json = File.ReadAllText(DraftFilePath);
            if (string.IsNullOrWhiteSpace(json)) return null;

            var draft = JsonSerializer.Deserialize<PostDraft>(json, JsonOptions);
            if (draft == null) return null;

            // Validate that media files still exist on disk
            draft.MediaAttachments.RemoveAll(attachment => !File.Exists(attachment.FilePath));

            return draft;
        }
        catch { return null; }
    }

    /// <summary>
    /// Returns whether a draft exists on disk.
    /// </summary>
    public static bool Exists() => File.Exists(DraftFilePath);

    /// <summary>
    /// Deletes the draft and its associated media files from disk.
    /// </summary>
    public static void Delete()
    {
        if (!File.Exists(DraftFilePath)) return;

        try
        {
            var draft = Load();
            if (draft != null)
            {
                foreach (var attachment in draft.MediaAttachments)
                {
                    try { if (File.Exists(attachment.FilePath)) File.Delete(attachment.FilePath); }
                    catch { }
                }
            }
        }
        catch { }

        try { File.Delete(DraftFilePath); }
        catch { }
    }
}

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
