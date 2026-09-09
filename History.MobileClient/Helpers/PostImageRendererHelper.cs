using CommunityToolkit.Maui.Alerts;
using History.Commons.DataTypes.Contents;
using History.MobileClient.ViewModels;
using History.Rendering;
#if !WINDOWS
using NativeMedia;
#endif

namespace History.MobileClient.Helpers;

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
    /// Builds the absolute timestamp text for image export headers.
    /// Photos have no "live" state, so relative stamps ("5분 전") are meaningless;
    /// always render the full absolute local time.
    /// </summary>
    public static string BuildFullTimestampText(DateTime createdAt, DateTime? modifiedAt = null) => PostImageRenderer.BuildFullTimestampText(createdAt, modifiedAt);

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
            ProfileImageUrl = post.ProfileMedia?.Uri,
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
                    ProfileImageUrl = parent.ProfileMedia?.Uri,
                    Nickname = parent.Nickname,
                    Contents = parentContents,
                    SharedUsersCount = parent.HasSharedUsers ? parent.SharedUsersCount : 0
                };
            }
        }

        var commentData = comments?.Select(comment => new CommentRenderData
        {
            ProfileImageUrl = comment.ProfileMedia?.Uri,
            Nickname = comment.Nickname,
            Contents = comment.GetRenderRawContents() ?? [],
            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.ModifiedAt
        });

        return await PostImageRenderer.RenderAsync(contents, resources, header, commentData, excludeMediaExceptFirst, sharedPostData);
    }

    /// <summary>
    /// Renders the post contents with a header derived from the shared post view model
    /// surface (profile media URI, nickname, absolute timestamp) and saves the resulting
    /// PNG to the device gallery. When the post view model is a share, the shared (parent)
    /// post card is included; when includeHeader is false the profile header is omitted
    /// but the shared post card is kept. Comments are appended below the contents when provided.
    /// Mirrors the save flow of FullScreenMediaViewerPage (permission → temp file →
    /// gallery → cleanup) and surfaces the result through a toast or an error alert.
    /// </summary>
    public static async Task SaveAsync(IEnumerable<BaseContent> contents, BasePostViewModel post = null, IEnumerable<BaseCommentViewModel> comments = null, bool includeHeader = true)
    {
#if !WINDOWS
        var status = await Permissions.RequestAsync<SaveMediaPermission>();
        if (status != PermissionStatus.Granted) return;
#endif

        byte[] bytes;
        try { bytes = await RenderAsync(contents, post, comments, includeHeader: includeHeader); }
        catch
        {
            await App.TopPage.DisplayAlertAsync("오류", "게시글 이미지 생성 중 오류가 발생하였습니다.", Constants.PromptOk);
            return;
        }

        if (bytes == null)
        {
            await App.TopPage.DisplayAlertAsync("오류", "이미지로 저장할 내용이 없습니다.", Constants.PromptOk);
            return;
        }

        var fileName = $"post_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        try
        {
            await File.WriteAllBytesAsync(filePath, bytes);
#if WINDOWS
            await WindowsMediaPickerHelper.SaveMediaAsync(filePath);
#else
            await MediaGallery.SaveAsync(MediaFileType.Image, filePath);
#endif
            await Toast.Make("게시글 이미지가 저장되었습니다.").Show();
        }
        catch { await App.TopPage.DisplayAlertAsync("오류", "게시글 이미지 저장 중 오류가 발생하였습니다.", Constants.PromptOk); }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    private static async Task<PostImageRendererResources> GetResourcesAsync()
    {
        if (s_resources != null) return s_resources;

        await s_resourceSemaphore.WaitAsync();
        try
        {
            if (s_resources != null) return s_resources;

            var fontBytes = await TryLoadPackageFileAsync("PretendardVariable.ttf");
            var profileBytes = await TryLoadPackageFileAsync(Constants.DefaultProfileImageFileName);
            s_resources = new PostImageRendererResources
            {
                FontBytes = fontBytes,
                DefaultProfileImageBytes = profileBytes
            };
            return s_resources;
        }
        finally { s_resourceSemaphore.Release(); }
    }

    private static async Task<byte[]> TryLoadPackageFileAsync(string fileName)
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch { return null; }
    }
}
