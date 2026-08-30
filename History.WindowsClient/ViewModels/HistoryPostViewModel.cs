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
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;

namespace History.WindowsClient.ViewModels;

// Mirrors the MAUI HistoryPostViewModel. Dialog prompts are requested on the host
// view model; the "..." and reaction menus are populated by PopulateMoreMenuFlyout
// and PopulateReactionMenuFlyout with the MAUI action labels as item Tag values.
public partial class HistoryPostViewModel : BasePostViewModel,
    IRecipient<ValueChangedMessage<PostResponseDto>>,
    IRecipient<ValueChangedMessage<UserResponseDto>>,
    IRecipient<ValueDeletedMessage<CommentResponseDto>>,
    IRecipient<NotificationPostReadMessage>
{
    [ObservableProperty]
    public partial PostResponseDto Post { get; private set; }

    [ObservableProperty]
    public partial UserResponseDto User { get; private set; }

    public HistoryPostViewModel(PostResponseDto post, PostType postType, BaseViewModel hostViewModel, bool isParentPost = false) : base(postType, isParentPost, hostViewModel)
    {
        RepostCountPrefix = "리포스트 ";

        UpdatePost(post ?? throw new Exception("[HistoryPostViewModel] POST IS NULL"));

        WeakReferenceMessenger.Default.Register((IRecipient<ValueChangedMessage<PostResponseDto>>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<ValueChangedMessage<UserResponseDto>>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<ValueDeletedMessage<CommentResponseDto>>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NotificationPostReadMessage>)this);
    }

    public void Receive(ValueChangedMessage<PostResponseDto> message)
    {
        if (message.Value.Id != Post.Id) return;

        UpdatePost(message.Value);
    }

    public void Receive(ValueChangedMessage<UserResponseDto> message)
    {
        if (message.Value.UserId != User.UserId) return;

        var newUser = message.Value;
        UpdateUserDependentProperties(newUser);
        User = newUser;
    }

    public void Receive(ValueDeletedMessage<CommentResponseDto> message)
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

    public void Receive(NotificationPostReadMessage message)
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
            var newUser = post?.User;

            // User-dependent
            UpdateUserDependentProperties(newUser);

            // Post-dependent simple
            DiscoveryOptionGlyph = PostHelper.GetDiscoveryOptionGlyph(post.DiscoveryOption);
            Contents = PostHelper.GenerateContentViewModels(post.Contents, PostType, IsParentPost, post.Id);
            ParentPost = post.ParentPost != null ? new HistoryPostViewModel(post.ParentPost, PostType, HostViewModel, true) : null;
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
            ReactionGlyph = Reaction?.Glyph ?? "\uEB51";
            ReactionBrush = Reaction?.ColorBrush ?? CreateDefaultReactionBrush();

            Comments = [.. post.Comments.Select(c => new HistoryCommentViewModel(c, post.User.UserId == CommonShared.UserId, PostType, this)).OrderBy(x => x.CreatedAt)];
            LatestComment = Comments.LastOrDefault();
            CommentsCount = post.CommentsCount;
            HasComments = CommentsCount > 0;
            HasNoComments = CommentsCount == 0;
            HasMoreComments = CommentsCount > Comments.Count;

            CreatedAt = post.CreatedAt;
            ModifiedAt = post.ModifiedAt;
            TimestampText = PostHelper.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

            HasUnreadNotification = post.HasUnreadNotification;
            IsNotificationsMuted = post.IsNotificationsMuted;

            // Assign Post and User last so all derived properties are already up-to-date.
            Post = post;
            User = newUser;
        }
        catch (Exception) { } // Ignore any exceptions during update, as the view might be in the foreground.
    }

    private static SolidColorBrush CreateDefaultReactionBrush()
    {
        var themeColor = (Windows.UI.Color)Application.Current.Resources["ReverseThemeColor"];
        return new SolidColorBrush(themeColor);
    }

    // Shared between UpdatePost and Receive(ValueChangedMessage<UserResponseDto>) to keep User-dependent properties in sync.
    private void UpdateUserDependentProperties(UserResponseDto user)
    {
        Nickname = user?.Nickname;
        IsModerator = user?.Rank == Rank.Moderator;
        IsAdmin = user?.Rank == Rank.Admin;
        ProfileThumbnailImageSource = CreateProfileImageSource(user);
    }

    private static BitmapImage CreateProfileImageSource(UserResponseDto user) => user?.ProfileThumbnailMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId)));

    // Adds a clickable item that runs the given async action when tapped.
    private static MenuFlyoutItem CreateActionItem(string text, string glyph, Func<Task> action, Windows.UI.Color? iconColor = null)
    {
        var item = new MenuFlyoutItem { Text = text, Tag = action };
        if (iconColor != null) item.Icon = new FontIcon { Glyph = glyph, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(iconColor.Value) };
        else item.Icon = new FontIcon { Glyph = glyph };
        item.Click += async (sender, _) => await ((Func<Task>)((MenuFlyoutItem)sender).Tag)();
        return item;
    }

    public override void PopulateMoreMenuFlyout(MenuFlyout menuFlyout)
    {
        menuFlyout.Items.Clear();

        if (PostType != PostType.Unwrapped)
        {
            var isReposted = Post.SharedAndRepostedUsers.Any(x => x.IsRepost && x.User.UserId == CommonShared.UserId);
            menuFlyout.Items.Add(CreateActionItem("게시글 공유", "\uE72D", HandleShareAsync));
            menuFlyout.Items.Add(CreateActionItem(isReposted ? "리포스트 해제" : "리포스트", "\uE8EB", HandleRepostAsync));
        }

        if (Post.IsBookmarked) menuFlyout.Items.Add(CreateActionItem("관심글 삭제", "\uE8A4", () => HandleBookmarkAsync(false)));
        else menuFlyout.Items.Add(CreateActionItem("관심글로 저장", "\uE8A4", () => HandleBookmarkAsync(true)));

        if (User.UserId != CommonShared.UserId)
        {
            menuFlyout.Items.Add(CreateActionItem(IsNotificationsMuted ? "이 글 알림 받기" : "이 글 알림 안받기", IsNotificationsMuted ? "\uEA8F" : "\uE7ED", HandleMuteNotificationsAsync));
            menuFlyout.Items.Add(CreateActionItem("이 글 숨기기", "\uE7B3", HandleHidePostAsync));
        }

        if (User.UserId == CommonShared.UserId)
        {
            var discoverySubItem = new MenuFlyoutSubItem { Text = "공개범위 설정" };
            foreach (var discoveryOption in Enum.GetValues<DiscoveryOption>())
            {
                var option = discoveryOption;
                discoverySubItem.Items.Add(CreateActionItem(option.ToDisplayString(), PostHelper.GetDiscoveryOptionGlyph(option), () => HandleChangeDiscoveryOptionAsync(option)));
            }
            menuFlyout.Items.Add(discoverySubItem);

            menuFlyout.Items.Add(CreateActionItem("게시글 수정", "\uE70F", HandleEditPostAsync));
            menuFlyout.Items.Add(CreateActionItem("게시글 삭제", "\uE74D", DeleteAsync));
            menuFlyout.Items.Add(CreateActionItem("프로필에 고정", "\uE718", HandlePinPostAsync));
            menuFlyout.Items.Add(CreateActionItem("게시글 홍보", "\uE789", HandlePromotePostAsync));
        }
        else if (CommonShared.MyRank >= Rank.Moderator)
        {
            menuFlyout.Items.Add(CreateActionItem("게시글 삭제", "\uE74D", DeleteAsync));
        }
        else
        {
            var reportSubItem = new MenuFlyoutSubItem { Text = "게시글 신고" };
            foreach (var reportType in Enum.GetValues<ReportType>())
            {
                var reportTypeValue = reportType;
                reportSubItem.Items.Add(CreateActionItem(reportType.ToDisplayString(), "\uE7C1", () => HandleReportAsync(reportTypeValue)));
            }
            menuFlyout.Items.Add(reportSubItem);
        }

        menuFlyout.Items.Add(CreateActionItem("게시글 URL 복사", "\uE71B", HandleCopyUrlAsync));
        menuFlyout.Items.Add(CreateActionItem("게시글 이미지로 저장", "\uEE71", HandleSaveImageAsync));
    }

    public override void PopulateReactionMenuFlyout(MenuFlyout menuFlyout)
    {
        menuFlyout.Items.Clear();

        // Delete reaction
        if (Reaction is HistoryInteractionViewModel historyReaction)
        {
            var reactionTypeValue = historyReaction.ReactionType.Value;
            menuFlyout.Items.Add(CreateActionItem("느낌 취소", "\uEA92", () => HandleReactionTypedAsync(reactionTypeValue)));
            return;
        }

        foreach (var reactionType in Enum.GetValues<ReactionType>())
        {
            var reactionTypeValue = reactionType;
            var (glyph, color) = reactionTypeValue switch
            {
                ReactionType.Like => ("\uEB52", Windows.UI.Color.FromArgb(0xFF, 0xEB, 0x55, 0x27)),
                ReactionType.Awesome => ("\uE735", Windows.UI.Color.FromArgb(0xFF, 0xBB, 0xCC, 0x29)),
                ReactionType.Happy => ("\uED54", Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xC1, 0x00)),
                ReactionType.Sad => ("\uEB42", Windows.UI.Color.FromArgb(0xFF, 0x00, 0x9F, 0xB2)),
                ReactionType.Support => ("\uE945", Windows.UI.Color.FromArgb(0xFF, 0xA0, 0x61, 0xB1)),
                _ => throw new ArgumentOutOfRangeException(nameof(reactionType), reactionType, null),
            };
            menuFlyout.Items.Add(CreateActionItem(reactionType.ToDisplayString(), glyph, () => HandleReactionTypedAsync(reactionTypeValue), color));
        }
    }

    private async Task HandleChangeDiscoveryOptionAsync(DiscoveryOption newDiscoveryOption)
    {
        if (newDiscoveryOption == Post.DiscoveryOption)
        {
            await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "이미 선택된 공개범위입니다."));
            return;
        }

        var result = await App.ExecuteRequestAsync(new ChangeDiscoveryOption(Post.Id, newDiscoveryOption, null));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
    }

    // TODO: Open the post editor once it is implemented.
    private async Task HandleEditPostAsync() => await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "아직 지원하지 않는 기능입니다."));

    private async Task HandlePinPostAsync()
    {
        var pin = await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "프로필에 이 게시글을 고정하시겠습니까? 기존에 고정된 게시글은 해제됩니다. 또한, 고정된 게시글을 다시 고정하는 경우, 고정이 해제됩니다.", "고정", "취소"));
        if (pin != ContentDialogResult.Primary) return;

        var result = await App.ExecuteRequestAsync(new UpdatePinnedPost(Post.Id));
        if (result.IsSuccess)
        {
            await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "게시글 고정(해제) 요청이 성공적으로 전송되었습니다."));
            WeakReferenceMessenger.Default.Send(new PostPinnedMessage());
        }
    }

    private async Task HandlePromotePostAsync()
    {
        var shouldWritePublicPost = await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "게시글을 홍보하면 '발견' 탭에서 모든 사용자에게 노출됩니다. 단, 홍보는 24시간에 한 번만 가능합니다.", "홍보", "취소"));
        if (shouldWritePublicPost != ContentDialogResult.Primary) return;

        var success = await App.ExecuteRequestAsync(new WritePublicPost(Post.Id));
        if (success.IsSuccess) await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "게시글 홍보가 성공적으로 전송되었습니다. 발견탭에서 확인할 수 있습니다."));
    }

    private async Task HandleReportAsync(ReportType reportType)
    {
        var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
        {
            Type = reportType,
            Target = ReportTarget.Post,
            AssociatedId = Post.Id
        }));

        if (result.IsSuccess) await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "게시글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다."));
    }

    private async Task HandleCopyUrlAsync()
    {
        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText($"https://historyweb.cc/post/{Post.Id}");
        Clipboard.SetContent(dataPackage);
        await Task.CompletedTask;
    }

    // TODO: Render the post to an image once the renderer is implemented.
    private async Task HandleSaveImageAsync() => await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "아직 지원하지 않는 기능입니다."));

    private async Task HandleBookmarkAsync(bool bookmark)
    {
        if (bookmark)
        {
            var result = await App.ExecuteRequestAsync(new BookmarkPost(Post.Id));
            if (result.IsSuccess)
            {
                await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "관심글로 저장되었습니다."));
                await RefreshAsync();
            }
        }
        else
        {
            var result = await App.ExecuteRequestAsync(new UnbookmarkPost(Post.Id));
            if (result.IsSuccess)
            {
                WeakReferenceMessenger.Default.Send(new PostUnbookmarkedMessage(Post.Id));
                await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "관심글에서 삭제되었습니다."));
                await RefreshAsync();
            }
        }
    }

    private async Task HandleHidePostAsync()
    {
        var confirm = await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("이 글 숨기기", "이 글을 숨기면 타임라인에서 더 이상 보이지 않습니다. 이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?", "숨기기", "취소"));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await App.ExecuteRequestAsync(new IgnorePost(Post.Id));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
    }

    private async Task HandleReactionTypedAsync(ReactionType reactionType)
    {
        await App.ExecuteRequestAsync(new HandlePostReaction(Post.Id, reactionType));
        await RefreshAsync();
    }

    public override async Task<Result> RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetPost(Post.Id));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        return result;
    }

    public async Task DeleteAsync()
    {
        if (User.UserId != CommonShared.UserId)
        {
            if (CommonShared.MyRank < Rank.Moderator)
            {
                await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("권한 부족", "게시글을 삭제할 권한이 없습니다."));
                return;
            }

            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await MainWindow.Frame.ShowSelectionDialogAsync("제재 카테고리 선택", reportTypes);
            if (action == null) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await MainWindow.Frame.ShowInputDialogAsync(new InputDialogParameters("게시글 삭제", "게시글을 삭제하는 이유를 입력해주세요."));
            if (string.IsNullOrWhiteSpace(reason)) return;

            var deleteResult = await App.ExecuteRequestAsync(new ModerationDeletePost(Post.Id, reason, reportType));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
        }
        else
        {
            var confirm = await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("게시글 삭제", "정말로 게시글을 삭제하시겠습니까?", "삭제", "취소"));
            if (confirm != ContentDialogResult.Primary) return;

            var deleteResult = await App.ExecuteRequestAsync(new DeletePost(Post.Id));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
        }
    }

    public override async Task HandleTapAsync()
    {
        var result = await RefreshAsync();
        if (result.IsFailure) return;

        // TODO: Navigate to the post page once it is implemented.
        await Task.CompletedTask;
    }

    // TODO: Navigate to the user profile page once it is implemented.
    public override async Task HandleProfileTapAsync() => await Task.CompletedTask;

    public override async Task HandleShareAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 공유할 수 없습니다."));
            return;
        }
        else if (Post.DisallowShare)
        {
            await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "이 게시글은 작성자가 공유를 허용하지 않은 관계로 공유할 수 없습니다."));
            return;
        }

        // TODO: Open the share editor once it is implemented.
        await Task.CompletedTask;
    }

    public override async Task HandleRepostAsync()
    {
        if (Post.DiscoveryOption == DiscoveryOption.SelectedUsers || Post.DiscoveryOption == DiscoveryOption.UnselectedUsers)
        {
            await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "공개 범위가 특정 친구 (비)공개인 게시글은 리포스트할 수 없습니다."));
            return;
        }

        var result = await App.ExecuteRequestAsync(new HandleRepost(Post.Id));
        if (result.IsFailure) return;

        var post = result.Value;
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post));
    }

    public override async Task HandleMuteNotificationsAsync()
    {
        var isMuting = !IsNotificationsMuted;
        var confirm = await HostViewModel.ShowMessageDialogAsync(new MessageDialogParameters("알림 설정", isMuting ? "이 글의 알림을 끄시겠습니까?" : "이 글의 알림을 다시 받으시겠습니까?", "설정", "취소"));
        if (confirm != ContentDialogResult.Primary) return;

        var result = isMuting ? await App.ExecuteRequestAsync(new MuteNotifications(Post.Id)) : await App.ExecuteRequestAsync(new UnmuteNotifications(Post.Id));
        if (result.IsFailure) return;

        await RefreshAsync();
    }

    // TODO: Navigate to the interactions page once it is implemented.
    public override async Task HandleReactionTapAsync() => await Task.CompletedTask;

    // TODO: Navigate to the interactions page once it is implemented.
    public override async Task HandleSharedTapAsync() => await Task.CompletedTask;

    // TODO: Navigate to the interactions page once it is implemented.
    public override async Task HandleRepostTapAsync() => await Task.CompletedTask;

    // TODO: Navigate to the user profile page once it is implemented.
    public override async Task HandleRepostedUserTap() => await Task.CompletedTask;

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