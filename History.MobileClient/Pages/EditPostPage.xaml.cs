#if ANDROID
using History.MobileClient.ThirdParty.StaggeredLayout;
#elif IOS
using NativeMedia;
#endif

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using SuggestingBox.Maui;
using System.Collections.ObjectModel;
using UraniumUI.Icons.MaterialSymbols;
using System.Net;
using History.MobileClient.KakaoStory;
using Microsoft.Maui.Graphics.Platform;
using System.Text;
using History.MobileClient.Enums;
using Syncfusion.Maui.Toolkit.Picker;
using MongoDB.Bson.Serialization.Serializers;
using Svg;
using System.Threading.Tasks;


namespace History.MobileClient.Pages;

public partial class EditPostPage : ContentPage
{
    private bool _isInForeground;
    private bool _preventDispose;

    private readonly bool _isShare;
    private readonly PostResponseDto _post;
    private readonly TextContent _sharedTextContent;

    private DateTime? _reservationTime;
    private AccessPermission? _commentPermission;
    private MediaAttachmentViewModel _attachmentViewModelBeingDragged;
    private ExternalUrlContentViewModel _externalUrlContentViewModel;

    private readonly ObservableCollection<MediaAttachmentViewModel> _attachmentViewModels = [];
    private readonly SemaphoreSlim _uploadSemaphore = new(1, 1);
    private PollContentViewModel _pollContentViewModel;
    private TaskCompletionSource<DateTime?> _dateTimePickerTaskCompletionSource;
    private bool _draftSaved;

    public EditPostPage()
    {
        InitializeComponent();
        Initialize();
#if IOS
        MediaCollectionView.ItemsLayout = new GridItemsLayout(3, ItemsLayoutOrientation.Vertical);
        DateTimePicker.WidthRequest = 300;
        DateTimePicker.HeightRequest = 300;
        DateTimePicker.HorizontalOptions = LayoutOptions.Center;
        DateTimePicker.VerticalOptions = LayoutOptions.Center;
#else
        DateTimePicker.IsVisible = true;
        DateTimePicker.Opacity = 0;
        DateTimePicker.Mode = PickerMode.Dialog;
#endif

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, OnKeyboardSizeMessageReceived);
        WeakReferenceMessenger.Default.Register<MentionEditorNewLineMessage>(this, OnMentionEditorNewLineMessageReceived);
        StickerCollectionView.SetTextContentView(MainTextContent);
    }

    public EditPostPage(PostResponseDto post, bool isShare) : this()
    {
        _isShare = isShare;
        _post = post;
    }

    public EditPostPage(List<MediaFile> mediaFiles) : this()
    {
        var sizeExceed = false;
        foreach (var mediaFile in mediaFiles)
        {
            var fileName = mediaFile.FileName;
            var mimeType = MimeTypes.GetMimeType(fileName);

            var isVideo = mimeType.StartsWith("video/");
            var maxSize = isVideo ? CommonsConstants.MaxUploadFileSize : CommonsConstants.MaxImageUploadFileSize;

            if (mediaFile.Bytes.Length > maxSize)
            {
                sizeExceed = true;
                continue;
            }


            var extension = Path.GetExtension(fileName);
            string randomFileName;
            do
            {
                randomFileName = Path.GetRandomFileName().Replace(".", string.Empty) + extension;
                var isExists = _attachmentViewModels.Any(x => x.FileName.Equals(randomFileName, StringComparison.OrdinalIgnoreCase));
                if (!isExists) break;
            }
            while (true);
            _attachmentViewModels.Add(new MediaAttachmentViewModel(randomFileName, mediaFile.Bytes, isVideo));
        }

        if (sizeExceed) Toast.Make("용량을 초과하는 미디어는 자동으로 제외되었습니다.").Show();
    }

    public EditPostPage(string sharedText) : this()
    {
        var url = ExtractUrlFromText(sharedText);
        if (url != null) Dispatcher.Dispatch(async () => await HandleExternalUrl(url));
        if (url != sharedText) _sharedTextContent = new() { Text = sharedText };
    }

    public EditPostPage(List<string> hashtags) : this()
    {
        // Insert hashtags as inline tokens after the view is loaded
        Loaded += (sender, eventArgs) =>
        {
            foreach (var hashtag in hashtags ?? [])
                MentionHelper.InsertToken(MainTextContent.SuggestingBoxControl, "#", hashtag, hashtag,
                    new SuggestionFormat
                    {
                        BackgroundColor = Colors.Transparent,
                        ForegroundColor = Application.Current.Resources["Primary"] as Color,
                        Bold = FormatEffect.On
                    });
        };
    }

    private void LoadPost()
    {
        if (!_isShare)
        {
            ButtonUpload.Text = "수정";
            MainTextContent.SetContents(_post.Contents);
            foreach (var mediaContent in _post.Contents.OfType<MediaContent>()) _attachmentViewModels.Add(new(mediaContent));
            var externalUrlContent = _post.Contents.OfType<ExternalUrlContent>().FirstOrDefault();
            if(externalUrlContent != null)
            {
                _externalUrlContentViewModel = new(externalUrlContent);
                ExternalUrlContentDataTemplatePresenter.ViewModel = _externalUrlContentViewModel;
                ExternalUrlContentBorder.IsVisible = true;
                ExternalUrlFontImageSource.Glyph = MaterialSharp.Link_off;
            }
            var pollContent = _post.Contents.OfType<PollContent>().FirstOrDefault();
            if (pollContent != null)
            {
                _pollContentViewModel = new(pollContent, _post.Id);
                PollContentDataTemplatePresenter.ViewModel = _pollContentViewModel;
                PollContentBorder.IsVisible = true;
            }
            if(_post.ParentPost != null)
            {
                ShareTargetPostDataTemplatePresenter.ViewModel = new PostViewModel(_post.ParentPost, PostType.Timeline);
                ShareTargetPostDataTemplatePresenter.IsVisible = true;
            }
        }
        else
        {
            ButtonUpload.Text = "공유";
            ShareTargetPostDataTemplatePresenter.ViewModel = new PostViewModel(_post, PostType.Timeline);
            ShareTargetPostDataTemplatePresenter.IsVisible = true;
        }

        var discoveryOption = Math.Min((int)Shared.LastUsedPostDiscoveryOption, (int)_post.DiscoveryOption);
        DiscoveryOptionPicker.SelectedIndex = discoveryOption;

        _commentPermission = _post.CommentPermission;
        CommentPermissionSwitch.IsToggled = _commentPermission.HasValue;
        CommentPermissionPicker.SelectedIndex = _commentPermission.HasValue ? (int)_commentPermission.Value : -1;

        DisallowShareSwitch.IsToggled = _post.DisallowShare;
    }

    private void Initialize()
    {
        MediaCollectionView.ItemsSource = _attachmentViewModels;
        CommentPermissionPicker.ItemsSource = Enum.GetValues<AccessPermission>().Select(x => x.ToDisplayString()).ToList();
        DiscoveryOptionPicker.ItemsSource = Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString()).ToList();
        DiscoveryOptionPicker.SelectedIndex = (int)Shared.LastUsedPostDiscoveryOption;
        MainTextContent.ImageInputRequested += OnImageInputRequested;
    }

    private async Task ToggleExternalMediaAsync()
    {
        if (_externalUrlContentViewModel == null)
        {
            var url = await DisplayPromptAsync("URL 입력", "URL을 입력해주세요", Constants.PromptOk, Constants.PromptCancel, "URL 입력", -1, Keyboard.Url, string.Empty);
            if (url == null) return;

            await HandleExternalUrl(url);
        }
        else
        {
            ExternalUrlContentBorder.IsVisible = false;
            ExternalUrlContentDataTemplatePresenter.ViewModel = null;
            _externalUrlContentViewModel = null;
            ExternalUrlFontImageSource.Glyph = MaterialSharp.Link;
        }
    }

    private async Task HandleExternalUrl(string url)
    {
        var externalUrlContent = new ExternalUrlContent { SourceUrl = url };

        IsEnabled = false;
        MainActivityIndicator.IsRunning = true;
        try
        {
            var fillResult = await App.ExecuteRequestAsync(new FillExternalUrlContent(externalUrlContent), ErrorType.BadRequest);
            if (fillResult.IsFailure)
            {
                if (fillResult.Error == ErrorType.BadRequest) await DisplayAlertAsync("오류", fillResult.ErrorMessage, Constants.PromptOk);
                return;
            }

            externalUrlContent = fillResult.Value;
            _externalUrlContentViewModel = new ExternalUrlContentViewModel(externalUrlContent);
            ExternalUrlContentDataTemplatePresenter.ViewModel = _externalUrlContentViewModel;
            ExternalUrlContentBorder.IsVisible = true;
            ExternalUrlFontImageSource.Glyph = MaterialSharp.Link_off;
        }
        finally
        {
            IsEnabled = true;
            MainActivityIndicator.IsRunning = false;
        }

        return;
    }

    private async void OnImageInputRequested(object sender, string path)
    {
        if (_attachmentViewModels.Count == 20)
        {
            await Toast.Make("미디어는 최대 20개까지 추가할 수 있습니다.", ToastDuration.Short, 14).Show();
            return;
        }

        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(fileName);
        var bytes = File.ReadAllBytes(path);
        string randomFileName;
        do
        {
            randomFileName = Path.GetRandomFileName().Replace(".", string.Empty) + extension;
            var isExists = _attachmentViewModels.Any(x => x.FileName.Equals(randomFileName, StringComparison.OrdinalIgnoreCase));
            if (!isExists) break;
        }
        while (true);
        _attachmentViewModels.Add(new MediaAttachmentViewModel(randomFileName, bytes));
    }

    private async void OnInsertImageTapped(object sender, TappedEventArgs e)
    {
        MainTextContent.UnfocusEditor();
        if (_attachmentViewModels.Count == 20)
        {
            await Toast.Make("미디어는 최대 20개까지 추가할 수 있습니다.", ToastDuration.Short, 14).Show();
            return;
        }

        var sizeExceed = false;
        var maxCount = 20 - _attachmentViewModels.Count;
#if IOS
        var request = new MediaPickRequest(maxCount, MediaFileType.Image) { Title = "이미지 추가" };

        var results = await MediaGallery.PickAsync(request);
        var files = results?.Files?.ToArray();
        if (files == null || files.Length == 0) return;

        if (files.Any(x => x.Extension.Equals("webp", StringComparison.OrdinalIgnoreCase)))
            _ = Toast.Make("webp 애니메이션 파일을 선택하신 경우, 업로드를 처리하는 데 시간이 오래 걸릴 수 있습니다.").Show();
            
        foreach (var file in files)
        {
            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream); 
            memoryStream.Seek(0, SeekOrigin.Begin);

            var fileName = file.GenerateFileName();
            var bytes = memoryStream.ToArray();
            
            if(bytes.Length > CommonsConstants.MaxImageUploadFileSize)
            {
                sizeExceed = true;
                continue;
            }

            _attachmentViewModels.Add(new MediaAttachmentViewModel(fileName, bytes));
            file.Dispose();
        }
