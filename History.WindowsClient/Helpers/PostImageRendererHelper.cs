using History.Commons.DataTypes.Contents;
using History.Rendering;
using History.WindowsClient.ViewModels;

namespace History.WindowsClient.Helpers;

/// <summary>
/// Renders post contents into a single vertical PNG image using SkiaSharp via History.Rendering.
/// Contents are stacked vertically (no carousel), spoilers are never hidden,
/// and videos are exported as their thumbnail with a play overlay.
/// </summary>
public static class PostImageRendererHelper
{
    private static PostImageRendererResources s_resources;
    private static readonly SemaphoreSlim s_resourceSemaphore = new(1, 1);

    /// <summary>
    /// Renders the given post contents into PNG bytes.
    /// Optional header (profile image, nickname, timestamp) is drawn above the contents;
    /// header values are derived from the shared post view model surface, and the
    /// timestamp is always absolute (relative timestamps are meaningless in exported images).
    /// When the post view model is a share (IsShare), the shared (parent) post card is drawn
    /// between the contents and the comments with the parent's profile header and contents.
    /// When includeHeader is false the profile header is omitted but the shared post card is kept.
    /// Optional comments are drawn below the contents under a thin separator, with the
    /// same absolute timestamp rule; contents are built from the comment view model surface.
    /// When excludeMediaExceptFirst is set, only the first MediaContent is rendered and
    /// every later MediaContent is skipped so it can be attached as a regular file instead.
    /// </summary>
    public static async Task<byte[]> RenderAsync(IEnumerable<BaseContent> contents, BasePostViewModel post = null, IEnumerable<BaseCommentViewModel> comments = null, bool excludeMediaExceptFirst = false, bool includeHeader = true)
    {
        var resources = await GetResourcesAsync();
        var header = post != null && includeHeader ? new PostRenderHeader
        {
            ProfileImageUrl = post.ProfileMediaUri,
            Nickname = post.Nickname,
            CreatedAt = post.CreatedAt,
            ModifiedAt = post.ModifiedAt
        } : null;

        SharedPostRenderData sharedPostData = null;
        if (post is { IsShare: true, ParentPost: not null })
        {
            var parent = post.ParentPost;
            var parentContents = parent.GetRenderRawContents();
            if (parentContents is { Count: > 0 })
            {
                sharedPostData = new SharedPostRenderData
                {
                    ProfileImageUrl = parent.ProfileMediaUri,
                    Nickname = parent.Nickname,
                    Contents = parentContents,
                    SharedUsersCount = parent.HasSharedUsers ? parent.SharedUsersCount : 0
                };
            }
        }

        var commentData = comments?.Select(comment => new CommentRenderData
        {
            ProfileImageUrl = comment.ProfileMediaUri,
            Nickname = comment.Nickname,
            Contents = comment.GetRenderRawContents() ?? [],
            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.ModifiedAt
        });

        return await PostImageRenderer.RenderAsync(contents, resources, header, commentData, excludeMediaExceptFirst, sharedPostData);
    }

    private static async Task<PostImageRendererResources> GetResourcesAsync()
    {
        if (s_resources != null) return s_resources;

        await s_resourceSemaphore.WaitAsync();
        try
        {
            if (s_resources != null) return s_resources;

            var fontBytes = await TryLoadAssetFileAsync("Assets/App/Renderer/PretendardVariable.ttf");
            var profileBytes = await TryLoadAssetFileAsync("Assets/App/DefaultProfileImage.jpg");
            s_resources = new PostImageRendererResources
            {
                FontBytes = fontBytes,
                DefaultProfileImageBytes = profileBytes
            };
            return s_resources;
        }
        finally { s_resourceSemaphore.Release(); }
    }

    private static async Task<byte[]> TryLoadAssetFileAsync(string relativePath)
    {
        try
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (!File.Exists(fullPath)) return null;
            return await File.ReadAllBytesAsync(fullPath);
        }
        catch { return null; }
    }
}