using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Comment;
using History.Commons.Api.Moderation;
using History.Commons.Api.Post;
using History.Commons.Api.Report;
using History.Commons.Api.User;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Uno.DataTypes;
using History.Uno.Enums;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace History.Uno.ViewModels;

public partial class PostViewModel : ObservableObject
{
    [ObservableProperty]
    public partial PostResponseDto Post { get; private set; }

    [ObservableProperty]
    public partial UserResponseDto User { get; private set; }

    // User-dependent properties — set in UpdatePost alongside User assignment.
    [ObservableProperty]
    public partial string Nickname { get; private set; }
    [ObservableProperty]
    public partial bool IsModerator { get; private set; }
    [ObservableProperty]
    public partial bool IsAdmin { get; private set; }
    [ObservableProperty]
    public partial IMediaViewModel ProfileMedia { get; private set; }

    // Post-dependent simple properties — all set in UpdatePost.
    [ObservableProperty]
    public partial string DiscoveryOptionGlyph { get; private set; }

    [ObservableProperty]
    public partial List<IContentViewModel> Contents { get; private set; }

    // Pre-slotted view model for timeline preview — avoids BindableLayout/ItemsRepeater overhead.
    [ObservableProperty]
    public partial TimelineContentsViewModel TimelineContents { get; private set; }

    [ObservableProperty]
    public partial bool HasInteractions { get; private set; }

    [ObservableProperty]
    public partial PostViewModel ParentPost { get; private set; }
    [ObservableProperty]
    public partial bool IsRepost { get; private set; }
    [ObservableProperty]
    public partial bool IsShare { get; private set; }

    [ObservableProperty]
    public partial bool HasRepostedUsers { get; private set; }
    [ObservableProperty]
    public partial int RepostedUsersCount { get; private set; }

    [ObservableProperty]
    public partial bool HasSharedUsers { get; private set; }
    [ObservableProperty]
    public partial int SharedUsersCount { get; private set; }

    [ObservableProperty]
    public partial bool HasReactions { get; private set; }
    [ObservableProperty]
    public partial int ReactionsCount { get; private set; }
    [ObservableProperty]
    public partial List<InteractionViewModel> Interactions { get; private set; }

    [ObservableProperty]
    public partial InteractionViewModel Reaction { get; private set; }
    [ObservableProperty]
    public partial string ReactionGlyph { get; private set; }
    [ObservableProperty]
    public partial SolidColorBrush ReactionBrush { get; private set; }

    [ObservableProperty]
    public partial ObservableCollection<CommentViewModel> Comments { get; private set; }
    [ObservableProperty]
    public partial CommentViewModel LatestComment { get; private set; }
    [ObservableProperty]
    public partial bool HasComments { get; private set; }
    [ObservableProperty]
    public partial bool HasNoComments { get; private set; }
    [ObservableProperty]
    public partial int CommentsCount { get; private set; }

    [ObservableProperty]
    public partial bool HasMoreComments { get; private set; }

    [ObservableProperty]
    public partial DateTime CreatedAt { get; private set; }
    [ObservableProperty]
    public partial DateTime? ModifiedAt { get; private set; }

    [ObservableProperty]
    public partial string TimestampText { get; private set; }

    [ObservableProperty]
    public partial string PreviewText { get; private set; }
    [ObservableProperty]
    public partial string PreviewTimestamp { get; private set; }

    public bool PreviewThumbnailVisible => Utils.GenerateThumbnailUrlFromPost(Post) != null;
    [ObservableProperty]
    public partial bool HasUnreadNotification { get; private set; }
    [ObservableProperty]
    public partial ImageViewModel PreviewThumbnail { get; private set; }

    public PostType PostType { get; }
    public bool IsParentPost { get; }

