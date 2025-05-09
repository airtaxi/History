
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using NativeMedia;
using SpeakLink.Mention;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.Pages;

public partial class PostPage : ContentPage
{
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
		_viewModel = viewModel;
        InitializeComponent();
		BindingContext = _viewModel;
        CommentMentionEditor.BindingContext = _mentionsViewModel;
        CommentUserCollectionView.BindingContext = _mentionsViewModel;
    }

    private void OnCommentTappedMessageReceived(object recipient, CommentTappedMessage message)
    {
        var user = message.Value;
        if (user.UserId == Shared.UserId) return;

        MentionHelper.AppendMention(CommentMentionEditor, user.UserId, user.Nickname, true);
    }

    public List<BaseContent> GetCommentContents()
    {
        var result = new List<BaseContent>();
        var spans = CommentMentionEditor.FormattedText.Spans;
        foreach (var span in CommentMentionEditor.FormattedText.Spans)
        {
            if (span is MentionSpan mentionSpan) result.Add(new ProfileContent() { UserId = MentionHelper.MentionIdMap[int.Parse(mentionSpan.MentionId)] });
            else result.Add(new TextContent() { Text = span.Text });
        }
        return result;
    }

    private async Task LoadMoreComments()
    {
        var lastViewModel = _viewModel.Comments.LastOrDefault();
        if (lastViewModel == null) return;

        _commentUpdating = true;
        IsEnabled = false;
        MainActivityIndicator.IsRunning = true;
        try
        {
            var commentsResult = await App.ExecuteRequestAsync(new GetCommentsByPostId(_viewModel.Post.Id, lastViewModel.Comment.Id, 20));
            if (commentsResult.IsSuccess)
            {
                var comments = commentsResult.Value;
                var commentViewModels = comments.Select(x => new CommentViewModel(x));
                foreach (var commentViewModel in commentViewModels) _viewModel.Comments.Add(commentViewModel);
            }
            else return;
        }
        finally
        {
            IsEnabled = true;
            MainActivityIndicator.IsRunning = false;
            _commentUpdating = false;
        }

        return;
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

            _commentMediaAttachmentViewModel = new MediaAttachmentViewModel(fileName, bytes);
            CommentMediaFontImageSource.Glyph = MaterialSharp.Hide_image;
        }
        else
        {
            _commentMediaAttachmentViewModel.Dispose();
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

        var textContents = contents.OfType<TextContent>();
        textContents.FirstOrDefault()?.Text.TrimStart();
        textContents.LastOrDefault()?.Text.TrimEnd();

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
                CommentMentionEditor.Text = string.Empty;
                CommentMentionEditor.Unfocus();

                await _viewModel.RefreshAsync();
                if (!_viewModel.IsWideMode)
                {
                    await Task.Delay(350);
                    await MainScrollView.ScrollToAsync(0, MainScrollView.ContentSize.Height - MainScrollView.Height - CommentsScrollView.ContentSize.Height + 150, false);
                }
                else await CommentsScrollView.ScrollToAsync(0, 0, false);
            }
        }
        finally { MainActivityIndicator.IsRunning = false; }
    }

    private void OnMainScrollViewSizeChanged(object sender, EventArgs e)
    {
        var scrollView = sender as ScrollView;
        if (_viewModel.IsWideMode) MainGrid.HeightRequest = scrollView.Height;
        else MainGrid.HeightRequest = -1;
    }

    private bool _commentUpdating = false;
    private async void OnCommentScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        if (_commentUpdating) return;
        else if (!_viewModel.IsWideMode) return;

        var scrollView = sender as ScrollView;
        // If scroll reached the bottom, load more comments
        if (_viewModel.Comments.Count != _viewModel.CommentsCount
            && scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 10)
        {
            await LoadMoreComments();
        }
    }

    private async void OnMainScrollViewScrolled(object sender, ScrolledEventArgs e)
    {
        if (_commentUpdating) return;
        else if (_viewModel.IsWideMode) return;

        var scrollView = sender as ScrollView;
        // If scroll reached the bottom, load more comments
        if (_viewModel.Comments.Count != _viewModel.CommentsCount
            && scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - 10)
        {
            await LoadMoreComments();
        }
    }

    private async void OnMoreImageTapped(object sender, TappedEventArgs e) => await _viewModel.DisplayActionSheetAsync(true);

    private void OnHandlerChanging(object sender, HandlerChangingEventArgs e)
    {
        if (e.NewHandler == null)
        {
            WeakReferenceMessenger.Default.Unregister<CommentTappedMessage>(this);
            _mentionsViewModel.ImageInputRequested -= OnImageInputRequested;
        }
        else
        {
            WeakReferenceMessenger.Default.Register<CommentTappedMessage>(this, OnCommentTappedMessageReceived);
            _mentionsViewModel.ImageInputRequested += OnImageInputRequested;
        }
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        MainGrid.ColumnDefinitions.Clear();
        MainGrid.RowDefinitions.Clear();
        _viewModel.IsWideMode = Width > 700;
        if (_viewModel.IsWideMode)
        {
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(300, GridUnitType.Absolute) });
            MainGrid.HeightRequest = MainScrollView.Height;
        }
        else
        {
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });
            MainGrid.HeightRequest = -1;
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await _viewModel.RefreshAsync();
        (sender as RefreshView).IsRefreshing = false;
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();

    private async void OnShareImageTapped(object sender, TappedEventArgs e)
    {
        var page = new EditPostPage(_viewModel.Post, true);
        await App.PushModalAsync(page);
    }
}