using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using NativeMedia;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class EditPostPage : ContentPage
{
    private MediaAttachmentViewModel _attachmentViewModelBeingDragged;
    private PostResponseDto _post;
    public ObservableCollection<MediaAttachmentViewModel> _attachmentViewModels = [];

	public EditPostPage()
    {
        InitializeComponent();
        Initialize();
    }

    public EditPostPage(PostResponseDto post)
    {
        _post = post;
        InitializeComponent();
        Initialize();
        LoadPost();
    }

    private void LoadPost()
    {
        MainTextContent.SetContents(_post.Contents);
        foreach (var mediaContent in _post.Contents.OfType<MediaContent>()) _attachmentViewModels.Add(new(mediaContent));
    }

    private void Initialize()
    {
        MediaCollectionView.ItemsSource = _attachmentViewModels;
        DiscoveryOptionPicker.ItemsSource = Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString()).ToList();
        DiscoveryOptionPicker.SelectedIndex = (int)Shared.LastUsedPostDiscoveryOption;
        MainTextContent.ImageInputRequested += OnImageInputRequested;
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

        var request = new MediaPickRequest(20 - _attachmentViewModels.Count, MediaFileType.Image) { Title = "이미지 추가" };

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

            _attachmentViewModels.Add(new MediaAttachmentViewModel(fileName, bytes));
            file.Dispose();
        }
    }

    private async void OnInsertVideoTapped(object sender, TappedEventArgs e)
    {
        if (_attachmentViewModels.Count == 20)
        {
            await Toast.Make("미디어는 최대 20개까지 추가할 수 있습니다.", ToastDuration.Short, 14).Show();
            return;
        }

        var request = new MediaPickRequest(20 - _attachmentViewModels.Count, MediaFileType.Video) { Title = "비디오 추가" };

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

            _attachmentViewModels.Add(new MediaAttachmentViewModel(fileName, bytes, true));
            file.Dispose();
        }
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

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();

    private async void OnUploadButtonClicked(object sender, EventArgs e)
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

        if (string.IsNullOrEmpty(MainTextContent.Text.Trim()) && mediaAndUploadContents.Count == 0)
        {
            await DisplayAlert("오류", "빈 내용의 글은 작성할 수 없습니다", Constants.PromptOk);
            return;
        }

        var discoveryOption = (DiscoveryOption)DiscoveryOptionPicker.SelectedIndex;

        try
		{
			MainActivityIndicator.IsVisible = true;
			var result = await App.ExecuteRequestAsync(new WritePost(contents, discoveryOption, null, null, files), ErrorType.BadRequest);
            if (result.Error == ErrorType.BadRequest) await DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
            else if (result.IsSuccess)
            {
                TimelinePage.ShouldRefreshTimeline = true;
                UserPage.ShouldRefreshMyProfile = true;
                await App.PopModalAsync();
            }
		}
        finally { MainActivityIndicator.IsVisible = false; }
    }

    private void OnHandlerChanging(object sender, HandlerChangingEventArgs e)
    {
        if (e.NewHandler != null) return;

        foreach (var viewModel in _attachmentViewModels) viewModel.Dispose();
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        var staggeredItemsLayout = MediaCollectionView.ItemsLayout as StaggeredItemsLayout;
        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 200) + 1;
        if (newSpan != previousSpan) MediaCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
    }

    private void OnDeleteAttachmentBorderTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MediaAttachmentViewModel;

        if (viewModel == null) return;

        viewModel.Dispose();
        _attachmentViewModels.Remove(viewModel);
    }
}