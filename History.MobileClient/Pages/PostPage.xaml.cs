using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.DataTypes.Contents;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.Foldable;
using NativeMedia;
using SpeakLink.Mention;
using System.Linq;
using System.Threading.Tasks;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.Pages;

public partial class PostPage : ContentPage
{
    public static Dictionary<int, string> MentionIdMap = [];

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
        _mentionsViewModel.ImageInputRequested += OnImageInputRequested;
    }

    public List<BaseContent> GetCommentContents()
    {
        var result = new List<BaseContent>();
        foreach (var span in CommentMentionEditor.FormattedText.Spans)
        {
            if (span is MentionSpan mentionSpan) result.Add(new ProfileContent() { UserId = MentionIdMap[int.Parse(mentionSpan.MentionId)] });
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
        MainActivityIndicator.IsVisible = true;
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
            MainActivityIndicator.IsVisible = false;
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
        CommentImageFontImageSource.Glyph = MaterialSharp.Hide_image;
    }

    private void OnUserGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MentionViewModel;

        MentionIdMap[MentionIdMap.Count] = viewModel.UserId;
        CommentMentionEditor.InsertMention(MentionIdMap.FirstOrDefault(x => x.Value == viewModel.UserId).Key.ToString(), viewModel.Nickname);
    }

    private async void OnCommentAttachmentImageTapped(object sender, TappedEventArgs e)
    {
        if (_commentMediaAttachmentViewModel == null)
        {
            var request = new MediaPickRequest(1, MediaFileType.Image) { Title = "이미지 추가" };

            var results = await MediaGallery.PickAsync(request);
            var files = results?.Files?.ToArray();
            if (files == null || files.Length == 0) return;

            using var file = files.FirstOrDefault();

            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var fileName = file.GenerateFileName();
            var bytes = memoryStream.ToArray();

            _commentMediaAttachmentViewModel = new MediaAttachmentViewModel(fileName, bytes);
            CommentImageFontImageSource.Glyph = MaterialSharp.Hide_image;
        }
        else
        {
            _commentMediaAttachmentViewModel.Dispose();
            _commentMediaAttachmentViewModel = null;
            CommentImageFontImageSource.Glyph = MaterialSharp.Image;
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
            MainActivityIndicator.IsVisible = true;
            var result = await App.ExecuteRequestAsync(new CreateComment(_viewModel.Post.Id, contents, files), ErrorType.BadRequest);
            if (result.Error == ErrorType.BadRequest) await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
            else if (result.IsSuccess)
            {
                CommentMentionEditor.Text = string.Empty;
                CommentMentionEditor.Unfocus();

                await _viewModel.RefreshAsync();
                if (!_viewModel.IsWideMode)
                {
                    await Task.Delay(1000);
                    await MainScrollView.ScrollToAsync(0, MainScrollView.ContentSize.Height - MainScrollView.Height - CommentsScrollView.ContentSize.Height + 150, false);
                }
                else await CommentsScrollView.ScrollToAsync(0, 0, false);
            }
        }
        finally { MainActivityIndicator.IsVisible = false; }
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

    private void OnUnloaded(object sender, EventArgs e) => _mentionsViewModel.ImageInputRequested -= OnImageInputRequested;

    private async void OnMoreImageTapped(object sender, TappedEventArgs e) => await _viewModel.DisplayActionSheet(true);
}