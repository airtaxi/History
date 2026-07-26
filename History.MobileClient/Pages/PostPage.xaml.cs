using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.Platform.Compatibility;
using NativeMedia;
using System.Diagnostics;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.Pages;

public partial class PostPage : ContentPage
{
    public PostViewModel ViewModel { get; set; }

    private bool _isInForeground;
    private MediaAttachmentViewModel _commentMediaAttachmentViewModel;
    private bool IsCommentEmpty
    {
        get
        {
            var text = CommentTextContentView.Text?.Trim();
            return string.IsNullOrEmpty(text);
        }
    }

    private bool IsCommentAvailable => _commentMediaAttachmentViewModel != null || !IsCommentEmpty;

    public PostPage(PostViewModel viewModel)
    {
        Debug.WriteLine("POST PAGE LOADED");

        ViewModel = viewModel;
        InitializeComponent();
        UpdateRepostStatus(viewModel.Post);

        CommentStickerCollectionView.SetTextContentView(CommentTextContentView);

        // Should be registered once. Do not register in OnAppearing / OnNavigatedTo since it won't be unregistered in OnDisappearing / OnNavigatedFrom
        WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, OnKeyboardSizeMessageReceived);
    }

    private void UpdateRepostStatus(PostResponseDto post)
    {
        var isReposted = post.SharedAndRepostedUsers.Any(x => x.User.UserId == Shared.UserId && x.IsRepost);
        if (isReposted) RepostFontImageSource.Glyph = MaterialSharp.Shift_lock_off;
        else RepostFontImageSource.Glyph = MaterialSharp.Shift_lock;
    }

    private static async Task CommentsScrollToEnd(ScrollView scrollView)
    {
        var scrollY = scrollView.ContentSize.Height - scrollView.Height;
        scrollY = Math.Clamp(scrollY, 0, scrollView.ContentSize.Height - scrollView.Height);
        await scrollView.ScrollToAsync(0, scrollY, false);
    }

    private void OnCommentTappedMessageReceived(object recipient, CommentTappedMessage message)
    {
        var user = message.Value;
        if (user.UserId == Shared.UserId) return;

        MentionHelper.AppendUser(CommentTextContentView.SuggestingBoxControl, user.UserId, user.Nickname, true);
    }

    private void OnAppleVideoUnloadedMessageMessageReceived(object recipient, AppleVideoUnloadedMessage message)
    {
#if IOS
        (PhoneContentDataTemplatePresenter as IView)?.InvalidateMeasure();
        (TabletContentDataTemplatePresenter as IView)?.InvalidateMeasure();
#endif
    }

    private void OnPostChangedMessageReceived(object recipient, ValueChangedMessage<PostResponseDto> message)
    {
#if IOS
        (PhoneContentDataTemplatePresenter as IView)?.InvalidateMeasure();
        (TabletContentDataTemplatePresenter as IView)?.InvalidateMeasure();
#endif
    }

    private void OnImageInputRequested(object sender, string path)
    {
        var fileName = Path.GetFileName(path);
        var bytes = File.ReadAllBytes(path);

        _commentMediaAttachmentViewModel?.Dispose();
        _commentMediaAttachmentViewModel = new MediaAttachmentViewModel(fileName, bytes);
        CommentMediaFontImageSource.Glyph = MaterialSharp.Hide_image;
    }

    private async void OnCommentAttachmentImageTapped(object sender, TappedEventArgs e)
    {
        if (_commentMediaAttachmentViewModel == null)
        {
#if IOS
            CommentTextContentView.UnfocusEditor();
            var request = new MediaPickRequest(1, MediaFileType.Image) { Title = "이미지 추가" };

            var results = await MediaGallery.PickAsync(request);
            var files = results?.Files?.ToArray();
            if (files == null || files.Length == 0) return;

            if (files.Any(x => x.Extension.Equals("webp", StringComparison.OrdinalIgnoreCase)))
                _ = Toast.Make("webp 애니메이션 파일을 선택하신 경우, 업로드를 처리하는 데 시간이 오래 걸릴 수 있습니다.").Show();

            using var file = files.FirstOrDefault();

            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var fileName = file.GenerateFileName();
            var bytes = memoryStream.ToArray();

            _commentMediaAttachmentViewModel?.Dispose();
            _commentMediaAttachmentViewModel = new MediaAttachmentViewModel(fileName, bytes);
#elif ANDROID
            var image = await AndroidMediaPickerHelper.PickMediaAsync(true, false);
            if (image == null) return;

            _commentMediaAttachmentViewModel?.Dispose();
            _commentMediaAttachmentViewModel = new MediaAttachmentViewModel(image.FileName, image.Bytes);
#endif
            CommentMediaFontImageSource.Glyph = MaterialSharp.Hide_image;
            AttachmentImage.BindingContext = _commentMediaAttachmentViewModel;
            AttachmentGrid.IsVisible = true;
        }
        else
        {
            _commentMediaAttachmentViewModel?.Dispose();
            _commentMediaAttachmentViewModel = null;
            CommentMediaFontImageSource.Glyph = MaterialSharp.Image;
            AttachmentImage.BindingContext = null;
            AttachmentGrid.IsVisible = false;
        }
    }

    private async void OnSendCommentImageTapped(object sender, TappedEventArgs e)
    {
        if (!IsCommentAvailable)
        {
            await DisplayAlertAsync("오류", "빈 내용의 댓글은 작성할 수 없습니다", Constants.PromptOk);
            return;
        }

        var contents = CommentTextContentView.GetContents();
        Utils.SanitizeContents(contents);

        var files = new Dictionary<string, byte[]>();
        if (_commentMediaAttachmentViewModel != null)
        {
            var uploadContent = new UploadContent() { FileName = _commentMediaAttachmentViewModel.FileName };
            contents.Add(uploadContent);
            files.Add(_commentMediaAttachmentViewModel.FileName, _commentMediaAttachmentViewModel.Data);
        }

        var result = await App.ExecuteRequestAsync(new CreateComment(ViewModel.Post.Id, contents, files), ErrorType.BadRequest, ErrorType.Forbidden);
        if (result.Error == ErrorType.BadRequest || result.Error == ErrorType.Forbidden) await DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
        else if (result.IsSuccess)
        {
            _commentMediaAttachmentViewModel?.Dispose();
            _commentMediaAttachmentViewModel = null;
            CommentMediaFontImageSource.Glyph = MaterialSharp.Image;
            AttachmentImage.BindingContext = null;
            AttachmentGrid.IsVisible = false;

            CommentTextContentView.Text = string.Empty;
            CommentTextContentView.UnfocusEditor();

            await ViewModel.RefreshAsync();
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(100);
                if (ViewModel.IsWideMode) await CommentsScrollToEnd(TabletCommentScrollView);
                else await CommentsScrollToEnd(PhoneScrollView);
            });
        }
    }

    private async void OnMoreImageTapped(object sender, TappedEventArgs e) => await ViewModel.DisplayActionSheetAsync(true);

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnShareImageTapped(object sender, TappedEventArgs e) => await ViewModel.HandleShareAsync();

    private async void OnRepostImageTapped(object sender, TappedEventArgs e)
    {
        await ViewModel.HandleRepostAsync();

        UpdateRepostStatus(ViewModel.Post);
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
#if IOS
        (sender as RefreshView).IsRefreshing = false;
        await Task.Delay(500);
        await ViewModel.RefreshAsync();
#else
        await ViewModel.RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
#endif
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        ViewModel.IsWideMode = Width > 700;
        if (ViewModel.IsWideMode)
        {
            PhoneContentDataTemplatePresenter.ViewModel = null;
            PhoneCommentDataTemplatePresenter.ViewModel = null;
            TabletContentDataTemplatePresenter.ViewModel = ViewModel;
            TabletCommentDataTemplatePresenter.ViewModel = ViewModel;
            PhoneRefreshView.IsVisible = false;
            TabletGrid.IsVisible = true;
        }
        else
        {
            PhoneContentDataTemplatePresenter.ViewModel = ViewModel;
            PhoneCommentDataTemplatePresenter.ViewModel = ViewModel;
            TabletContentDataTemplatePresenter.ViewModel = null;
            TabletCommentDataTemplatePresenter.ViewModel = null;
            PhoneRefreshView.IsVisible = true;
            TabletGrid.IsVisible = false;
        }
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<PostResponseDto>>(this, OnPostChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<AppleVideoUnloadedMessage>(this, OnAppleVideoUnloadedMessageMessageReceived);
        WeakReferenceMessenger.Default.Register<CommentTappedMessage>(this, OnCommentTappedMessageReceived);
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        CommentTextContentView.ImageInputRequested += OnImageInputRequested;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        _commentMediaAttachmentViewModel?.Dispose();
        _commentMediaAttachmentViewModel = null;
        CommentMediaFontImageSource.Glyph = MaterialSharp.Image;
        AttachmentImage.BindingContext = null;
        AttachmentGrid.IsVisible = false;

        WeakReferenceMessenger.Default.Unregister<ValueChangedMessage<PostResponseDto>>(this);
        WeakReferenceMessenger.Default.Unregister<AppleVideoUnloadedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<CommentTappedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<LoadingStateChangedMessage>(this);
        // Do not unregister KeyboardSizeMessage, if keyboard is still open, page layout will be broken
        CommentTextContentView.ImageInputRequested -= OnImageInputRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        _ = MarkPostNotificationsAsReadAsync();

        BindingContext = ViewModel;

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    private async Task MarkPostNotificationsAsReadAsync()
    {
        var postId = ViewModel.Post.Id;
        var success = await Shared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByPostId(postId));
        if (success) WeakReferenceMessenger.Default.Send(new NotificationPostReadMessage(postId));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopAsync();
        return true;
    }

    private void OnKeyboardSizeMessageReceived(object recipient, KeyboardSizeMessage message)
    {
#if IOS
        MainGrid.Margin = new(0, 0, 0, message.Value);
#else
        MainGrid.Margin = new(0, 0, 0, message.Value);
#endif
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private void OnDeleteAttachmentGridTapped(object sender, TappedEventArgs e)
    {
        _commentMediaAttachmentViewModel?.Dispose();
        _commentMediaAttachmentViewModel = null;
        CommentMediaFontImageSource.Glyph = MaterialSharp.Image;
        AttachmentImage.BindingContext = null;
        AttachmentGrid.IsVisible = false;
    }

    private async void OnCommentStickerImageTapped(object sender, TappedEventArgs e)
    {
        CommentTextContentView.UnfocusEditor();
        await CommentStickerCollectionView.ToggleAsync();
    }
}
