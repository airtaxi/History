using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
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
using History.MobileClient.Helpers;
using History.MobileClient.Messages;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.ViewModels;

public partial class HistoryPostViewModel : BasePostViewModel
{
    [ObservableProperty]
    public partial PostResponseDto Post { get; private set; }

    [ObservableProperty]
    public partial UserResponseDto User { get; private set; }

    public HistoryPostViewModel(PostResponseDto post, PostType postType, bool isParentPost = false) : base(postType, isParentPost)
    {
        RepostCountPrefix = "리포스트 ";
        try
        {
            UpdatePost(post ?? throw new Exception("[HistoryPostViewModel] POST IS NULL"));

            WeakReferenceMessenger.Default.Register<ValueChangedMessage<PostResponseDto>>(this, OnPostChangedMessageReceived);
            WeakReferenceMessenger.Default.Register<ValueChangedMessage<UserResponseDto>>(this, OnUserChangedMessageReceived);
            WeakReferenceMessenger.Default.Register<ValueDeletedMessage<CommentResponseDto>>(this, OnCommentDeletedMessageReceived);
            WeakReferenceMessenger.Default.Register<NotificationPostReadMessage>(this, OnNotificationPostReadMessage);
        }
        catch (Exception exception) { App.Page.DisplayAlertAsync("오류", $"{exception.Message}\n{exception.StackTrace}", Constants.PromptOk); }
    }

    private void OnNotificationPostReadMessage(object _, NotificationPostReadMessage message)
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
            ParentPost = post.ParentPost != null ? new HistoryPostViewModel(post.ParentPost, PostType, true) : null;
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
            Interactions = [.. post.PostReactions.Select(x => new HistoryInteractionViewModel(x))
                .Concat(post.SharedAndRepostedUsers.Where(x => !x.IsRepost).Select(x => new HistoryInteractionViewModel(x, true)))
                .Concat(post.SharedAndRepostedUsers.Where(x => x.IsRepost).Select(x => new HistoryInteractionViewModel(x, false)))
                .OrderByDescending(x => x.CreatedAt)];

            Reaction = Interactions.FirstOrDefault(r => r is HistoryInteractionViewModel historyInteraction && historyInteraction.User.UserId == CommonShared.UserId && r.ReactionType != null);
            ReactionGlyph = Reaction?.Glyph ?? Solid.Heart;
            ReactionFontFamily = Reaction != null ? "FASolid" : "FARegular";
            ReactionColor = Reaction?.Color ?? (Utils.GetGlobalAppTheme() == AppTheme.Dark ? Colors.White : Colors.Black);

            Comments = [.. post.Comments.Select(c => new HistoryCommentViewModel(c, post.User.UserId == CommonShared.UserId, PostType, this)).OrderBy(x => x.CreatedAt)];
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
            IsNotificationsMuted = post.IsNotificationsMuted;

            var thumbnailUrl = Utils.GenerateThumbnailUrlFromPost(post);
            PreviewThumbnail = thumbnailUrl != null ? new ImageViewModel(thumbnailUrl)
            {
                Aspect = Aspect.AspectFill,
                HorizontalContentOptions = LayoutOptions.Fill,
                VerticalContentOptions = LayoutOptions.Fill
            } : null;
            PreviewThumbnailVisible = thumbnailUrl != null;

