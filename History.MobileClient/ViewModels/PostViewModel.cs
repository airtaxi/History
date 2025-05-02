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

public partial class PostViewModel(PostResponseDto post, bool wrapMedias) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsRepost))]
    [NotifyPropertyChangedFor(nameof(Contents))]
    [NotifyPropertyChangedFor(nameof(ParentPost))]
    [NotifyPropertyChangedFor(nameof(HasBeenSimpleReposted))]
    [NotifyPropertyChangedFor(nameof(HasComments))]
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
    public PostViewModel ParentPost => new(Post.ParentPost, true);
    public bool HasBeenSimpleReposted => Post.HasBeenSimpleReposted;

    public List<BaseContent> Contents => Post.Contents;
    public List<IContentViewModel> ContentViewModels => Utils.GenerateContentViewModels(Contents, wrapMedias);

    public bool HasNoComments => Post.CommentsCount == 0;
    public bool HasComments => Post.CommentsCount > 0;
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
