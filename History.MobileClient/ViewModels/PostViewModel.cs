using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class PostViewModel : ObservableObject
{
    private readonly bool _wrapMedias;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsRepost))]
    [NotifyPropertyChangedFor(nameof(Contents))]
    [NotifyPropertyChangedFor(nameof(ParentPost))]
    [NotifyPropertyChangedFor(nameof(HasBeenSimpleReposted))]
    [NotifyPropertyChangedFor(nameof(HasNoComments))]
    [NotifyPropertyChangedFor(nameof(HasComments))]
    [NotifyPropertyChangedFor(nameof(CommentsCount))]
    [NotifyPropertyChangedFor(nameof(Comments))]
    [NotifyPropertyChangedFor(nameof(HasPostReactions))]
    [NotifyPropertyChangedFor(nameof(PostReactionsCount))]
    [NotifyPropertyChangedFor(nameof(PostReactions))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(ModifiedAt))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    public partial PostResponseDto Post { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotWideMode))]
    public partial bool IsWideMode { get; set; }
    public bool IsNotWideMode => !IsWideMode;

    public string Nickname => Post.User.Nickname;
    public IMediaViewModel ProfileMedia => Post.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(Post.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(Post.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public bool IsRepost => Post.IsRepost;
    public PostViewModel ParentPost => new(Post.ParentPost, true);
    public bool HasBeenSimpleReposted => Post.HasBeenSimpleReposted;

    public List<IContentViewModel> Contents => Utils.GenerateContentViewModels(Post.Contents, _wrapMedias);

    public bool HasNoComments => Post.CommentsCount == 0;
    public bool HasComments => Post.CommentsCount > 0;
    public int CommentsCount => Post.CommentsCount;

    [ObservableProperty]
    public partial ObservableCollection<CommentViewModel> Comments { get; private set; }

    public bool HasPostReactions => Post.PostReactions.Count > 0;
    public int PostReactionsCount => Post.PostReactions.Count;
    public List<PostReactionViewModel> PostReactions => [.. Post.PostReactions.Select(r => new PostReactionViewModel(r))];

    public DateTime CreatedAt => Post.CreatedAt;
    public DateTime? ModifiedAt => Post.ModifiedAt;

    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

    public PostViewModel(PostResponseDto post, bool wrapMedias)
    {
        _wrapMedias = wrapMedias;

        Post = post;
        Comments = [.. Post.Comments.Select(c => new CommentViewModel(c))];

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<PostResponseDto>>(this, OnPostValueChangedMessageReceived);
    }

    private void OnPostValueChangedMessageReceived(object sender, ValueChangedMessage<PostResponseDto> message)
    {
        if (message.Value.Id != Post.Id) return;

        Post = message.Value;
        Comments = [.. Post.Comments.Select(c => new CommentViewModel(c))];
    }

    public async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetPost(Post.Id));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
    }
}
