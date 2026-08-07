using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.Messages;
using History.MobileClient.ViewModels;
using System.Net;
using System.Text;
using UraniumUI.Icons.MaterialSymbols;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;
#if IOS
using NativeMedia;
#endif

namespace History.MobileClient.Pages;

public partial class EditCommentPage : ContentPage
{
    private bool _isInForeground;
    private readonly bool _isKakaoEdit;
    private CommentResponseDto _comment;
    private Comment _kakaoComment;
    private string _kakaoPostId;
    private MediaAttachmentViewModel _attachmentViewModel;

    public EditCommentPage(CommentResponseDto comment)
    {
        _comment = comment;
        InitializeComponent();
        Initialize();
    }

    public EditCommentPage(Comment comment, string postId)
    {
        _isKakaoEdit = true;
        _kakaoComment = comment;
        _kakaoPostId = postId;
        InitializeComponent();
        Initialize();
    }

    private void Initialize()
    {
        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        StickerCollectionView.SetTextContentView(MainTextContent);
    }

    private async Task LoadCommentAsync(CommentResponseDto comment)
    {
        await MainTextContent.SetContentsAsync(comment.Contents);
        var mediaContent = comment.Contents.OfType<MediaContent>().FirstOrDefault();

        var hasMediaContent = mediaContent != null;
        if (hasMediaContent)
        {
            _attachmentViewModel = new(mediaContent);
            AttachmentImage.BindingContext = _attachmentViewModel;
            AttachmentGrid.IsVisible = true;
        }

        CommentMediaFontImageSource.Glyph = hasMediaContent ? MaterialSharp.Hide_image : MaterialSharp.Image;
    }

    private async Task LoadKakaoCommentAsync(Comment comment)
    {
        // Emoticons are preserved as "(이모티콘)" text placeholders; the comment image
        // is loaded into the attachment grid so it can be kept, replaced, or removed.
        await MainTextContent.SetContentsAsync(TextTypeContentsViewModel.ConvertToBaseContents(comment.decorators ?? []));

        var commentMedia = comment.decorators?.FirstOrDefault(x => x.media?.thumbnail_url != null)?.media;
        if (commentMedia != null)
        {
            var medium = new Medium
            {
                media_path = commentMedia.media_path,
                thumbnail_url = commentMedia.thumbnail_url,
                url = commentMedia.url,
                origin_url = commentMedia.origin_url,
                content_type = "image",
                width = commentMedia.width,
                height = commentMedia.height
            };
            _attachmentViewModel = new MediaAttachmentViewModel(medium);
            AttachmentImage.BindingContext = _attachmentViewModel;
            AttachmentGrid.IsVisible = true;
        }

        CommentMediaFontImageSource.Glyph = _attachmentViewModel != null ? MaterialSharp.Hide_image : MaterialSharp.Image;
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        if (_isKakaoEdit)
        {
            await EditKakaoCommentAsync();
            return;
        }

        var contents = MainTextContent.GetContents();
        Utils.SanitizeContents(contents);

        var files = new Dictionary<string, byte[]>();

        if (_attachmentViewModel != null)
        {
            if (_attachmentViewModel.IsUpload)
            {
                var uploadContent = new UploadContent() { FileName = _attachmentViewModel.FileName };
                contents.Add(uploadContent);
                files.Add(_attachmentViewModel.FileName, _attachmentViewModel.Data);
            }
            else contents.Add(_attachmentViewModel.ServerContent);
        }

        if (string.IsNullOrEmpty(MainTextContent.Text?.Trim()) && _attachmentViewModel == null)
        {
            await DisplayAlertAsync("오류", "빈 내용의 댓글은 작성할 수 없습니다", Constants.PromptOk);
            return;
        }

        var result = await App.ExecuteRequestAsync(new ModifyComment(_comment.Id, contents, files), ErrorType.BadRequest);
        if (result.Error == ErrorType.BadRequest) await DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
        else if (result.IsSuccess)
        {
            WeakReferenceMessenger.Default.Send<ValueChangedMessage<CommentResponseDto>>(new(result.Value));
            await App.PopAsync();
        }
    }

