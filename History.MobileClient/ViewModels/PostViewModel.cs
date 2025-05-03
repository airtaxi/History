using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
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
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    public partial UserResponseDto User { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotWideMode))]
    public partial bool IsWideMode { get; set; }
    public bool IsNotWideMode => !IsWideMode;

    public string Nickname => User.Nickname;
    public IMediaViewModel ProfileMedia => User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

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
        User = post.User;
        Comments = [.. Post.Comments.Select(c => new CommentViewModel(c))];

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<PostResponseDto>>(this, OnPostChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<UserResponseDto>>(this, OnUserChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<CommentResponseDto>>(this, OnCommentChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<CommentResponseDto>>(this, OnCommentDeletedMessageReceived);
    }

    private void OnPostChangedMessageReceived(object sender, ValueChangedMessage<PostResponseDto> message)
    {
        if (message.Value.Id != Post.Id) return;

        Post = message.Value;
        Comments = [.. Post.Comments.Select(c => new CommentViewModel(c))];
    }

    private void OnUserChangedMessageReceived(object recipient, ValueChangedMessage<UserResponseDto> message)
    {
        if (message.Value.UserId != User.UserId) return;
        User = message.Value;
    }

    private void OnCommentDeletedMessageReceived(object recipient, ValueDeletedMessage<CommentResponseDto> message)
    {
        var viewModel = Comments.FirstOrDefault(c => c.Comment.Id == message.Value.Id);
        if (viewModel == null) return;

        Comments.Remove(viewModel);
    }

    private void OnCommentChangedMessageReceived(object recipient, ValueChangedMessage<CommentResponseDto> message)
    {
        var viewModel = Comments.FirstOrDefault(c => c.Comment.Id == message.Value.Id);
        if (viewModel == null) return;

        viewModel.Comment = message.Value;
    }


    public async Task DisplayActionSheet(bool popModal)
    {
        var options = new List<string>() { "관심글로 저장", "이 글 알림 끄기" };
        if (User.UserId == Shared.UserId) options.AddRange(["공개범위 설정", "게시글 수정", "게시글 삭제"]);
        else options.AddRange("게시글 신고");

        var result = await App.Page.DisplayActionSheet("게시물 옵션", "취소", null, [.. options]);

        if (result == null) return;

        if (result == "게시글 삭제") await DeleteAsync(popModal);
        else await App.Page.DisplayAlert("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
    }

    public async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetPost(Post.Id));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
    }

    public async Task DeleteAsync(bool popModal)
    {
        var confirm = await App.Page.DisplayAlert("게시글 삭제", "정말로 게시글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (confirm)
        {
            var deleteResult = await App.ExecuteRequestAsync(new DeletePost(Post.Id));
            if (deleteResult.IsSuccess)
            {
                WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
                if (popModal) await App.PopModalAsync();
            }
        }
    }
}
