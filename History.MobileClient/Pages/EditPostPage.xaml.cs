#if ANDROID
using History.MobileClient.Helpers;
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
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;
using UraniumUI.Icons.MaterialSymbols;


namespace History.MobileClient.Pages;

public partial class EditPostPage : ContentPage
{
    public ObservableCollection<MediaAttachmentViewModel> _attachmentViewModels = [];

    private bool _isInForeground;
    private bool _isUploading;

    private readonly bool _isShare;
    private readonly PostResponseDto _post;

    private MediaAttachmentViewModel _attachmentViewModelBeingDragged;
    private ExternalUrlContentViewModel _externalUrlContentViewModel;

	public EditPostPage()
    {
        InitializeComponent();
        Initialize();
#if IOS
        MediaCollectionView.ItemsLayout = new GridItemsLayout(3, ItemsLayoutOrientation.Vertical);
#endif

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, OnKeyboardSizeMessageReceived);
        UserCollectionView.SetTextContentView(MainTextContent);
    }

    public EditPostPage(PostResponseDto post, bool isShare) : this()
    {
        _isShare = isShare;
        _post = post;
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
            if(_post.ParentPost != null)
            {
                ShareTargetPostDataTemplatePresenter.ViewModel = new PostViewModel(_post.ParentPost, true);
                ShareTargetPostDataTemplatePresenter.IsVisible = true;
            }
        }
        else
        {
            ButtonUpload.Text = "공유";
            ShareTargetPostDataTemplatePresenter.ViewModel = new PostViewModel(_post, true);
            ShareTargetPostDataTemplatePresenter.IsVisible = true;
        }

        DiscoveryOptionPicker.SelectedIndex = (int)_post.DiscoveryOption;
    }

    private void Initialize()
    {
        MediaCollectionView.ItemsSource = _attachmentViewModels;
        DiscoveryOptionPicker.ItemsSource = Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString()).ToList();
        DiscoveryOptionPicker.SelectedIndex = (int)Shared.LastUsedPostDiscoveryOption;
        MainTextContent.ImageInputRequested += OnImageInputRequested;
    }

    private async Task ToggleExternalMediaAsync()
    {
        if (_externalUrlContentViewModel == null)
        {
            var url = await DisplayPromptAsync("URL 입력", "URL을 입력해주세요", Constants.PromptOk, Constants.PromptCancel, "URL 입력", -1, Keyboard.Url, string.Empty);
            if (url == null || url == Constants.PromptCancel) return;

            var externalUrlContent = new ExternalUrlContent { SourceUrl = url };

            IsEnabled = false;
            MainActivityIndicator.IsRunning = true;
            try
            {
                var fillResult = await App.ExecuteRequestAsync(new FillExternalUrlContent(externalUrlContent), ErrorType.BadRequest);
                if(fillResult.IsFailure)
                {
                    if (fillResult.Error == ErrorType.BadRequest) await DisplayAlert("오류", fillResult.ErrorMessage, Constants.PromptOk);
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
        }
        else
        {
            ExternalUrlContentBorder.IsVisible = false;
            ExternalUrlContentDataTemplatePresenter.ViewModel = null;
            _externalUrlContentViewModel = null;
            ExternalUrlFontImageSource.Glyph = MaterialSharp.Link;
        }
    }

    private async void OnImageInputRequested(object sender, string path)
    {
        if (_attachmentViewModels.Count == 20)
        {
            await Toast.Make("미디어는 최대 20개까지 추가할 수 있습니다.", ToastDuration.Short, 14).Show();
            return;
        }

        var fileName = Path.GetFileName(path);
        var bytes = File.ReadAllBytes(path);
        _attachmentViewModels.Add(new MediaAttachmentViewModel(fileName, bytes));
    }

    private async void OnInsertImageTapped(object sender, TappedEventArgs e)
    {
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
            _attachmentViewModels.Add(new MediaAttachmentViewModel(image.FileName, image.Bytes));
        }
#endif

        if (sizeExceed) await Toast.Make("25MB 이상의 미디어는 자동으로 제외되었습니다.").Show();
    }

    private async void OnInsertVideoTapped(object sender, TappedEventArgs e)
    {
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
            _attachmentViewModels.Add(new MediaAttachmentViewModel(video.FileName, video.Bytes));
        }
#endif

        if (sizeExceed) await Toast.Make("15MB 이상의 미디어는 자동으로 제외되었습니다.").Show();
    }

    private async void OnMediaDescriptionGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MediaAttachmentViewModel;
        if (viewModel == null) return;

        var description = await DisplayPromptAsync("설명 입력", "이 미디어에 대한 설명을 입력해주세요", Constants.PromptOk, "설명 삭제", "이 미디어에 대한 설명 입력", CommonsConstants.MaxMediaDescriptionLength, null, viewModel.Description);
        viewModel.Description = description?.Trim() ?? string.Empty;
    }

    private void OnMediaDragStarting(object sender, DragStartingEventArgs e) => _attachmentViewModelBeingDragged = (sender as Element).BindingContext as MediaAttachmentViewModel;

    private void OnMediaDrop(object sender, DropEventArgs e)
    {
        var itemToMove = _attachmentViewModelBeingDragged;
        var itemToInsertBefore = (sender as Element).BindingContext as MediaAttachmentViewModel;
        if (itemToMove == null || itemToInsertBefore == null || itemToMove == itemToInsertBefore)
            return;
        int insertAtIndex = _attachmentViewModels.IndexOf(itemToInsertBefore);
        if (insertAtIndex >= 0 && insertAtIndex < _attachmentViewModels.Count)
        {
            _attachmentViewModels.Remove(itemToMove);
            _attachmentViewModels.Insert(insertAtIndex, itemToMove);
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnUploadButtonClicked(object sender, EventArgs e)
    {
        if (_isUploading) return;
        _isUploading = true;
        try
        {
            var editorContents = MainTextContent.GetContents();
            Utils.TrimContents(editorContents);

            var files = new Dictionary<string, byte[]>();
            var mediaAndUploadContents = new List<BaseContent>();
            foreach(var viewModel in _attachmentViewModels)
            {
                if (viewModel.IsUpload)
                {
                    var uploadContent = new UploadContent
                    {
                        Description = string.IsNullOrEmpty(viewModel.Description) ? null : viewModel.Description,
                        FileName = viewModel.FileName
                    };
                    mediaAndUploadContents.Add(uploadContent);
                    files.Add(viewModel.FileName, viewModel.Data);
                }
                else
                {
                    var mediaContent = viewModel.ServerContent;
                    mediaContent.Description = viewModel.Description;
                    mediaAndUploadContents.Add(mediaContent);
                }
            }

            var contents = editorContents.Concat(mediaAndUploadContents).ToList();

            if (_externalUrlContentViewModel != null) contents.Add(_externalUrlContentViewModel.ExternalUrlContent);

            if (string.IsNullOrEmpty(MainTextContent.Text?.Trim()) && mediaAndUploadContents.Count == 0 && _externalUrlContentViewModel == null && !_isShare)
            {
                await DisplayAlert("오류", "빈 내용의 글은 작성할 수 없습니다", Constants.PromptOk);
                return;
            }

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
                        await DisplayAlert("오류", "선택된 친구가 없습니다.", Constants.PromptOk);
                        return;
                    }

                    discoveryOptionSelectedUserIds = result;
                    await Task.Delay(1000);
                }

                if (_post != null && !_isShare)
                {
                    var result = await App.ExecuteRequestAsync(new ModifyPost(_post.Id, contents, discoveryOption, discoveryOptionSelectedUserIds, files), ErrorType.BadRequest);
                    if (result.Error == ErrorType.BadRequest) await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
                    else if (result.IsSuccess)
                    {
                        _isUploading = false;
                        WeakReferenceMessenger.Default.Send<ValueChangedMessage<PostResponseDto>>(new(result.Value));
                        await App.PopAsync();
                    }
                }
                else
                {
                    var result = await App.ExecuteRequestAsync(new WritePost(contents, discoveryOption, _isShare ? _post.Id : null, discoveryOptionSelectedUserIds, files), ErrorType.BadRequest);
                    if (result.Error == ErrorType.BadRequest) await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
                    else if (result.IsSuccess)
                    {
                        if (!_isShare) Shared.LastUsedPostDiscoveryOption = discoveryOption;
                        TimelinePage.ShouldRefresh = RefreshSwitch.IsToggled;
                        UserPage.ShouldRefresh = RefreshSwitch.IsToggled;
                        await App.PopAsync();
                    }
                }
		    }
            finally { MainActivityIndicator.IsRunning = false; }
        }
        finally { _isUploading = false; }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        if (_isUploading) return;

        foreach (var viewModel in _attachmentViewModels) viewModel.Dispose();
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
#if ANDROID
        var staggeredItemsLayout = MediaCollectionView.ItemsLayout as StaggeredItemsLayout;
        if (staggeredItemsLayout == null) return;

        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 200) + 1;
        if (newSpan != previousSpan) MediaCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