    public PostViewModel(PostResponseDto post, PostType postType, bool isParentPost = false)
    {
        PostType = postType;
        IsParentPost = isParentPost;
        UpdatePost(post ?? throw new Exception("[PostViewModel] POST IS NULL"));

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<PostResponseDto>>(this, OnPostChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<UserResponseDto>>(this, OnUserChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<CommentResponseDto>>(this, OnCommentChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<ValueDeletedMessage<CommentResponseDto>>(this, OnCommentDeletedMessageReceived);
        WeakReferenceMessenger.Default.Register<NotificationPostReadMessage>(this, OnNotificationPostReadMessage);
    }

    private void OnNotificationPostReadMessage(object recipient, NotificationPostReadMessage message)
    {
        if (Post.Id != message.Value) return;

        Post.HasUnreadNotification = false;
        HasUnreadNotification = false;
    }

    private void UpdatePost(PostResponseDto post)
    {
        try
        {
            // Compute all derived properties from the new post/user before assigning Post/User.
            // This avoids per-binding-read allocations (LINQ, new ImageViewModel, etc.) during scroll.
            var newUser = post?.User;

            // User-dependent
            UpdateUserDependentProperties(newUser);

            // Post-dependent simple
            DiscoveryOptionGlyph = Utils.GetDiscoveryOptionGlyph(post.DiscoveryOption);
            Contents = Utils.GenerateContentViewModels(post.Contents, PostType, IsParentPost, post.Id);
            TimelineContents = new TimelineContentsViewModel(Contents);
            ParentPost = post.ParentPost != null ? new(post.ParentPost, PostType, true) : null;
            var isRepost = post.IsRepost;
            IsRepost = isRepost;
            IsShare = post.ParentPost != null && !isRepost;

            HasRepostedUsers = post.SharedAndRepostedUsers.Any(x => x.IsRepost);
            RepostedUsersCount = post.SharedAndRepostedUsers.Count(x => x.IsRepost);
            HasSharedUsers = post.SharedAndRepostedUsers.Any(x => !x.IsRepost);
            SharedUsersCount = post.SharedAndRepostedUsers.Count(x => !x.IsRepost);

            HasReactions = post.PostReactions.Count > 0;
            ReactionsCount = post.PostReactions.Count;
            HasInteractions = post.PostReactions.Count > 0 || post.SharedAndRepostedUsers.Count > 0;
            Interactions = [.. post.PostReactions.Select(x => new InteractionViewModel(x))
                .Concat(post.SharedAndRepostedUsers.Where(x => !x.IsRepost).Select(x => new InteractionViewModel(x, true)))
                .Concat(post.SharedAndRepostedUsers.Where(x => x.IsRepost).Select(x => new InteractionViewModel(x, false)))
                .OrderByDescending(x => x.CreatedAt)];

            Reaction = Interactions.FirstOrDefault(r => r.User.UserId == Shared.UserId && r.ReactionType != null);
            ReactionGlyph = Reaction?.Glyph ?? "\uEB52"; // HeartFill
            ReactionBrush = Reaction?.Brush ?? new SolidColorBrush(Utils.GetGlobalAppTheme() == Microsoft.UI.Xaml.ApplicationTheme.Dark ? Colors.White : Colors.Black);

            Comments = [.. post.Comments.Select(comment => new CommentViewModel(comment, post.User.UserId == Shared.UserId, PostType, this)).OrderBy(x => x.CreatedAt)];
            LatestComment = Comments.LastOrDefault();
            CommentsCount = post.CommentsCount;
            HasComments = CommentsCount > 0;
            HasNoComments = CommentsCount == 0;
            HasMoreComments = CommentsCount > Comments.Count;

            CreatedAt = post.CreatedAt;
            ModifiedAt = post.ModifiedAt;
            TimestampText = Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

            PreviewText = Utils.GenerateTextPreviewFromPost(post);
            PreviewTimestamp = post.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd");
            HasUnreadNotification = post.HasUnreadNotification;

            var thumbnailUrl = Utils.GenerateThumbnailUrlFromPost(post);
            PreviewThumbnail = thumbnailUrl != null ? new ImageViewModel(thumbnailUrl) { Stretch = Stretch.UniformToFill } : null;

            // Assign Post and User last so all derived properties are already up-to-date.
            Post = post;
            User = newUser;
        }
        catch (ObjectDisposedException) { } // The view is disposed. this view model also will be removed on next GC
        catch (Exception) { } // Ignore any exceptions during update, as the view might be in the foreground.
    }

    private void OnPostChangedMessageReceived(object sender, ValueChangedMessage<PostResponseDto> message)
    {
        if (message.Value.Id != Post.Id) return;

        UpdatePost(message.Value);
    }

    // Shared between UpdatePost and OnUserChangedMessageReceived to keep User-dependent properties in sync.
    private void UpdateUserDependentProperties(UserResponseDto user)
    {
        Nickname = user?.Nickname;
        IsModerator = user?.Rank == Rank.Moderator;
        IsAdmin = user?.Rank == Rank.Admin;
        ProfileMedia = user != null
            ? new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName)
            : null;
    }

    private void OnUserChangedMessageReceived(object recipient, ValueChangedMessage<UserResponseDto> message)
    {
        if (message.Value.UserId != User.UserId) return;

        var newUser = message.Value;
        UpdateUserDependentProperties(newUser);
        User = newUser;
    }

    private void OnCommentDeletedMessageReceived(object recipient, ValueDeletedMessage<CommentResponseDto> message)
    {
        var viewModel = Comments.FirstOrDefault(comment => comment.Comment.Id == message.Value.Id);
        if (viewModel == null) return;

        var removedCount = Post.Comments.RemoveAll(x => x.Id == viewModel.Comment.Id);
        Post.CommentsCount -= removedCount;

        Comments.Remove(viewModel);
        LatestComment = Comments.LastOrDefault();
        CommentsCount = Post.CommentsCount;
        HasComments = CommentsCount > 0;
        HasNoComments = CommentsCount == 0;
    }

    private void OnCommentChangedMessageReceived(object recipient, ValueChangedMessage<CommentResponseDto> message)
    {
        // CommentViewModel already subscribes to ValueChangedMessage<CommentResponseDto> and
        // calls UpdateComment internally. No action needed here.
    }

    public async Task DisplayActionSheetAsync(bool popModal)
    {
        var options = new List<string>();

        if (PostType != PostType.Unwrapped)
        {
            var isReposted = Post.SharedAndRepostedUsers.Any(x => x.IsRepost && x.User.UserId == Shared.UserId);
            options.AddRange(["게시글 공유", isReposted ? "리포스트 해제" : "리포스트"]);
        }

        if (Post.IsBookmarked) options.Add("관심글 삭제");
        else options.Add("관심글로 저장");

        if (User.UserId != Shared.UserId) options.Add("이 글 숨기기");

        if (User.UserId == Shared.UserId) options.AddRange(["공개범위 설정", "게시글 수정", "게시글 삭제", "프로필에 고정", "게시글 홍보"]);
        else if (Shared.MyRank >= Rank.Moderator) options.AddRange("게시글 삭제");
        else options.AddRange("게시글 신고");

        var action = await App.DisplayActionSheetAsync("게시물 옵션", Constants.PromptCancel, null, [.. options]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action == "게시글 삭제") await DeleteAsync(popModal);
        else if (action == "게시글 수정")
        {
            // TODO: Navigate to EditPostPage (migrated in a later phase).
            await App.DisplayAlertAsync("안내", "게시글 수정은 아직 지원되지 않습니다.", Constants.PromptOk);
        }
        else if (action == "공개범위 설정")
        {
            var discoveryOptions = Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString());
            var rawNewDiscoveryOption = await App.DisplayActionSheetAsync("공개범위 설정", Constants.PromptCancel, null, [.. discoveryOptions]);
            if (rawNewDiscoveryOption == null || rawNewDiscoveryOption == Constants.PromptCancel) return;

            var newDiscoveryOption = DiscoveryOptionExtensions.FromDisplayString(rawNewDiscoveryOption);
            if (newDiscoveryOption == Post.DiscoveryOption)
            {
                await App.DisplayAlertAsync("안내", "이미 선택된 공개범위입니다.", Constants.PromptOk);
                return;
            }

            var result = await App.ExecuteRequestAsync(new ChangeDiscoveryOption(Post.Id, newDiscoveryOption, null));
            if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        }
        else if (action == "프로필에 고정")
        {
            var result = await App.DisplayAlertAsync("안내", "프로필에 이 게시글을 고정하시겠습니까? 기존에 고정된 게시글은 해제됩니다. 또한, 고정된 게시글을 다시 고정하는 경우, 고정이 해제됩니다.", Constants.PromptOk, Constants.PromptCancel);
            if (result != ContentDialogResult.Primary) return;

            var updateResult = await App.ExecuteRequestAsync(new UpdatePinnedPost(Post.Id));
            if (updateResult.IsSuccess)
            {
                await App.DisplayAlertAsync("안내", "게시글 고정(해제) 요청이 성공적으로 전송되었습니다.", Constants.PromptOk);
                WeakReferenceMessenger.Default.Send(new PostPinnedMessage());
            }
        }
        else if (action == "게시글 공유") await HandleShareAsync();
        else if (action.StartsWith("리포스트")) await HandleRepostAsync();
        else if (action == "게시글 신고")
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();

            var rawReportType = await App.DisplayActionSheetAsync("신고 카테고리", Constants.PromptCancel, null, reportTypes);
            if (rawReportType == null || rawReportType == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(rawReportType);

            var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
            {
                Type = reportType,
                Target = ReportTarget.Post,
                AssociatedId = Post.Id
            }));

            if (result.IsSuccess) await App.DisplayAlertAsync("안내", "게시글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다.", Constants.PromptOk);
        }
        else if (action == "게시글 홍보")
        {
            var result = await App.DisplayAlertAsync("안내", "게시글을 홍보하면 '발견' 탭에서 모든 사용자에게 노출됩니다. 단, 홍보는 24시간에 한 번만 가능합니다.", Constants.PromptOk, Constants.PromptCancel);
            if (result != ContentDialogResult.Primary) return;

            var success = await App.ExecuteRequestAsync(new WritePublicPost(Post.Id));
            if (success.IsSuccess) await App.DisplayAlertAsync("안내", "게시글 홍보가 성공적으로 전송되었습니다. 발견탭에서 확인할 수 있습니다.", Constants.PromptOk);
        }
        else if (action == "관심글로 저장") await HandleBookmarkAsync();
        else if (action == "관심글 삭제") await HandleUnbookmarkAsync();
        else if (action == "이 글 숨기기") await HandleHidePostAsync();
        else await App.DisplayAlertAsync("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
    }