#elif ANDROID
        await Toast.Make($"{maxCount}개가 넘는 미디어 파일은 무시됩니다.").Show();
        var images = await AndroidMediaPickerHelper.PickMediasAsync(maxCount, true, false);
        if (images == null || images.Count == 0) return;

        if (images.Any(x => x.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || x.FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)))
            await Toast.Make("애니메이션 이미지 파일(gif, webp)을 선택하신 경우, 업로드를 처리하는 데 시간이 오래 걸릴 수 있습니다.").Show();

        foreach (var image in images)
        {
            if(image.Bytes.Length > CommonsConstants.MaxImageUploadFileSize)
            {
                sizeExceed = true;
                continue;
            }

            var extension = Path.GetExtension(image.FileName);
            string randomFileName;
            do
            {
                randomFileName = Path.GetRandomFileName().Replace(".", string.Empty) + extension;
                var isExists = _attachmentViewModels.Any(x => x.FileName != null && x.FileName.Equals(randomFileName, StringComparison.OrdinalIgnoreCase));
                if (!isExists) break;
            }
            while (true);
            _attachmentViewModels.Add(new MediaAttachmentViewModel(randomFileName, image.Bytes));
        }
#endif

        if (sizeExceed) await Toast.Make("용량을 초과하는 미디어는 자동으로 제외되었습니다.").Show();
    }

    private async void OnInsertVideoTapped(object sender, TappedEventArgs e)
    {
        MainTextContent.UnfocusEditor();

        if (_attachmentViewModels.Count == 20)
        {
            await Toast.Make("미디어는 최대 20개까지 추가할 수 있습니다.", ToastDuration.Short, 14).Show();
            return;
        }

        var sizeExceed = false;
        var maxCount = 20 - _attachmentViewModels.Count;
#if IOS
        var request = new MediaPickRequest(maxCount, MediaFileType.Video) { Title = "비디오 추가" };

        var results = await MediaGallery.PickAsync(request);
        var files = results?.Files?.ToArray();
        if (files == null || files.Length == 0) return;

        foreach (var file in files)
        {
            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var fileName = file.GenerateFileName();
            var bytes = memoryStream.ToArray();
            
            if(bytes.Length > CommonsConstants.MaxUploadFileSize)
            {
                sizeExceed = true;
                continue;
            }

            _attachmentViewModels.Add(new MediaAttachmentViewModel(fileName, bytes, true));
            file.Dispose();
        }
#elif ANDROID
        await Toast.Make($"{maxCount}개가 넘는 미디어 파일은 무시됩니다.").Show();
        var videos = await AndroidMediaPickerHelper.PickMediasAsync(maxCount, false, true);
        if (videos == null || videos.Count == 0) return;

        foreach (var video in videos)
        {
            if (video.Bytes.Length > CommonsConstants.MaxUploadFileSize)
            {
                sizeExceed = true;
                continue;
            }

            var extension = Path.GetExtension(video.FileName);
            string randomFileName;
            do
            {
                randomFileName = Path.GetRandomFileName().Replace(".", string.Empty) + extension;
                var isExists = _attachmentViewModels.Any(x => x.FileName.Equals(randomFileName, StringComparison.OrdinalIgnoreCase));
                if (!isExists) break;
            }
            while (true);
            _attachmentViewModels.Add(new MediaAttachmentViewModel(randomFileName, video.Bytes, true));
        }
#endif

        if (sizeExceed) await Toast.Make("용량을 초과하는 미디어는 자동으로 제외되었습니다.").Show();
    }

    private async void OnMediaDescriptionGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        if (element.BindingContext is not MediaAttachmentViewModel viewModel) return;

        var description = await DisplayPromptAsync("설명 입력", "이 미디어에 대한 설명을 입력해주세요", Constants.PromptOk, "설명 삭제", "이 미디어에 대한 설명 입력", CommonsConstants.MaxMediaDescriptionLength, null, viewModel.Description);
        viewModel.Description = description?.Trim() ?? string.Empty;
    }

    private void OnMediaDragStarting(object sender, DragStartingEventArgs e) => _attachmentViewModelBeingDragged = (sender as Element).BindingContext as MediaAttachmentViewModel;

    private void OnMediaDrop(object sender, DropEventArgs e)
    {
        var itemToMove = _attachmentViewModelBeingDragged;
        if (itemToMove == null || (sender as Element).BindingContext is not MediaAttachmentViewModel itemToInsertBefore || itemToMove == itemToInsertBefore) return;

        int insertAtIndex = _attachmentViewModels.IndexOf(itemToInsertBefore);
        if (insertAtIndex >= 0 && insertAtIndex < _attachmentViewModels.Count)
        {
            _attachmentViewModels.Remove(itemToMove);
            _attachmentViewModels.Insert(insertAtIndex, itemToMove);
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e)
    {
        if (await TryNavigateBackAsync()) await App.PopAsync();
    }

    private async void OnUploadButtonClicked(object sender, EventArgs e)
    {
        if (!await _uploadSemaphore.WaitAsync(0)) return;
        _preventDispose = true;
        try
        {
            if (_reservationTime.HasValue)
            {
                var proceed = await DisplayAlertAsync("안내", "예약 시간을 설정하셨습니다. 예약 게시글은 예약 시간이 지나야 게시되며, 게시가 되기 전 까지는 게시글을 수정할 수 없습니다. 예약 게시글을 작성하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
                if (!proceed) return;
            }
            var disallowShare = DisallowShareSwitch.IsToggled;
            var editorContents = MainTextContent.GetContents();

            var files = new Dictionary<string, byte[]>();
            var mediaAndUploadContents = new List<BaseContent>();
            foreach(var viewModel in _attachmentViewModels)
            {
                if (viewModel.IsUpload)
                {
                    var uploadContent = new UploadContent
                    {
                        Description = string.IsNullOrEmpty(viewModel.Description) ? null : viewModel.Description,
                        FileName = viewModel.FileName,
                        IsSpoiler = viewModel.IsSpoiler
                    };
                    mediaAndUploadContents.Add(uploadContent);
                    files.Add(viewModel.FileName, viewModel.Data);
                }
                else
                {
                    var mediaContent = viewModel.ServerContent;
                    mediaContent.Description = viewModel.Description;
                    mediaContent.IsSpoiler = viewModel.IsSpoiler;
                    mediaAndUploadContents.Add(mediaContent);
                }
            }

            var contents = editorContents.Concat(mediaAndUploadContents).ToList();

            if (_externalUrlContentViewModel != null) contents.Add(_externalUrlContentViewModel.ExternalUrlContent);
            if (_pollContentViewModel != null) contents.Add(_pollContentViewModel.PollContent);

            if (string.IsNullOrEmpty(MainTextContent.Text?.Trim()) && mediaAndUploadContents.Count == 0 && _externalUrlContentViewModel == null && _pollContentViewModel == null && !_isShare && !editorContents.OfType<HashtagContent>().Any())
            {
                await DisplayAlertAsync("오류", "빈 내용의 글은 작성할 수 없습니다", Constants.PromptOk);
                return;
            }

            // Save draft before upload to prevent data loss on crash or error
            if (_post == null && HasDraftableContent()) SaveDraft();

            try
            {
                MainActivityIndicator.IsRunning = true;

                List<string> discoveryOptionSelectedUserIds = null;
                var discoveryOption = (DiscoveryOption)DiscoveryOptionPicker.SelectedIndex;
                if (discoveryOption == DiscoveryOption.SelectedUsers || discoveryOption == DiscoveryOption.UnselectedUsers)
                {
                    var selectUserPage = new DiscoveryOptionSelectUsersPage(_post?.DiscoveryOptionSelectedUserIds);
                    await App.PushAsync(selectUserPage);

                    var result = await selectUserPage.GetResultAsync();
                    if (result == null || result.Count == 0)
                    {
                        await DisplayAlertAsync("오류", "선택된 친구가 없습니다.", Constants.PromptOk);
                        return;
                    }

                    discoveryOptionSelectedUserIds = result;
                    await Task.Delay(1000);
                }

                if (_post != null && !_isShare)
                {
                    var result = await App.ExecuteRequestAsync(new ModifyPost(_post.Id, contents, discoveryOption, _commentPermission, disallowShare, discoveryOptionSelectedUserIds, files), ErrorType.BadRequest);
                    if (result.Error == ErrorType.BadRequest) await DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
                    else if (result.IsSuccess)
                    {
                        PostDraft.Delete();
                        WeakReferenceMessenger.Default.Send<ValueChangedMessage<PostResponseDto>>(new(result.Value));
                        await App.PopAsync();
                    }
                }
                else
                {
                    if (_post == null)
                    {
                        var shouldWritePostToKakaoStory = Configuration.GetValue<bool?>("ShouldWritePostToKakaoStory");
                        if (!shouldWritePostToKakaoStory.HasValue)
                        {
                            shouldWritePostToKakaoStory = await DisplayAlertAsync("안내", "카카오스토리에도 게시글을 작성하는 옵션을 활성화하시겠습니까? 이 옵션은 글쓰기 하단의 설정을 펼쳐 언제든지 변경할 수 있습니다.", Constants.PromptOk, Constants.PromptCancel);
                            Configuration.SetValue("ShouldWritePostToKakaoStory", shouldWritePostToKakaoStory.Value);
                        }

                        if (shouldWritePostToKakaoStory.Value)
                        {
                            MainActivityIndicator.IsRunning = true;
                            IsEnabled = false;

                            // Samsung pass will overwrite the text content, fetch the text content before logging in to KakaoStory
                            var text = MainTextContent.Text;
                            text = text?.Trim() ?? string.Empty;

                            var inlineHashtags = editorContents.OfType<HashtagContent>().Select(hashtagContent => hashtagContent.Tag).ToList();
                            if (inlineHashtags.Count > 0)
                            {
                                text += "\n\n";
                                text += string.Join(" ", inlineHashtags.Select(hashtagText => $"#{hashtagText}"));
                            }

                            // Check for profanity before uploading to KakaoStory
                            var isKakaoStoryProfanityCheckEnabled = Configuration.GetValue<bool?>("KakaoStoryProfanityCheckEnabled") ?? true;
                            if (isKakaoStoryProfanityCheckEnabled)
                            {
                                await ProfanityFilterHelper.LoadAsync();
                                var profanityWords = ProfanityFilterHelper.FindProfanity(text);
                                if (profanityWords.Count > 0)
                                {
                                    var wordList = string.Join(", ", profanityWords.Take(20));
                                    if (profanityWords.Count > 20) wordList += $" 외 {profanityWords.Count - 20}개";

                                    var rewrite = await DisplayAlertAsync(
                                        "욕설 감지",
                                        $"카카오스토리에 미러링할 글에서 다음 욕설이 감지되었습니다:\n\n{wordList}\n\n자동화된 계정 정지를 방지하기 위해 카카오스토리용 글 내용을 수정하시겠습니까?",
                                        "글 수정",
                                        "그대로 게시");

                                    if (rewrite)
                                    {
                                        IsEnabled = true;
                                        MainActivityIndicator.IsRunning = false;

                                        var rewritePage = new KakaoStoryRewritePage(text);
                                        await App.PushAsync(rewritePage);

                                        var rewrittenText = await rewritePage.GetResultAsync();

                                        IsEnabled = false;
                                        MainActivityIndicator.IsRunning = true;

                                        if (rewrittenText == null) return;
                                        text = rewrittenText;
                                    }
                                }
                            }

                            //if (!string.IsNullOrWhiteSpace(text)) text += "\n----------------------\n";
                            //text += $"이 게시글은 대체 SNS인 히스토리를 통하여 작성되었습니다.\n히스토리 디스코드 채널에 참여하여 히스토리 베타에 참여해보세요.\n베타 참여 디스코드: {Constants.DiscordInviteUrl}";

                            bool loginNeeded = true;
                            var cookies = Configuration.GetValue<List<Cookie>>("KakaoStoryCookies");
                            if (cookies != null)
                            {
                                var cookieContainer = new CookieContainer();
                                foreach (var cookie in cookies) cookieContainer.Add(cookie);

                                KakaoStoryApiHandler.Init(cookieContainer, cookies, null);
                                try
                                {
                                    await KakaoStoryApiHandler.GetFriends();
                                    loginNeeded = false;
                                }
                                catch { }
                            }

                            if (loginNeeded)
                            {
                                var savedEmail = Configuration.GetValue<string>("KakaoStoryEmail");
                                var savedEncryptedPassword = Configuration.GetValue<string>("KakaoStoryPassword");
                                if (savedEmail == null || savedEncryptedPassword == null)
                                {
                                    var useAutoFill = await DisplayAlertAsync("자동 입력", "쿠키 만료로 인해 로그인이 필요합니다. 카카오스토리 로그인 정보를 저장하여 자동 입력하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
                                    if (useAutoFill)
                                    {
                                        var email = await DisplayPromptAsync("이메일 입력", "카카오 계정 이메일을 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "이메일", -1, Keyboard.Email);
                                        if (!string.IsNullOrWhiteSpace(email))
                                        {
                                            var password = await DisplayPromptAsync("비밀번호 입력", "카카오 계정 비밀번호를 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "비밀번호", -1, Keyboard.Password);
                                            if (!string.IsNullOrWhiteSpace(password))
                                            {
                                                var encryptedPassword = AesCryptoHelper.Encrypt(password, Constants.KakaoStoryCredentialEncryptionKey);
                                                Configuration.SetValue("KakaoStoryEmail", email);
                                                Configuration.SetValue("KakaoStoryPassword", encryptedPassword);
                                            }
                                        }
                                    }
                                }

                                var kakaoStoryLoginPage = new KakaoStoryLoginPage();
                                await App.PushModalAsync(kakaoStoryLoginPage);

                                cookies = await kakaoStoryLoginPage.GetResultAsync();

                                IsEnabled = false;
                                MainActivityIndicator.IsRunning = true;

                                if (cookies == null)
                                {
                                    await DisplayAlertAsync("오류", "카카오스토리 로그인에 실패하였습니다.", Constants.PromptOk);
                                    return;
                                }

                                var cookieContainer = new CookieContainer();
                                foreach (var cookie in cookies) cookieContainer.Add(cookie);

                                KakaoStoryApiHandler.Init(cookieContainer, cookies, null);
                                Configuration.SetValue("KakaoStoryCookies", cookies);
                            }

                            try
                            {
                                if (text.Length > 4000)
                                {
                                    await DisplayAlertAsync("오류", $"카카오스토리에도 업로드 기능이 활성화되어 있으나, 카카오스토리의 글자 수 제한은 4,000자입니다. 현재 작성하신 게시글은 {text.Length}자로 제한을 초과하여 업로드하실 수 없습니다. 게시글 내용을 수정하신 후 다시 시도해 주세요.", Constants.PromptOk);
                                    return;
                                }

                                if (_attachmentViewModels.Count > 0 && _externalUrlContentViewModel != null)
                                {
                                    var proceed = await DisplayAlertAsync("경고", "카카오스토리 업로드 시, 외부 URL이 포함된 사진 및 동영상은 히스토리에서는 지원되지만, 카카오스토리에서는 지원되지 않습니다. 따라서 외부 URL은 카카오스토리에 게시되지 않습니다. 계속 진행하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
                                    if (!proceed) return;
                                }
                                var quoteDatas = KakaoStoryUtils.GetQuoteDataFromString(text);

                                var conversionFailedCount = 0;
                                KakaoStoryApiHandler.DataType.MediaData mediaData;
                                if (_attachmentViewModels.Count > 0)
                                {
                                    mediaData = new();
                                    var medias = new List<KakaoStoryApiHandler.DataType.MediaData.MediaObject>();
                                    foreach (var attachment in _attachmentViewModels)
                                    {
                                        var media = new KakaoStoryApiHandler.DataType.MediaData.MediaObject();
                                        if (!attachment.IsVideo)
                                        {
                                            string filePath = attachment.FilePath;
                                            var isWebp = attachment.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
                                            if (isWebp)
                                            {
                                                var fileName = Path.GetFileNameWithoutExtension(filePath) + ".png";
                                                filePath = Path.GetTempPath() + "c_" + fileName;
                                                using var stream = File.OpenRead(attachment.FilePath);
                                                using var image = PlatformImage.FromStream(stream);
                                                var saveStream = File.Create(filePath);
                                                if (image == null)
                                                {
                                                    conversionFailedCount++;
                                                    continue;
                                                }
                                                else
                                                {
                                                    await image.SaveAsync(saveStream, ImageFormat.Png);
                                                    saveStream.Dispose();
                                                }
                                            }

                                            try
                                            {
                                                var key = await KakaoStoryApiHandler.UploadImage(filePath);
                                                media.media_path = key;
                                                media.media_type = "image";
                                            }
                                            finally
                                            {
                                                if (filePath != attachment.FilePath)
                                                {
                                                    try { File.Delete(filePath); } catch { }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            File.WriteAllBytes(attachment.FilePath, attachment.Data);
                                            var key = await KakaoStoryApiHandler.UploadVideo(attachment.FilePath);
                                            media.media_path = key;
                                            await KakaoStoryApiHandler.WaitForVideoUploadFinish(key);
                                            media.media_type = "video";
                                        }
                                        medias.Add(media);
                                    }
                                    mediaData.media = medias;

                                    string mediaType = null;
                                    var imageExists = _attachmentViewModels.Any(x => !x.IsVideo);
                                    var videoExists = _attachmentViewModels.Any(x => x.IsVideo);
                                    if (imageExists && videoExists)
                                        mediaType = "mixed";
                                    else if (imageExists)
                                        mediaType = "image";
                                    else if (videoExists)
                                        mediaType = "video";
                                    mediaData.media_type = mediaType;
                                }
                                else mediaData = null;

                                string permission = "F";
                                if (discoveryOption == DiscoveryOption.Everyone) permission = "A";
                                else if (discoveryOption == DiscoveryOption.OnlyMe) permission = "M";

                                string scrap = null;
                                var scrapTryCount = 0;
                                if (mediaData == null && _externalUrlContentViewModel != null)
                                {
                                    async Task DoScrap()
                                    {
                                        try { scrap = await KakaoStoryApiHandler.GetScrapData(_externalUrlContentViewModel.ExternalUrlContent.SourceUrl); }
                                        catch (WebException)
                                        {
                                            scrapTryCount++;
                                            if (scrapTryCount > 3) throw;
                                            else await DoScrap();
                                        }
                                    }
                                    await DoScrap();
                                }

                                await KakaoStoryApiHandler.WritePost(quoteDatas, mediaData, permission, true, true, null, null, scrap, false, null, null);
                                if (conversionFailedCount > 0) await DisplayAlertAsync("오류", $"카키오스토리 업로드 도중 일부 webp 이미지를 png로 변환하는 데 실패하여 {conversionFailedCount}개의 이미지가 제외되었습니다. 일반적으로 이러한 이미지는 애니메이션이 포함된 webp 이미지입니다.", Constants.PromptOk);
                            }
                            catch (WebException exception)
                            {
                                var response = exception.Response as HttpWebResponse;
                                using var respReader = response.GetResponseStream();
                                using var reader = new StreamReader(respReader, Encoding.UTF8);
                                var message = await reader.ReadToEndAsync();
                                await DisplayAlertAsync("오류", $"카카오스토리 API 오류가 발생하였습니다: [{response.StatusCode}] {message}", Constants.PromptOk);
                            }
                        }
                    }

                    var result = await App.ExecuteRequestAsync(new WritePost(contents, discoveryOption, _commentPermission, disallowShare, _isShare ? _post.Id : null, discoveryOptionSelectedUserIds, files, _reservationTime.HasValue ? _reservationTime.Value.ToUniversalTime() : null), ErrorType.BadRequest);
                    if (!result.IsSuccess) await DisplayAlertAsync("오류", result.ErrorMessage, Constants.PromptOk);
                    else
                    {
                        if (!_isShare) Shared.LastUsedPostDiscoveryOption = discoveryOption;
                        if (_reservationTime == null)
                        {
                            TimelinePage.ShouldRefresh = RefreshSwitch.IsToggled;
                            UserPage.ShouldRefresh = RefreshSwitch.IsToggled;
                        }
                        PostDraft.Delete();
                        await App.PopAsync();
                    }
                }
            }
            finally
            {
                MainActivityIndicator.IsRunning = false;
                IsEnabled = true;
            }
        }
        finally
        {
            _preventDispose = false;
            _uploadSemaphore.Release();
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        if (_preventDispose || _draftSaved) return;

        foreach (var viewModel in _attachmentViewModels) viewModel.Dispose();
    }

    /// <summary>
    /// Checks whether the editor has any content worth saving as a draft.
    /// </summary>
    private bool HasDraftableContent()
    {
        var hasText = !string.IsNullOrWhiteSpace(MainTextContent.Text?.Trim());
        var hasMedia = _attachmentViewModels.Count > 0;
        var hasExternalUrl = _externalUrlContentViewModel != null;
        var hasPoll = _pollContentViewModel != null;
        var hasHashtags = MainTextContent.GetContents().OfType<HashtagContent>().Any();
        return hasText || hasMedia || hasExternalUrl || hasPoll || hasHashtags;
    }

    /// <summary>
    /// Saves the current editor state as a draft to disk.
    /// Media files are copied to a dedicated draft directory to avoid disposal.
    /// </summary>
    private void SaveDraft()
    {
        var draftDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "History", "Drafts", "Media");
        if (!Directory.Exists(draftDirectoryPath)) Directory.CreateDirectory(draftDirectoryPath);

        var draft = new PostDraft
        {
            TextContents = MainTextContent.GetContents(),
            DiscoveryOptionIndex = DiscoveryOptionPicker.SelectedIndex,
            CommentPermission = _commentPermission,
            DisallowShare = DisallowShareSwitch.IsToggled,
            SavedAtUtc = DateTime.UtcNow
        };

        if (_externalUrlContentViewModel != null) draft.ExternalUrlContent = _externalUrlContentViewModel.ExternalUrlContent;
        if (_pollContentViewModel != null) draft.PollContent = _pollContentViewModel.PollContent;

        foreach (var viewModel in _attachmentViewModels)
        {
            if (!viewModel.IsUpload) continue;

            // Copy media data to draft directory to prevent loss on Dispose
            var draftMediaPath = Path.Combine(draftDirectoryPath, viewModel.FileName);
            File.WriteAllBytes(draftMediaPath, viewModel.Data);

            draft.MediaAttachments.Add(new PostDraftMediaAttachment
            {
                FilePath = draftMediaPath,
                FileName = viewModel.FileName,
                IsVideo = viewModel.IsVideo,
                Description = viewModel.Description,
                IsSpoiler = viewModel.IsSpoiler
            });
        }

        PostDraft.Save(draft);
        _draftSaved = true;
    }

    /// <summary>
    /// Restores a saved draft into the editor.
    /// </summary>
    private void RestoreDraft(PostDraft draft)
    {
        if (draft.TextContents.Count > 0) MainTextContent.SetContents(draft.TextContents);

        foreach (var attachment in draft.MediaAttachments)
        {
            if (!File.Exists(attachment.FilePath)) continue;

            var bytes = File.ReadAllBytes(attachment.FilePath);
            var viewModel = new MediaAttachmentViewModel(attachment.FileName, bytes, attachment.IsVideo)
            {
                Description = attachment.Description ?? string.Empty,
                IsSpoiler = attachment.IsSpoiler
            };
            _attachmentViewModels.Add(viewModel);
        }

        if (draft.ExternalUrlContent != null)
        {
            _externalUrlContentViewModel = new ExternalUrlContentViewModel(draft.ExternalUrlContent);
            ExternalUrlContentDataTemplatePresenter.ViewModel = _externalUrlContentViewModel;
            ExternalUrlContentBorder.IsVisible = true;
            ExternalUrlFontImageSource.Glyph = MaterialSharp.Link_off;
        }

        if (draft.PollContent != null)
        {
            _pollContentViewModel = new PollContentViewModel(draft.PollContent, Guid.NewGuid().ToString("N"));
            PollContentDataTemplatePresenter.ViewModel = _pollContentViewModel;
            PollContentBorder.IsVisible = true;
        }

        if (draft.DiscoveryOptionIndex >= 0 && draft.DiscoveryOptionIndex < DiscoveryOptionPicker.ItemsSource.Count)
            DiscoveryOptionPicker.SelectedIndex = draft.DiscoveryOptionIndex;

        if (draft.CommentPermission.HasValue)
        {
            _commentPermission = draft.CommentPermission.Value;
            CommentPermissionSwitch.IsToggled = true;
            CommentPermissionPicker.SelectedIndex = (int)_commentPermission;
        }

        DisallowShareSwitch.IsToggled = draft.DisallowShare;

        PostDraft.Delete();
    }

    /// <summary>
    /// Prompts the user to save draft before navigating back. Returns true if navigation should proceed.
    /// </summary>
    private async Task<bool> TryNavigateBackAsync()
    {
        // Only prompt for new posts (not editing or sharing existing posts)
        if (_post != null) return true;

        if (!HasDraftableContent()) return true;

        var saveDraft = await DisplayAlertAsync("임시 저장", "작성 중인 내용이 있습니다. 임시 저장하시겠습니까?", "임시 저장", "저장하지 않음");
        if (saveDraft) SaveDraft();

        return true;
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
#if ANDROID
        if (MediaCollectionView.ItemsLayout is not StaggeredItemsLayout staggeredItemsLayout) return;

        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 200) + 1;
        if (newSpan != previousSpan) MediaCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
#elif IOS
        if (MediaCollectionView.ItemsLayout is not GridItemsLayout gridItemsLayout) return;

        var previousSpan = gridItemsLayout.Span;
        var newSpan = ((int)Width / 200) + 1;
        if (newSpan != previousSpan) gridItemsLayout.Span = newSpan;
#endif
    }

    private bool _loaded;
    private void OnMainTextContentLoaded(object sender, EventArgs e)
    {
        if (_post != null && !_loaded)
        {
            _loaded = true;
            LoadPost();
        }
        else if(_sharedTextContent != null && !_loaded)
        {
            _loaded = true;
            MainTextContent.SetContents([_sharedTextContent]);
        }
    }

    private void OnDeleteAttachmentBorderTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        if (element.BindingContext is not MediaAttachmentViewModel viewModel) return;

        viewModel.Dispose();
        _attachmentViewModels.Remove(viewModel);
    }

    private async void OnDiscoveryOptionPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var discoveryOption = (DiscoveryOption)DiscoveryOptionPicker.SelectedIndex;

        if (_commentPermission.HasValue && (discoveryOption == DiscoveryOption.SelectedUsers || discoveryOption == DiscoveryOption.UnselectedUsers))
        {
            await DisplayAlertAsync("오류", "댓글 작성 권한을 설정한 경우, 공개 범위를 특정 친구 (비)공개로 설정할 수 없습니다.", Constants.PromptOk);
            DiscoveryOptionPicker.SelectedIndex = (int)Shared.LastUsedPostDiscoveryOption;
            return;
        }

        if (!_isShare)
        {
            DiscoveryOptionFontImageSource.Glyph = Utils.GetDiscoveryOptionGlyph(discoveryOption);
            return;
        }

        if (discoveryOption > _post.DiscoveryOption)
        {
            await DisplayAlertAsync("오류", "공유된 글의 공개 범위는 원본 글의 공개 범위보다 클 수 없습니다.", Constants.PromptOk);
            DiscoveryOptionPicker.SelectedIndex = (int)_post.DiscoveryOption;
            return;
        }

        DiscoveryOptionFontImageSource.Glyph = Utils.GetDiscoveryOptionGlyph(discoveryOption);
    }

    private async void OnCommentPermissionPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (CommentPermissionPicker.SelectedIndex < 0) return; // No selection

        var discoveryOption = (DiscoveryOption)DiscoveryOptionPicker.SelectedIndex;
        var commentPermission = (AccessPermission)CommentPermissionPicker.SelectedIndex;
        var convertedCommentPermission = commentPermission.ToDiscoveryOption();

        if (convertedCommentPermission > discoveryOption)
        {
            await DisplayAlertAsync("오류", "댓글 작성 권한은 공개 범위보다 클 수 없습니다.", Constants.PromptOk);
            CommentPermissionPicker.SelectedIndex = (int)_commentPermission;
            return;
        }

        CommentPermissionFontImageSource.Glyph = Utils.GetDiscoveryOptionGlyph(convertedCommentPermission);
        _commentPermission = commentPermission;
    }

    private async void OnDeleteExternalUrlContentBorderTapped(object sender, TappedEventArgs e) => await ToggleExternalMediaAsync();
    private async void OnInsertOrDeleteExternalUrlTapped(object sender, TappedEventArgs e) => await ToggleExternalMediaAsync();

    private void OnRefreshSwitchToggled(object sender, ToggledEventArgs e)
    {
        var @switch = sender as Switch;
        Configuration.SetValue($"ShouldRefreshOnNewPost[{_isShare}]", @switch.IsToggled);
    }

    private void OnWritePostToKakaoStorySwitchToggled(object sender, ToggledEventArgs e)
    {
        var @switch = sender as Switch;
        Configuration.SetValue("ShouldWritePostToKakaoStory", @switch.IsToggled);
    }

    private void OnDisallowShareSwitchSwitchToggled(object sender, ToggledEventArgs e)
    {
        if (_post != null) return;

        var @switch = sender as Switch;
        Configuration.SetValue("DisallowShare", @switch.IsToggled);
    }

    private async void OnCommentPermissionSwitchSwitchToggled(object sender, ToggledEventArgs e)
    {
        var @switch = sender as Switch;
        if (@switch.IsToggled)
        {
            var discoveryOption = (DiscoveryOption)DiscoveryOptionPicker.SelectedIndex;
            if (discoveryOption == DiscoveryOption.SelectedUsers || discoveryOption == DiscoveryOption.UnselectedUsers)
            {
                await DisplayAlertAsync("오류", "공개 범위를 특정 친구 (비)공개로 설정한 경우, 댓글 작성 권한을 설정할 수 없습니다.", Constants.PromptOk);
                @switch.IsToggled = false;
                return;
            }

            var commentPermission = discoveryOption.ToAccessPermission();
            if (!commentPermission.HasValue)
            {
                await DisplayAlertAsync("오류", "로직 오류. 개발자에게 문의해주세요.", Constants.PromptOk);
                @switch.IsToggled = false;
                return;
            }

            _commentPermission = commentPermission.Value;
            CommentPermissionPicker.SelectedIndex = (int)_commentPermission;
            DiscoveryOptionPickerParent.IsVisible = true;
        }
        else
        {
            _commentPermission = null;
            CommentPermissionPicker.SelectedIndex = -1; // Reset to default (None)commentPermission;
            DiscoveryOptionPickerParent.IsVisible = false;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        var disallowShare = Configuration.GetValue<bool?>("DisallowShare") ?? false;
        DisallowShareSwitch.IsToggled = disallowShare;

        var shouldRefreshOnNewPost = Configuration.GetValue<bool?>($"ShouldRefreshOnNewPost[{_isShare}]") ?? !_isShare;
        RefreshSwitch.IsToggled = shouldRefreshOnNewPost;

        var shouldWritePostToKakaoStory = Configuration.GetValue<bool?>("ShouldWritePostToKakaoStory");
        if (shouldWritePostToKakaoStory.HasValue) WritePostToKakaoStorySwitch.IsToggled = shouldWritePostToKakaoStory.Value;
        WritePostToKakaoStoryGrid.IsVisible = !_isShare && _post == null;

        Dispatcher.Dispatch(async () => await LoginPage.RefreshFriendsAsync());

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
        if ((!_isInForeground && message.Value) || _preventDispose) return;

        Dispatcher.Dispatch(() =>
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

        // Prompt to restore draft only for new posts (not editing/sharing)
        if (_post == null && _sharedTextContent == null && _attachmentViewModels.Count == 0 && PostDraft.Exists())
        {
            var draft = PostDraft.Load();
            if (draft != null)
            {
                var restore = await DisplayAlertAsync("임시 저장", "임시 저장된 글이 있습니다. 복원하시겠습니까?", "복원", "삭제");
                if (restore) RestoreDraft(draft);
                else PostDraft.Delete();
            }
        }
    }

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () =>
        {
            if (await TryNavigateBackAsync()) await App.PopAsync();
        });
        return true;
    }

    private void OnKeyboardSizeMessageReceived(object recipient, KeyboardSizeMessage message)
    {
#if ANDROID
        MainGrid.Margin = new(0, 0, 0, message.Value);
#else
        MainGrid.Margin = new(0, 0, 0, message.Value);
#endif
    }

    private void OnMentionEditorNewLineMessageReceived(object recipient, MentionEditorNewLineMessage message)
    {
        // Ensure the editor is scrolled to the bottom when a new line is added
        // (At least on iOS, the editor does not scroll automatically)
        var textContentViewHeight = MainTextContent.Height;
#if IOS
        var targetScrollOffsetY = textContentViewHeight - MainScrollView.Height + 40;
#else
        var targetScrollOffsetY = textContentViewHeight - MainScrollView.Height;
#endif
        targetScrollOffsetY = Math.Max(targetScrollOffsetY, 0); // Ensure it's not negative
        if (MainScrollView.ScrollY < targetScrollOffsetY)
        {
            MainScrollView.ScrollToAsync(0, targetScrollOffsetY, false);
        }
    }

    private void OnExpandCollapseSettingsImageTapped(object sender, TappedEventArgs e)
    {
        SettingsGrid.IsVisible = !SettingsGrid.IsVisible;
        if (SettingsGrid.IsVisible) ExpandCollapseSettingsFontImageSource.Glyph = MaterialSharp.Keyboard_arrow_down;
        else ExpandCollapseSettingsFontImageSource.Glyph = MaterialSharp.Keyboard_arrow_up;
    }

    private async void OnReservationImageTapped(object sender, TappedEventArgs e)
    {
        if (_reservationTime.HasValue)
        {
            _reservationTime = null;
            ReservationFontImageSource.Glyph = MaterialSharp.Alarm_off;
        }
        else
        {
            _dateTimePickerTaskCompletionSource = new();
            DateTimePicker.MinimumDate = DateTime.Now.AddMinutes(1);
#if IOS
            DateTimePicker.IsVisible = true;
#endif
            DateTimePicker.IsOpen = true;

            var time = await _dateTimePickerTaskCompletionSource.Task;
            if (time.HasValue)
            {
                _reservationTime = time.Value;
                ReservationFontImageSource.Glyph = MaterialSharp.Alarm_on;
            }
            else
            {
                _reservationTime = null;
                ReservationFontImageSource.Glyph = MaterialSharp.Alarm_off;
            }
        }
    }

    private void OnDateTimePickerOkButtonClicked(object sender, EventArgs e)
    {
        var dateTime = DateTimePicker.SelectedDate;
        if (dateTime.HasValue) _dateTimePickerTaskCompletionSource.TrySetResult(dateTime.Value);
        else _dateTimePickerTaskCompletionSource.TrySetResult(null);
#if IOS
        DateTimePicker.IsVisible = false;
#endif
        DateTimePicker.IsOpen = false;
    }

    private void OnDateTimePickerCancelButtonClicked(object sender, EventArgs e)
    {
        _dateTimePickerTaskCompletionSource.TrySetResult(null);
#if IOS
        DateTimePicker.IsVisible = false;
#endif
        DateTimePicker.IsOpen = false;
    }

    private static string ExtractUrlFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = Utils.UrlRegex().Match(text);
        if (match.Success) return match.Value;

        return null;
    }

    private async void OnEditAttachmentGridTapped(object sender, TappedEventArgs e)
    {
        var view = sender as View;
        if (view?.BindingContext is not MediaAttachmentViewModel viewModel) return;

        if (!viewModel.IsUpload)
        {
            await Toast.Make("업로드된 미디어는 편집을 지원하지 않습니다.").Show();
            return;
        }
        else if (viewModel.IsVideo)
        {
            await Toast.Make("영상 미디어는 편집을 지원하지 않습니다.").Show();
            return;
        }

        try
        {
            _preventDispose = true;
            var page = new ImageEditorPage(viewModel.ImageSource);
            await App.PushModalAsync(page);

            var bytes = await page.GetResultAsync();
            if (bytes != null) viewModel.ApplyEdit(bytes);
        }
        finally { _preventDispose = false; }
    }

    private async void OnStickerImageTapped(object sender, TappedEventArgs e)
    {
        MainTextContent.UnfocusEditor();
        await StickerCollectionView.ToggleAsync();
    }

    private async void OnInsertPollTapped(object sender, TappedEventArgs e)
    {
        MainTextContent.UnfocusEditor();

        if (_pollContentViewModel != null)
        {
            var replace = await DisplayAlertAsync("투표 수정", "이미 추가된 투표가 있습니다. 새로 만들까요?", Constants.PromptOk, Constants.PromptCancel);
            if (!replace) return;
        }

        var question = await DisplayPromptAsync("투표 질문", "투표 질문을 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "질문", 200, Keyboard.Text);
        if (string.IsNullOrWhiteSpace(question)) return;

        var options = new List<string>();
        while (true)
        {
#if IOS
            var option = await DisplayPromptAsync("투표 옵션", options.Count < 2 ? "옵션을 2개 이상 입력해주세요. 취소를 누르면 종료됩니다." : "옵션 추가 (취소 시 종료)", Constants.PromptOk, Constants.PromptCancel, "옵션", 100, Keyboard.Text);
#else
            var option = await DisplayPromptAsync("투표 옵션", options.Count < 2 ? "옵션을 2개 이상 입력해주세요. 취소를 누르면 종료됩니다." : "옵션 추가 (취소 시 종료)", Constants.PromptOk, options.Count < 2 ? null : Constants.PromptCancel, "옵션", 100, Keyboard.Text);
#endif
            if (string.IsNullOrWhiteSpace(option))
            {
                if (options.Count >= 2) break;
                else return;
            }

            if (options.Contains(option))
            {
                await DisplayAlertAsync("오류", "중복된 옵션입니다.", Constants.PromptOk);
                continue;
            }
            options.Add(option);
        }

        if (options.Count < 2)
        {
            await DisplayAlertAsync("오류", "옵션은 최소 2개 이상이어야 합니다.", Constants.PromptOk);
            return;
        }

        var allowMultiple = await DisplayAlertAsync("투표 설정", "복수 선택을 허용할까요?", "예", "아니오");

        DateTime? expiresAt = null;
        var setExpire = await DisplayAlertAsync("마감 설정", "마감 시간을 설정하시겠습니까?", "예", "아니오");
        if (setExpire)
        {
            _dateTimePickerTaskCompletionSource = new();
            DateTimePicker.MinimumDate = DateTime.Now.AddHours(1);
#if IOS
            DateTimePicker.IsVisible = true;
#endif
            DateTimePicker.IsOpen = true;
            expiresAt = await _dateTimePickerTaskCompletionSource.Task;
        }

        var pollContent = new PollContent
        {
            PollId = Guid.NewGuid().ToString("N"),
            Question = question.Trim(),
            AllowMultipleSelection = allowMultiple,
            ExpiresAt = expiresAt,
            Options = [.. options.Select(o => new PollOption { Text = o.Trim() })]
        };

        _pollContentViewModel = new PollContentViewModel(pollContent, _post?.Id ?? Guid.NewGuid().ToString("N"));
        PollContentDataTemplatePresenter.ViewModel = _pollContentViewModel;
        PollContentBorder.IsVisible = true;
    }

    private void OnDeletePollContentTapped(object sender, TappedEventArgs e)
    {
        _pollContentViewModel = null;
        PollContentDataTemplatePresenter.ViewModel = null;
        PollContentBorder.IsVisible = false;
    }
}
