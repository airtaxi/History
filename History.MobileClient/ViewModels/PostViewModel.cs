using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.Api.Moderation;
using History.Commons.Api.Post;
using History.Commons.Api.Report;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using System.Collections.ObjectModel;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.ViewModels;

public partial class PostViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsModerator))]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    [NotifyPropertyChangedFor(nameof(DiscoveryOptionGlyph))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(IsRepost))]
    [NotifyPropertyChangedFor(nameof(Contents))]
    [NotifyPropertyChangedFor(nameof(ParentPost))]
    [NotifyPropertyChangedFor(nameof(IsShare))]
    [NotifyPropertyChangedFor(nameof(HasRepostedUsers))]
    [NotifyPropertyChangedFor(nameof(RepostedUsersCount))]
    [NotifyPropertyChangedFor(nameof(HasSharedUsers))]
    [NotifyPropertyChangedFor(nameof(SharedUsersCount))]
    [NotifyPropertyChangedFor(nameof(HasNoComments))]
    [NotifyPropertyChangedFor(nameof(HasComments))]
    [NotifyPropertyChangedFor(nameof(CommentsCount))]
    [NotifyPropertyChangedFor(nameof(LatestComment))]
    [NotifyPropertyChangedFor(nameof(HasInteractions))]
    [NotifyPropertyChangedFor(nameof(HasReactions))]
    [NotifyPropertyChangedFor(nameof(HasSharedUsers))]
    [NotifyPropertyChangedFor(nameof(ReactionsCount))]
    [NotifyPropertyChangedFor(nameof(Interactions))]
    [NotifyPropertyChangedFor(nameof(Reaction))]
    [NotifyPropertyChangedFor(nameof(ReactionGlyph))]
    [NotifyPropertyChangedFor(nameof(ReactionFontFamily))]
    [NotifyPropertyChangedFor(nameof(ReactionColor))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(ModifiedAt))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    public partial PostResponseDto Post { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    public partial UserResponseDto User { get; private set; }

    public string Nickname => User.Nickname;
    public bool IsModerator => User.Rank == Rank.Moderator;
    public bool IsAdmin => User.Rank == Rank.Admin;
    public IMediaViewModel ProfileMedia => User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public string DiscoveryOptionGlyph => Post.DiscoveryOption switch
    {
        DiscoveryOption.OnlyMe => Solid.Lock,
        DiscoveryOption.SelectedUsers => Solid.UserPlus,
        DiscoveryOption.UnselectedUsers => Solid.UserMinus,
        DiscoveryOption.Friends => Solid.Users,
        DiscoveryOption.FriendsOfFriends => Solid.UsersBetweenLines,
        DiscoveryOption.Everyone => Solid.Globe,
        _ => Solid.Question
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotWideMode))]
    public partial bool IsWideMode { get; set; }
    public bool IsNotWideMode => !IsWideMode;

    public List<IContentViewModel> Contents => Utils.GenerateContentViewModels(Post.Contents, IsTimeline, IsParentPost);

    public bool HasInteractions => Post.PostReactions.Count > 0 || Post.SharedAndRepostedUsers.Count > 0;

    public bool IsRepost => Post.IsRepost;
    public PostViewModel ParentPost => Post.ParentPost != null ? new(Post.ParentPost, IsTimeline, true) : null;
    public bool IsShare => Post.ParentPost != null && !IsRepost;

    public bool HasRepostedUsers => Post.SharedAndRepostedUsers.Any(x => x.IsRepost);
    public int RepostedUsersCount => Post.SharedAndRepostedUsers.Count(x => x.IsRepost);

    public bool HasSharedUsers => Post.SharedAndRepostedUsers.Any(x => !x.IsRepost);
    public int SharedUsersCount => Post.SharedAndRepostedUsers.Count(x => !x.IsRepost);

    public bool HasReactions => Post.PostReactions.Count > 0;
    public int ReactionsCount => Post.PostReactions.Count;
    public List<PostInteractionViewModel> Interactions
    {
        get
        {
            var reactions = Post.PostReactions.Select(x => new PostInteractionViewModel(x));
            var shared = Post.SharedAndRepostedUsers.Where(x => !x.IsRepost).Select(x => new PostInteractionViewModel(x, true));
            var reposted = Post.SharedAndRepostedUsers.Where(x => x.IsRepost).Select(x => new PostInteractionViewModel(x, false));

            var result = reactions.Concat(shared).Concat(reposted).OrderByDescending(x => x.CreatedAt).ToList();
            return result;
        }
    }
    public bool IsTimeline { get; }
    public bool IsParentPost { get; }

    public PostInteractionViewModel Reaction => Interactions.FirstOrDefault(r => r.User.UserId == Shared.UserId && r.ReactionType != null);
    public string ReactionGlyph => Reaction?.Glyph ?? Solid.Heart;
    public string ReactionFontFamily => Reaction != null ? "FASolid" : "FARegular";
    public Color ReactionColor => Reaction?.Color ?? (Utils.GetGlobalAppTheme() == AppTheme.Dark ? Colors.White : Colors.Black);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LatestComment))]
    [NotifyPropertyChangedFor(nameof(HasComments))]
    [NotifyPropertyChangedFor(nameof(HasNoComments))]
    [NotifyPropertyChangedFor(nameof(CommentsCount))]
    public partial ObservableCollection<CommentViewModel> Comments { get; private set; }

    public CommentViewModel LatestComment => Comments.LastOrDefault();
    public bool HasComments => Post.CommentsCount > 0;
    public bool HasNoComments => Post.CommentsCount == 0;
    public int CommentsCount => Post.CommentsCount;

    [ObservableProperty]
    public partial bool HasMoreComments { get; private set; }

    public DateTime CreatedAt => Post.CreatedAt;
    public DateTime? ModifiedAt => Post.ModifiedAt;

    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

    public PostViewModel(PostResponseDto post, bool isTimeline, bool isParentPost = false)
    {
        try
        {
            IsTimeline = isTimeline;
            IsParentPost = isParentPost;

            if (post == null)
            {
                throw new Exception("[PostViewModel] POST IS NULL");
            }

            Post = post;
            User = post?.User;
            if (User == null) throw new Exception("[PostViewModel] USER IS NULL");
            if (Post.Comments == null) throw new Exception("[PostViewModel] COMMENT IS NULL");
            else
            {
                Comments = [.. Post.Comments.Select(c => new CommentViewModel(c, Post.User.UserId == Shared.UserId)).OrderBy(x => x.CreatedAt)];
                HasMoreComments = Post.CommentsCount > Comments.Count; // If comments count is greater than loaded comments, there are more comments to load
            }

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<PostResponseDto>>(this, OnPostChangedMessageReceived);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<UserResponseDto>>(this, OnUserChangedMessageReceived);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<CommentResponseDto>>(this, OnCommentChangedMessageReceived);
            WeakReferenceMessenger.Default.Register<ValueDeletedMessage<CommentResponseDto>>(this, OnCommentDeletedMessageReceived);
        }
        catch(Exception exception) { App.Page.DisplayAlert("오류", $"{exception.Message}\n{exception.StackTrace}", Constants.PromptOk); }
    }

    private void OnPostChangedMessageReceived(object sender, ValueChangedMessage<PostResponseDto> message)
    {
        if (message.Value.Id != Post.Id) return;

        Post = message.Value;
        User = message.Value.User;

        Comments = [.. Post.Comments.Select(c => new CommentViewModel(c, Post.User.UserId == Shared.UserId)).OrderBy(x => x.CreatedAt)];
        HasMoreComments = Post.CommentsCount > Comments.Count; // If comments count is greater than loaded comments, there are more comments to load
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

        var removedCount = Post.Comments.RemoveAll(x => x.Id == viewModel.Comment.Id);
        Post.CommentsCount -= removedCount;

        Comments.Remove(viewModel);
        OnPropertyChanged(nameof(LatestComment));
        OnPropertyChanged(nameof(HasComments));
        OnPropertyChanged(nameof(HasNoComments));
        OnPropertyChanged(nameof(CommentsCount));
    }

    private void OnCommentChangedMessageReceived(object recipient, ValueChangedMessage<CommentResponseDto> message)
    {
        var viewModel = Comments.FirstOrDefault(c => c.Comment.Id == message.Value.Id);
        if (viewModel == null) return;

        viewModel.Comment = message.Value;
    }

    public async Task DisplayActionSheetAsync(bool popModal)
    {
        var options = new List<string>();

        if (IsTimeline)
        {
            var isReposted = Post.SharedAndRepostedUsers.Any(x => x.IsRepost && x.User.UserId == Shared.UserId);
            options.AddRange(["게시글 공유", isReposted ? "리포스트 해제" : "리포스트"]);
        }

        options.AddRange(["관심글로 저장", "이 글 알림 끄기"]);

        if (User.UserId == Shared.UserId) options.AddRange(["공개범위 설정", "게시글 수정", "게시글 삭제", "프로필에 고정"]);
        else if (Shared.MyRank >= Rank.Moderator) options.AddRange("게시글 삭제");
        else options.AddRange("게시글 신고");

        var action = await App.Page.DisplayActionSheet("게시물 옵션", Constants.PromptCancel, null, [.. options]);

        if (action == null || action == Constants.PromptCancel) return;

        if (action == "게시글 삭제") await DeleteAsync(popModal);
        else if (action == "게시글 수정")
        {
            var editPostPage = new EditPostPage(Post, false);
            await App.PushAsync(editPostPage);
        }
        else if (action == "공개범위 설정")
        {
            var discoveryOptions = Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString());
            var rawNewDiscoveryOption = await App.Page.DisplayActionSheet("공개범위 설정", Constants.PromptCancel, null, [.. discoveryOptions]);
            if (rawNewDiscoveryOption == null || rawNewDiscoveryOption == Constants.PromptCancel) return;

            var newDiscoveryOption = DiscoveryOptionExtensions.FromDisplayString(rawNewDiscoveryOption);
            if (newDiscoveryOption == Post.DiscoveryOption)
            {
                await App.Page.DisplayAlert("안내", "이미 선택된 공개범위입니다.", Constants.PromptOk);
                return;
            }

            var result = await App.ExecuteRequestAsync(new ChangeDiscoveryOption(Post.Id, newDiscoveryOption, null));
            if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        }
        else if (action == "프로필에 고정")
        {
            var pin = await App.Page.DisplayAlert("안내", "프로필에 이 게시글을 고정하시겠습니까? 기존에 고정된 게시글은 해제됩니다. 또한, 고정된 게시글을 다시 고정하는 경우, 고정이 해제됩니다.", Constants.PromptOk, Constants.PromptCancel);
            if (!pin) return;

            var result = await App.ExecuteRequestAsync(new UpdatePinnedPost(Post.Id));
            if (result.IsSuccess)
            {
                await App.Page.DisplayAlert("안내", "게시글 고정(해제) 요청이 성공적으로 전송되었습니다.", Constants.PromptOk);
                WeakReferenceMessenger.Default.Send(new PostPinnedMessage());
            }
        }
        else if (action == "게시글 공유") await HandleShareAsync();
        else if (action.StartsWith("리포스트")) await HandleRepostAsync();
        else if (action == "게시글 신고")
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();

            var rawReportType = await App.Page.DisplayActionSheet("신고 카테고리", Constants.PromptCancel, null, reportTypes);
            if (rawReportType == null || rawReportType == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(rawReportType);

            var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
            {
                Type = reportType,
                Target = ReportTarget.Post,
                AssociatedId = Post.Id
            }));

            if (result.IsSuccess) await App.Page.DisplayAlert("안내", "게시글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다.", Constants.PromptOk);
        }
        else await App.Page.DisplayAlert("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
    }

    public async Task<Result> RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetPost(Post.Id));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        return result;
    }

    public async Task DeleteAsync(bool popModal)
    {
        if (User.UserId != Shared.UserId)
        {
            if (Shared.MyRank < Rank.Moderator)
            {
                await App.Page.DisplayAlert("권한 부족", "댓글을 삭제할 권한이 없습니다.", Constants.PromptOk);
                return;
            }

            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await App.Page.DisplayActionSheet("제재 카테고리 선택", Constants.PromptCancel, null, reportTypes);
            if (action == null || action == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await App.Page.DisplayPromptAsync("게시글 삭제", "게시글을 삭제하는 이유를 입력해주세요.", "삭제", "취소", "삭제 사유");
            if (string.IsNullOrWhiteSpace(reason)) return;

            var deleteResult = await App.ExecuteRequestAsync(new ModerationDeletePost(Post.Id, reason, reportType));
            if (deleteResult.IsSuccess)
            {
                WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
                if (popModal) await App.PopAsync();
            }
        }
        else
        {
            var confirm = await App.Page.DisplayAlert("게시글 삭제", "정말로 게시글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (confirm)
            {
                var deleteResult = await App.ExecuteRequestAsync(new DeletePost(Post.Id));
                if (deleteResult.IsSuccess)
                {
                    WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
                    if (popModal) await App.PopAsync();
                }
            }
        }
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        var result = await RefreshAsync();
        if (result.IsFailure) return;

        var newViewModel = new PostViewModel(Post, false);

#if IOS
        WeakReferenceMessenger.Default.Send(new ApplePostViewModelTapMessage(this));

#endif
        var postPage = new PostPage(newViewModel);
        await App.PushAsync(postPage);
    }

    [RelayCommand]
    public async Task HandleProfileTapAsync()
    {
        var profilePage = new UserPage(Post.User.UserId);
        await App.PushAsync(profilePage);
    }

    [RelayCommand]
    public async Task HandleMoreTapAsync()
    {
        await DisplayActionSheetAsync(false);
    }

    [RelayCommand]
    public async Task HandleReactionAsync()
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

        // Delete reaction
        if (Reaction != null)
        {
            await App.ExecuteRequestAsync(new HandlePostReaction(Post.Id, Reaction.ReactionType.Value));
            await RefreshAsync();
            return;
        }

        // Add reaction
        var rawReaction = await App.Page.DisplayActionSheet("느낌 달기", Constants.PromptCancel, null, [.. Enum.GetValues<PostReactionType>().Select(x => x.ToDisplayString())]);
        if (rawReaction == null || rawReaction == Constants.PromptCancel) return;

        var reaction = PostReactionTypeExtensions.FromDisplayString(rawReaction);

        await App.ExecuteRequestAsync(new HandlePostReaction(Post.Id, reaction));
        await RefreshAsync();
    }

    [RelayCommand]
    public async Task HandleShareAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await App.Page.DisplayAlert("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 공유할 수 없습니다.", Constants.PromptOk);
            return;
        }

        var page = new EditPostPage(Post, true);
        await App.PushAsync(page);
    }

    [RelayCommand]
    public async Task HandleRepostAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await App.Page.DisplayAlert("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 리포스트할 수 없습니다.", Constants.PromptOk);
            return;
        }

        var result = await App.ExecuteRequestAsync(new HandleRepost(Post.Id));
        if (result.IsFailure) return;

        var post = result.Value;
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post));
    }

    [RelayCommand]
    public async Task HandleReactionTapAsync()
    {
        var page = new PostInteractionsPage(Interactions.Where(x => x.Type == PostInteractionType.Reaction).Select(x => new FriendshipViewModel(x.User, x)), Enums.PostInteractionType.Reaction);
#if IOS
        await App.PushAsync(page);
#else
        await App.PushAsync(page);
#endif
    }

    [RelayCommand]
    public async Task HandleSharedTapAsync()
    {
        var page = new PostInteractionsPage(Interactions.Where(x => x.Type == PostInteractionType.Share).Select(x => new FriendshipViewModel(x.User, x)), Enums.PostInteractionType.Share);
#if IOS
        await App.PushAsync(page);
#else
        await App.PushAsync(page);
#endif
    }

    [RelayCommand]
    public async Task HandleRepostTapAsync()
    {
        var page = new PostInteractionsPage(Interactions.Where(x => x.Type == PostInteractionType.Repost).Select(x => new FriendshipViewModel(x.User, x)), Enums.PostInteractionType.Repost);
#if IOS
        await App.PushAsync(page);
#else
        await App.PushAsync(page);
#endif
    }

    [RelayCommand]
    private async Task HandleLoadMoreComments()
    {
        var oldestViewModel = Comments.FirstOrDefault();
        if (oldestViewModel == null) return;

        var commentsResult = await App.ExecuteRequestAsync(new GetCommentsByPostId(Post.Id, oldestViewModel.Comment.Id, 20));
        if (commentsResult.IsSuccess)
        {
            var comments = commentsResult.Value;
            var commentViewModels = comments.Select(x => new CommentViewModel(x, User.UserId == Shared.UserId));
            foreach (var commentViewModel in commentViewModels) Comments.Insert(0, commentViewModel);
            HasMoreComments = Post.CommentsCount > Comments.Count;
        }
    }
}