    private async Task EditKakaoCommentAsync()
    {
        var stickerContents = MainTextContent.GetContents().OfType<StickerContent>().ToList();

        // Replace image tokens with "(스티커)" first so the text layer always has a placeholder.
        var replacedText = MainTextContent.GetTextWithImageTokenReplacement("(스티커)");

        if (string.IsNullOrWhiteSpace(replacedText) && _attachmentViewModel == null)
        {
            await DisplayAlertAsync("오류", "빈 내용의 댓글은 작성할 수 없습니다", Constants.PromptOk);
            return;
        }

        try
        {
            var (decorators, text) = await KakaoStoryCommentHelper.BuildCommentPayloadAsync(replacedText, stickerContents, _attachmentViewModel);

            // The old image is preserved only when the user kept it (server medium, not re-uploaded).
            var hadOriginalImage = _kakaoComment.decorators?.Any(x => x.media_path != null) == true;
            var preserveOldImage = hadOriginalImage && _attachmentViewModel != null && !_attachmentViewModel.IsUpload;
            var comment = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.EditComment(_kakaoComment, _kakaoPostId, decorators, text, preserveOldImage));

            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Comment>(comment));
            await App.PopAsync();
        }
        catch (WebException exception)
        {
            var message = exception.Message;
            try
            {
                var response = exception.Response as HttpWebResponse;
                using var respReader = response.GetResponseStream();
                using var reader = new StreamReader(respReader, Encoding.UTF8);
                message = await reader.ReadToEndAsync();
            }
            catch { }
            await DisplayAlertAsync("오류", $"카카오스토리 API 오류가 발생하였습니다: [{exception.Status}] {message}", Constants.PromptOk);
        }
    }

    private async void OnCommentMediaImageTapped(object sender, TappedEventArgs e)
    {
        if (_attachmentViewModel != null)
        {
            _attachmentViewModel?.Dispose();
            _attachmentViewModel = null;
            CommentMediaFontImageSource.Glyph = MaterialSharp.Image;
            AttachmentImage.BindingContext = null;
            AttachmentGrid.IsVisible = false;
        }
        else
        {
            string fileName;
            byte[] bytes;
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

            fileName = file.GenerateFileName();
            bytes = memoryStream.ToArray();
#elif ANDROID
            var image = await AndroidMediaPickerHelper.PickMediaAsync(true, false);
            if (image == null) return;

            fileName = image.FileName;
            bytes = image.Bytes;
#endif

            _attachmentViewModel = new MediaAttachmentViewModel(fileName, bytes);
            CommentMediaFontImageSource.Glyph = MaterialSharp.Hide_image;
            AttachmentImage.BindingContext = _attachmentViewModel;
            AttachmentGrid.IsVisible = true;
        }
    }

    private async void OnMainTextContentLoaded(object sender, EventArgs e)
    {
        if (_isKakaoEdit) await LoadKakaoCommentAsync(_kakaoComment);
        else await LoadCommentAsync(_comment);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
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

    private async void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
        await Task.Delay(100);
        MainTextContent.FocusEditor();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopAsync();
        return true;
    }

    private void OnDeleteAttachmentBorderTapped(object sender, TappedEventArgs e)
    {
        _attachmentViewModel?.Dispose();
        _attachmentViewModel = null;
        CommentMediaFontImageSource.Glyph = MaterialSharp.Image;
        AttachmentImage.BindingContext = null;
        AttachmentGrid.IsVisible = false;
    }

    private async void OnStickerImageTapped(object sender, TappedEventArgs e)
    {
        MainTextContent.UnfocusEditor();
        await StickerCollectionView.ToggleAsync();
    }
}