            // Assign Post and User last so all derived properties are already up-to-date.
            Post = post;
            User = newUser;
        }
        catch (ObjectDisposedException) { } // The view is disposed. this view model also will be removed on next GC
        catch (Exception) { } // Ignore any exceptions during update, as the view might be in the foreground.
    }

    private void OnPostChangedMessageReceived(object _, ValueChangedMessage<PostResponseDto> message)
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
            ? (user.UsesAnimatedProfileMedia ? new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true } : new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName))
            : null;
    }

    private void OnUserChangedMessageReceived(object _, ValueChangedMessage<UserResponseDto> message)
    {
        if (message.Value.UserId != User.UserId) return;

        var newUser = message.Value;
        UpdateUserDependentProperties(newUser);
        User = newUser;
    }

    private void OnCommentDeletedMessageReceived(object recipient, ValueDeletedMessage<CommentResponseDto> message)
    {
        var viewModel = Comments.OfType<HistoryCommentViewModel>().FirstOrDefault(c => c.Comment.Id == message.Value.Id);
        if (viewModel == null) return;

        var removedCount = Post.Comments.RemoveAll(x => x.Id == viewModel.Comment.Id);
        Post.CommentsCount -= removedCount;

        Comments.Remove(viewModel);
        LatestComment = Comments.LastOrDefault();
        CommentsCount = Post.CommentsCount;
        HasComments = CommentsCount > 0;
        HasNoComments = CommentsCount == 0;
    }

    public override async Task DisplayActionSheetAsync(bool popModal)
    {
        var options = new List<string>();

        if (PostType != PostType.Unwrapped)
        {
            var isReposted = Post.SharedAndRepostedUsers.Any(x => x.IsRepost && x.User.UserId == CommonShared.UserId);
            options.AddRange(["게시글 공유", isReposted ? "리포스트 해제" : "리포스트"]);
        }

        if (Post.IsBookmarked) options.Add("관심글 삭제");
        else options.Add("관심글로 저장");

        if (User.UserId != CommonShared.UserId)
        {
            options.Add(IsNotificationsMuted ? "이 글 알림 받기" : "이 글 알림 안받기");
            options.Add("이 글 숨기기");
        }

        if (User.UserId == CommonShared.UserId) options.AddRange(["공개범위 설정", "게시글 수정", "게시글 삭제", "프로필에 고정", "게시글 홍보"]);
        else if (CommonShared.MyRank >= Rank.Moderator) options.AddRange("게시글 삭제");
        else options.AddRange("게시글 신고");

        options.Add("게시글 URL 복사");
        options.Add("게시글 이미지로 저장");
        options.Add("게시글 본문만 이미지로 저장");

        var action = await App.Page.DisplayActionSheetAsync("게시물 옵션", Constants.PromptCancel, null, [.. options]);
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
            var rawNewDiscoveryOption = await App.Page.DisplayActionSheetAsync("공개범위 설정", Constants.PromptCancel, null, [.. discoveryOptions]);
            if (rawNewDiscoveryOption == null || rawNewDiscoveryOption == Constants.PromptCancel) return;

            var newDiscoveryOption = DiscoveryOptionExtensions.FromDisplayString(rawNewDiscoveryOption);
            if (newDiscoveryOption == Post.DiscoveryOption)
            {
                await App.Page.DisplayAlertAsync("안내", "이미 선택된 공개범위입니다.", Constants.PromptOk);
                return;
            }

            var result = await App.ExecuteRequestAsync(new ChangeDiscoveryOption(Post.Id, newDiscoveryOption, null));
            if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        }
        else if (action == "프로필에 고정")
        {
            var pin = await App.Page.DisplayAlertAsync("안내", "프로필에 이 게시글을 고정하시겠습니까? 기존에 고정된 게시글은 해제됩니다. 또한, 고정된 게시글을 다시 고정하는 경우, 고정이 해제됩니다.", Constants.PromptOk, Constants.PromptCancel);
            if (!pin) return;

            var result = await App.ExecuteRequestAsync(new UpdatePinnedPost(Post.Id));
            if (result.IsSuccess)
            {
                await App.Page.DisplayAlertAsync("안내", "게시글 고정(해제) 요청이 성공적으로 전송되었습니다.", Constants.PromptOk);
                WeakReferenceMessenger.Default.Send(new PostPinnedMessage());
            }
        }
        else if (action == "게시글 공유") await HandleShareAsync();
        else if (action.StartsWith("리포스트")) await HandleRepostAsync();
        else if (action == "게시글 신고")
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();

            var rawReportType = await App.Page.DisplayActionSheetAsync("신고 카테고리", Constants.PromptCancel, null, reportTypes);
            if (rawReportType == null || rawReportType == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(rawReportType);

            var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
            {
                Type = reportType,
                Target = ReportTarget.Post,
                AssociatedId = Post.Id
            }));

            if (result.IsSuccess) await App.Page.DisplayAlertAsync("안내", "게시글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다.", Constants.PromptOk);
        }
        else if (action == "게시글 홍보")
        {
            var shouldWritePublicPost = await App.Page.DisplayAlertAsync("안내", "게시글을 홍보하면 '발견' 탭에서 모든 사용자에게 노출됩니다. 단, 홍보는 24시간에 한 번만 가능합니다.", Constants.PromptOk, Constants.PromptCancel);
            if (!shouldWritePublicPost) return;

            var success = await App.ExecuteRequestAsync(new WritePublicPost(Post.Id));
            if (success.IsSuccess)
            {
                PublicPostsPage.ShouldRefresh = true;
                await App.Page.DisplayAlertAsync("안내", "게시글 홍보가 성공적으로 전송되었습니다. 발견탭에서 확인할 수 있습니다.", Constants.PromptOk);
            }
        }
        else if (action == "관심글로 저장") await HandleBookmarkAsync();
        else if (action == "관심글 삭제") await HandleUnbookmarkAsync();
        else if (action is "이 글 알림 받기" or "이 글 알림 안받기") await HandleMuteNotificationsAsync();
        else if (action == "이 글 숨기기") await HandleHidePostAsync();
        else if (action == "게시글 URL 복사")
        {
            await Clipboard.SetTextAsync($"https://historyweb.cc/post/{Post.Id}");
            await Toast.Make("게시글 URL이 클립보드에 복사되었습니다.").Show();
        }
        else if (action == "게시글 이미지로 저장")
        {
            var confirm = await App.Page.DisplayAlertAsync("게시글 이미지로 저장", "이 게시글을 이미지로 저장하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (!confirm) return;

            var includeComments = await ConfirmIncludeCommentsAsync();
            await App.ExecuteWithLoadingAsync(async () => await PostImageRendererHelper.SaveAsync(Post.Contents, this, includeComments ? Comments : null));
        }
        else if (action == "게시글 본문만 이미지로 저장")
        {
            // Pass null for the post so the profile header (profile image, nickname, timestamp) is omitted.
            await App.ExecuteWithLoadingAsync(async () => await PostImageRendererHelper.SaveAsync(Post.Contents, null, null));
        }
        else await App.Page.DisplayAlertAsync("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
    }

    private async Task<bool> ConfirmIncludeCommentsAsync()
    {
        if (Comments.Count == 0) return false;
        return await App.Page.DisplayAlertAsync("게시글 이미지로 저장", $"댓글 {Comments.Count}개를 포함해서 저장하시겠습니까?", "포함", "미포함");
    }

    private async Task HandleBookmarkAsync()
    {
        var result = await App.ExecuteRequestAsync(new BookmarkPost(Post.Id));
        if (result.IsSuccess)
        {
            await App.Page.DisplayAlertAsync("안내", "관심글로 저장되었습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
    }

    private async Task HandleUnbookmarkAsync()
    {
        var result = await App.ExecuteRequestAsync(new UnbookmarkPost(Post.Id));
        if (result.IsSuccess)
        {
            WeakReferenceMessenger.Default.Send(new PostUnbookmarkedMessage(Post.Id));
            await App.Page.DisplayAlertAsync("안내", "관심글에서 삭제되었습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
    }

    private async Task HandleHidePostAsync()
    {
        var confirm = await App.Page.DisplayAlertAsync("이 글 숨기기", "이 글을 숨기면 타임라인에서 더 이상 보이지 않습니다. 이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new IgnorePost(Post.Id));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
    }

    public override async Task HandleMuteNotificationsAsync()
    {
        var isMuting = !IsNotificationsMuted;
        var confirm = await App.Page.DisplayAlertAsync("알림 설정", isMuting ? "이 글의 알림을 끄시겠습니까?" : "이 글의 알림을 다시 받으시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        var result = isMuting
            ? await App.ExecuteRequestAsync(new MuteNotifications(Post.Id))
            : await App.ExecuteRequestAsync(new UnmuteNotifications(Post.Id));
        if (result.IsFailure) return;

        await RefreshAsync();
    }

    public override async Task<Result> RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetPost(Post.Id));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        return result;
    }

    public override async Task DeleteAsync(bool popModal)
    {
        if (User.UserId != CommonShared.UserId)
        {
            if (CommonShared.MyRank < Rank.Moderator)
            {
                await App.Page.DisplayAlertAsync("권한 부족", "게시글을 삭제할 권한이 없습니다.", Constants.PromptOk);
                return;
            }

            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await App.Page.DisplayActionSheetAsync("제재 카테고리 선택", Constants.PromptCancel, null, reportTypes);
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
            var confirm = await App.Page.DisplayAlertAsync("게시글 삭제", "정말로 게시글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
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

    public override async Task HandleTapAsync()
    {
        var result = await RefreshAsync();
        if (result.IsFailure) return;

        var newViewModel = new HistoryPostViewModel(Post, PostType.Unwrapped);
        var postPage = new PostPage(newViewModel);
        await App.PushAsync(postPage);
    }

    public override async Task HandleProfileTapAsync()
    {
        var profilePage = new BlazorUserPage(Post.User.UserId);
        await App.PushAsync(profilePage);
    }

    public override async Task HandleMoreTapAsync() => await DisplayActionSheetAsync(false);

    public override async Task HandleReactionAsync()
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
        var rawReaction = await App.Page.DisplayActionSheetAsync("느낌 달기", Constants.PromptCancel, null, [.. Enum.GetValues<ReactionType>().Select(x => x.ToDisplayString())]);
        if (rawReaction == null || rawReaction == Constants.PromptCancel) return;

        var reaction = ReactionTypeExtensions.FromDisplayString(rawReaction);

        await App.ExecuteRequestAsync(new HandlePostReaction(Post.Id, reaction));
        await RefreshAsync();
    }

    public override async Task HandleShareAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await App.Page.DisplayAlertAsync("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 공유할 수 없습니다.", Constants.PromptOk);
            return;
        }
        else if (Post.DisallowShare)
        {
            await App.Page.DisplayAlertAsync("안내", "이 게시글은 작성자가 공유를 허용하지 않은 관계로 공유할 수 없습니다.", Constants.PromptOk);
            return;
        }

        var page = new EditPostPage(Post, true);
        await App.PushAsync(page);
    }

    public override async Task HandleRepostAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await App.Page.DisplayAlertAsync("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 리포스트할 수 없습니다.", Constants.PromptOk);
            return;
        }

        var result = await App.ExecuteRequestAsync(new HandleRepost(Post.Id));
        if (result.IsFailure) return;

        var post = result.Value;
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post));
    }

    public override async Task HandleReactionTapAsync()
    {
        var page = new InteractionsPage(Interactions.Where(x => x.Type == InteractionType.Reaction).Select(x => new HistoryFriendshipViewModel(((HistoryInteractionViewModel)x).User, (HistoryInteractionViewModel)x)), InteractionType.Reaction);
#if IOS
        await App.PushAsync(page);
#else
        await App.PushAsync(page);
#endif
    }

    public override async Task HandleSharedTapAsync()
    {
        var page = new InteractionsPage(Interactions.Where(x => x.Type == InteractionType.Share).Select(x => new HistoryFriendshipViewModel(((HistoryInteractionViewModel)x).User, (HistoryInteractionViewModel)x)), InteractionType.Share);
#if IOS
        await App.PushAsync(page);
#else
        await App.PushAsync(page);
#endif
    }

    public override async Task HandleRepostTapAsync()
    {
        var page = new InteractionsPage(Interactions.Where(x => x.Type == InteractionType.Repost).Select(x => new HistoryFriendshipViewModel(((HistoryInteractionViewModel)x).User, (HistoryInteractionViewModel)x)), InteractionType.Repost);
#if IOS
        await App.PushAsync(page);
#else
        await App.PushAsync(page);
#endif
    }

    public override async Task HandleLoadMoreComments()
    {
        var oldestViewModel = Comments.OfType<HistoryCommentViewModel>().FirstOrDefault();
        if (oldestViewModel == null) return;

        var commentsResult = await App.ExecuteRequestAsync(new GetCommentsByPostId(Post.Id, oldestViewModel.Comment.Id, 20));
        if (commentsResult.IsSuccess)
        {
            var comments = commentsResult.Value;
            var commentViewModels = comments.Select(x => new HistoryCommentViewModel(x, Post.User.UserId == CommonShared.UserId, PostType, this));
            foreach (var commentViewModel in commentViewModels) Comments.Insert(0, commentViewModel);
            HasMoreComments = Post.CommentsCount > Comments.Count;
        }
    }
}
