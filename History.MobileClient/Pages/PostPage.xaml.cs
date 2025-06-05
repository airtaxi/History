
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using NativeMedia;
using SpeakLink.Mention;
using System.Diagnostics;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.Pages;

public partial class PostPage : ContentPage
{
    private bool _isInForeground;
    private PostViewModel _viewModel;
    private MentionsViewModel _mentionsViewModel = new();
    private MediaAttachmentViewModel _commentMediaAttachmentViewModel;

    private bool IsCommentEmpty
    {
        get
        {
            var text = CommentMentionEditor.Text?.Trim();
            return string.IsNullOrEmpty(text);
        }
    }

    private bool IsCommentAvailable => _commentMediaAttachmentViewModel != null || !IsCommentEmpty;

    public PostPage(PostViewModel viewModel)
	{
        Debug.WriteLine("POST PAGE LOADED");

		_viewModel = viewModel;
        InitializeComponent();
        UpdateRepostStatus(viewModel.Post);

        CommentMentionEditor.BindingContext = _mentionsViewModel;
        CommentUserCollectionView.BindingContext = _mentionsViewModel;
    }

    public List<BaseContent> GetCommentContents()
    {
        var result = new List<BaseContent>();

        var spans = CommentMentionEditor?.FormattedText?.Spans;
        if (spans != null)
        {
            foreach (var span in spans)
            {
                if (span is MentionSpan mentionSpan) result.Add(new ProfileContent() { UserId = MentionHelper.MentionIdMap[int.Parse(mentionSpan.MentionId)] });
                else result.Add(new TextContent() { Text = span.Text });
            }
        }
        return result;
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

        MentionHelper.AppendMention(CommentMentionEditor, user.UserId, user.Nickname, true);
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

    private void OnUserGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MentionViewModel;

        MentionHelper.InsertMention(CommentMentionEditor, viewModel.UserId, viewModel.Nickname);
    }

    private async void OnCommentAttachmentImageTapped(object sender, TappedEventArgs e)
    {
        if (_commentMediaAttachmentViewModel == null)
        {
#if IOS
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
        }
        else
        {
            _commentMediaAttachmentViewModel?.Dispose();
            _commentMediaAttachmentViewModel = null;
            CommentMediaFontImageSource.Glyph = MaterialSharp.Image;
        }
    }

    private async void OnSendCommentImageTapped(object sender, TappedEventArgs e)
    {
        if (!IsCommentAvailable)
        {
            await DisplayAlert("오류", "빈 내용의 댓글은 작성할 수 없습니다", Constants.PromptOk);
            return;
        }

        var contents = GetCommentContents();
        Utils.TrimContents(contents);

        var files = new Dictionary<string, byte[]>();
        if (_commentMediaAttachmentViewModel != null)
        {
            var uploadContent = new UploadContent() { FileName = _commentMediaAttachmentViewModel.FileName };
            contents.Add(uploadContent);
            files.Add(_commentMediaAttachmentViewModel.FileName, _commentMediaAttachmentViewModel.Data);
        }

        try
        {
            MainActivityIndicator.IsRunning = true;
            var result = await App.ExecuteRequestAsync(new CreateComment(_viewModel.Post.Id, contents, files), ErrorType.BadRequest);
            if (result.Error == ErrorType.BadRequest) await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
            else if (result.IsSuccess)
            {
                _commentMediaAttachmentViewModel?.Dispose();
                _commentMediaAttachmentViewModel = null;
                CommentMediaFontImageSource.Glyph = MaterialSharp.Image;

                CommentMentionEditor.Text = string.Empty;
                CommentMentionEditor.Unfocus();

                await _viewModel.RefreshAsync();
                if (!_viewModel.IsWideMode)
                {
                    await Task.Delay(400);
                    await CommentsScrollToEnd(PhoneScrollView);
                }
                else await CommentsScrollToEnd(TabletCommentScrollView);
            }
        }
        finally { MainActivityIndicator.IsRunning = false; }
    }

    private async void OnMoreImageTapped(object sender, TappedEventArgs e) => await _viewModel.DisplayActionSheetAsync(true);

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnShareImageTapped(object sender, TappedEventArgs e) => await _viewModel.HandleShareAsync();

    private async void OnRepostImageTapped(object sender, TappedEventArgs e)
    {
        await _viewModel.HandleRepostAsync();

        UpdateRepostStatus(_viewModel.Post);
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
#if IOS
        (sender as RefreshView).IsRefreshing = false;
        await Task.Delay(500);
        await _viewModel.RefreshAsync();
#else
        await _viewModel.RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
#endif
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        _viewModel.IsWideMode = Width > 700;
        if (_viewModel.IsWideMode)
        {
            PhoneContentDataTemplatePresenter.ViewModel = null;
            PhoneCommentDataTemplatePresenter.ViewModel = null;
            TabletContentDataTemplatePresenter.ViewModel = _viewModel;
            TabletCommentDataTemplatePresenter.ViewModel = _viewModel;
            PhoneRefreshView.IsVisible = false;
            TabletGrid.IsVisible = true;
        }
        else
        {
            PhoneContentDataTemplatePresenter.ViewModel = _viewModel;
            PhoneCommentDataTemplatePresenter.ViewModel = _viewModel;
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
        WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, OnKeyboardSizeMessageReceived);
        _mentionsViewModel.ImageInputRequested += OnImageInputRequested;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        _commentMediaAttachmentViewModel?.Dispose();
        _commentMediaAttachmentViewModel = null;
        CommentMediaFontImageSource.Glyph = MaterialSharp.Image;

        WeakReferenceMessenger.Default.UnregisterAll(this);
        _mentionsViewModel.ImageInputRequested -= OnImageInputRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        BindingContext = _viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
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
        MainGrid.Margin = new(0, 0, 0, message.Value);
    }
}