    private async Task HandleBookmarkAsync()
    {
        var result = await App.ExecuteRequestAsync(new BookmarkPost(Post.Id));
        if (result.IsSuccess)
        {
            await App.DisplayAlertAsync("안내", "관심글로 저장되었습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
    }

    private async Task HandleUnbookmarkAsync()
    {
        var result = await App.ExecuteRequestAsync(new UnbookmarkPost(Post.Id));
        if (result.IsSuccess)
        {
            WeakReferenceMessenger.Default.Send(new PostUnbookmarkedMessage(Post.Id));
            await App.DisplayAlertAsync("안내", "관심글에서 삭제되었습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
    }

    private async Task HandleHidePostAsync()
    {
        var result = await App.DisplayAlertAsync("이 글 숨기기", "이 글을 숨기면 타임라인에서 더 이상 보이지 않습니다. 이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (result != ContentDialogResult.Primary) return;

        var ignoreResult = await App.ExecuteRequestAsync(new IgnorePost(Post.Id));
        if (ignoreResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
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
                await App.DisplayAlertAsync("권한 부족", "게시글을 삭제할 권한이 없습니다.", Constants.PromptOk);
                return;
            }

            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await App.DisplayActionSheetAsync("제재 카테고리 선택", Constants.PromptCancel, null, reportTypes);
            if (action == null || action == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await App.DisplayPromptAsync("게시글 삭제", "게시글을 삭제하는 이유를 입력해주세요.", "삭제", "취소", "삭제 사유");
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
            var result = await App.DisplayAlertAsync("게시글 삭제", "정말로 게시글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (result == ContentDialogResult.Primary)
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

        // TODO: Navigate to PostPage with a fresh unwrapped PostViewModel (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "게시글 상세 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    [RelayCommand]
    public async Task HandleProfileTapAsync() => await App.PushAsync(typeof(Pages.UserPage), Post.User.UserId);

    [RelayCommand]
    public async Task HandleMoreTapAsync() => await DisplayActionSheetAsync(false);

    [RelayCommand]
    public async Task HandleReactionAsync()
    {
        // Delete reaction
        if (Reaction != null)
        {
            await App.ExecuteRequestAsync(new HandlePostReaction(Post.Id, Reaction.ReactionType.Value));
            await RefreshAsync();
            return;
        }

        // Add reaction
        var rawReaction = await App.DisplayActionSheetAsync("느낌 달기", Constants.PromptCancel, null, [.. Enum.GetValues<ReactionType>().Select(x => x.ToDisplayString())]);
        if (rawReaction == null || rawReaction == Constants.PromptCancel) return;

        var reaction = ReactionTypeExtensions.FromDisplayString(rawReaction);

        await App.ExecuteRequestAsync(new HandlePostReaction(Post.Id, reaction));
        await RefreshAsync();
    }

    [RelayCommand]
    public async Task HandleShareAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await App.DisplayAlertAsync("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 공유할 수 없습니다.", Constants.PromptOk);
            return;
        }
        else if (Post.DisallowShare)
        {
            await App.DisplayAlertAsync("안내", "이 게시글은 작성자가 공유를 허용하지 않은 관계로 공유할 수 없습니다.", Constants.PromptOk);
            return;
        }

        // TODO: Navigate to EditPostPage for sharing (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "게시글 공유는 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    [RelayCommand]
    public async Task HandleRepostAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await App.DisplayAlertAsync("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 리포스트할 수 없습니다.", Constants.PromptOk);
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
        // TODO: Navigate to InteractionsPage (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "느낌 목록 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    [RelayCommand]
    public async Task HandleSharedTapAsync()
    {
        // TODO: Navigate to InteractionsPage (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "공유 목록 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    [RelayCommand]
    public async Task HandleRepostTapAsync()
    {
        // TODO: Navigate to InteractionsPage (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "리포스트 목록 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
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
            var commentViewModels = comments.Select(x => new CommentViewModel(x, Post.User.UserId == Shared.UserId, PostType, this));
            foreach (var commentViewModel in commentViewModels) Comments.Insert(0, commentViewModel);
            HasMoreComments = Post.CommentsCount > Comments.Count;
        }
    }
}
