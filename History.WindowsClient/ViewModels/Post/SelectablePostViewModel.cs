using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.ViewModels.Extras;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels.Post;

// A post item in the bulk management grid: adds selection toggling and the compact cell
// surface (text, timestamp, thumbnail) that the selection template binds to.
public sealed class SelectablePostViewModel : HistoryPostViewModel
{
    public SelectablePostViewModel(PostResponseDto post, BulkPostManagePageViewModel pageViewModel) : base(post, PostType.Timeline, pageViewModel)
    {
        IsSelectable = true;

        PreviewText = PostHelper.GenerateTextPreviewFromPost(post);
        PreviewTimestamp = PostHelper.GenerateFriendlyTimestamp(post.CreatedAt, post.ModifiedAt);

        var thumbnailUrl = PostHelper.GenerateThumbnailUrlFromPost(post);
        if (!string.IsNullOrEmpty(thumbnailUrl)) PreviewThumbnail = new BitmapImage(new Uri(thumbnailUrl));
    }

    // Raised whenever the selection state changes so the owner can refresh its summary.
    public event EventHandler SelectionChanged;

    // The item's own post id: a repost keeps its own id in RepostId while Post holds the origin post.
    public override string PostId => RepostId ?? Post.Id;

    public string PreviewText { get; }
    public string PreviewTimestamp { get; }
    public ImageSource PreviewThumbnail { get; }
    public bool PreviewThumbnailVisible => PreviewThumbnail != null;

    // Tapping a cell toggles its selection instead of opening the post.
    public override Task HandleTapAsync()
    {
        SetSelected(!IsSelected);
        return Task.CompletedTask;
    }

    public void SetSelected(bool isSelected)
    {
        if (IsSelected == isSelected) return;

        IsSelected = isSelected;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyDiscoveryOption(DiscoveryOption discoveryOption) => DiscoveryOptionGlyph = PostHelper.GetDiscoveryOptionGlyph(discoveryOption);
}