#elif IOS
        var gridItemsLayout = MediaCollectionView.ItemsLayout as GridItemsLayout;
        if (gridItemsLayout == null) return;

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
    }

    private void OnDeleteAttachmentBorderTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MediaAttachmentViewModel;

        if (viewModel == null) return;

        viewModel.Dispose();
        _attachmentViewModels.Remove(viewModel);
    }

    private async void OnDiscoveryOptionPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (!_isShare) return;

        var discoveryOption = (DiscoveryOption)DiscoveryOptionPicker.SelectedIndex;
        if (discoveryOption > _post.DiscoveryOption)
        {
            await DisplayAlert("오류", "공유된 글의 공개 범위는 원본 글의 공개 범위보다 클 수 없습니다.", Constants.PromptOk);
            DiscoveryOptionPicker.SelectedIndex = (int)_post.DiscoveryOption;
        }
    }

    private async void OnDeleteExternalUrlContentBorderTapped(object sender, TappedEventArgs e) => await ToggleExternalMediaAsync();
    private async void OnInsertOrDeleteExternalUrlTapped(object sender, TappedEventArgs e) => await ToggleExternalMediaAsync();

    private void OnRefreshSwitchToggled(object sender, ToggledEventArgs e)
    {
        var @switch = sender as Switch;
        Configuration.SetValue($"ShouldRefreshOnNewPost[{_isShare}]", @switch.IsToggled);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        var shouldRefreshOnNewPost = Configuration.GetValue<bool?>($"ShouldRefreshOnNewPost[{_isShare}]") ?? !_isShare;
        RefreshSwitch.IsToggled = shouldRefreshOnNewPost;

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
        if (!_isInForeground && message.Value) return;

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
    }

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopAsync();
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
}