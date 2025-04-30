using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class PostViewModel(PostResponseDto post, bool isTimeline) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsRepost))]
    [NotifyPropertyChangedFor(nameof(Contents))]
    [NotifyPropertyChangedFor(nameof(ParentPost))]
    [NotifyPropertyChangedFor(nameof(HasBeenSimpleReposted))]
    [NotifyPropertyChangedFor(nameof(CommentsCount))]
    [NotifyPropertyChangedFor(nameof(Comments))]
    [NotifyPropertyChangedFor(nameof(ContentViewModels))]
    [NotifyPropertyChangedFor(nameof(PostReactions))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(ModifiedAt))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    public partial PostResponseDto Post { get; set; } = post;

    public string Nickname => Post.User.Nickname;
    public IMediaViewModel ProfileMedia => Post.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(Post.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(Post.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public bool IsRepost => Post.IsRepost;
    public PostViewModel ParentPost => new(Post.ParentPost, isTimeline);
    public bool HasBeenSimpleReposted => Post.HasBeenSimpleReposted;

    public List<BaseContent> Contents => Post.Contents;
    public List<IContentViewModel> ContentViewModels
    {
        get
        {
            var contentViewModels = new List<IContentViewModel>();

            var mediaContents = new List<MediaContent>();
            var allMediaContents = Contents.OfType<MediaContent>();
            void FlushMediaContents()
            {
                if (mediaContents.Count > 0)
                {
                    contentViewModels.Add(new MediaContentsViewModel(mediaContents, allMediaContents));
                    mediaContents = [];
                }
            }

            var textAndProfileContents = new List<BaseContent>();
            void FlushTextAndProfileContents()
            {
                if (textAndProfileContents.Count > 0)
                {
                    contentViewModels.Add(new TextAndProfileContentsViewModel(textAndProfileContents));
                    textAndProfileContents = [];
                }
            }

            // Fill contentViewModels with contents
            foreach (var content in Contents)
            {
                if (content is TextContent or ProfileContent)
                {
                    FlushMediaContents();
                    textAndProfileContents.Add(content);
                }
                else if (content is StickerContent stickerContent)
                {
                    FlushMediaContents();
                    FlushTextAndProfileContents();
                    contentViewModels.Add(new StickerContentViewModel(stickerContent));
                }
                else if (content is MediaContent mediaContent)
                {
                    if (isTimeline)
                    {
                        FlushTextAndProfileContents();
                        mediaContents.Add(mediaContent);
                    }
                    else contentViewModels.Add(new MediaContentViewModel(mediaContent, allMediaContents));
                }
            }

            // Flush remaining contents
            FlushTextAndProfileContents();
            FlushMediaContents();

            return contentViewModels;
        }
    }

    public int CommentsCount => Post.CommentsCount;
    public List<CommentViewModel> Comments => [.. Post.Comments.Select(c => new CommentViewModel(c))];

    public List<PostReactionViewModel> PostReactions => [.. Post.PostReactions.Select(r => new PostReactionViewModel(r))];

    public DateTime CreatedAt => Post.CreatedAt;
    public DateTime? ModifiedAt => Post.ModifiedAt;

    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

    public async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetPost(Post.Id));
        if (result.IsSuccess) Post = result.Value;
    }
}
