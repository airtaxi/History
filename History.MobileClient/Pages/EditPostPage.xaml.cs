
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.MobileClient.ThirdParty.StaggeredLayout;
using History.MobileClient.ViewModels;
using NativeMedia;
using SpeakLink.Mention;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace History.MobileClient.Pages;

public partial class EditPostPage : ContentPage
{
    private MediaAttachmentViewModel _attachmentViewModelBeingDragged;

    public ObservableCollection<MediaAttachmentViewModel> _attachmentViewModels = [];
	public EditPostPage()
    {
        InitializeComponent();
        Initialize();
    }

    public EditPostPage(string postId)
    {
        InitializeComponent();
        Initialize();
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

        foreach(var file in files)
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

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.MainWindow.Page.Navigation.PopModalAsync();

    private async void OnUploadButtonClicked(object sender, EventArgs e)
    {
        var editorContents = MainTextContent.GetContents();
        var textContents = editorContents.OfType<TextContent>();
        textContents.FirstOrDefault()?.Text.TrimStart();
        textContents.LastOrDefault()?.Text.TrimEnd();

        if (editorContents.Count == 0 || (editorContents.Count == 1 && textContents.Count() == 1 && string.IsNullOrEmpty(textContents.First().Text)))
        {
            await DisplayAlert("오류", "빈 내용의 글은 작성할 수 없습니다", Constants.PromptOk);
            return;
        }

        var uploadContents = _attachmentViewModels.Select(x => new UploadContent()
        {
            Description = string.IsNullOrEmpty(x.Description) ? null : x.Description,
            FileName = x.FileName
        });

        var contents = editorContents.Concat(uploadContents).ToList();
        var discoveryOption = (DiscoveryOption)DiscoveryOptionPicker.SelectedIndex;

        var files = new Dictionary<string, byte[]>();
        foreach (var viewModel in _attachmentViewModels) files.Add(viewModel.FileName, viewModel.Data);

        await App.ExecuteRequestAsync(new WritePost(contents, discoveryOption, null, null, files));
        await App.MainWindow.Page.Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        foreach (var viewModel in _attachmentViewModels) viewModel.Dispose();
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        var staggeredItemsLayout = MediaCollectionView.ItemsLayout as StaggeredItemsLayout;
        var previousSpan = staggeredItemsLayout.Span;
        var newSpan = ((int)Width / 200) + 1;
        if (newSpan != previousSpan) MediaCollectionView.ItemsLayout = new StaggeredItemsLayout() { Span = newSpan };
    }
}