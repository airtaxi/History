using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Message;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;


#if IOS
using NativeMedia;
#endif

namespace History.MobileClient.Pages;

public partial class WriteMessagePage : ContentPage
{
    private bool _isInForeground;

    private byte[] _imageBytes;
    private string _imageFileName;
    private readonly string _receiverId;

    public WriteMessagePage(string receiverId, string nickname)
    {
        InitializeComponent();
        _receiverId = receiverId;
        ReceiverLabel.Text = $"받는 사람: {nickname}";

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        // Since MAUI 10.0.70, Dispatcher.Dispatch and MainThread.BeginInvokeOnMainThread can hang the UI on iOS after async work.
#if ANDROID
        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
#endif
    }

    private async void OnAttachImageButtonClicked(object sender, EventArgs e)
    {
#if IOS
        var request = new MediaPickRequest(1, MediaFileType.Image) { Title = "이미지 첨부" };
        var results = await MediaGallery.PickAsync(request);
        var files = results?.Files?.ToArray();
        if (files == null || files.Length == 0) return;
        using var file = files[0];
        using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        _imageFileName = file.GenerateFileName();
        _imageBytes = ms.ToArray();
#elif ANDROID
        var image = await AndroidMediaPickerHelper.PickMediaAsync(true, false);
        if (image == null) return;
        _imageFileName = image.FileName;
        _imageBytes = image.Bytes;
#endif
        AttachmentImage.Source = ImageSource.FromStream(() => new MemoryStream(_imageBytes));
        AttachmentImage.IsVisible = true;
        RemoveImageButton.IsVisible = true;
    }

    private void OnRemoveImageButtonClicked(object sender, EventArgs e)
    {
        _imageBytes = null;
        _imageFileName = null;
        AttachmentImage.Source = null;
        AttachmentImage.IsVisible = false;
        RemoveImageButton.IsVisible = false;
    }

    private async void OnBackImageTapped(object sender, EventArgs e) => await App.PopModalAsync();

    private async void OnSendButtonClicked(object sender, EventArgs e)
    {
        var text = MessageEditor.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlertAsync("오류", "쪽지 내용을 입력하세요.", "확인");
            return;
        }
        if (text.Length > 100)
        {
            await DisplayAlertAsync("오류", "쪽지는 100자 이내로 작성해야 합니다.", "확인");
            return;
        }
        var contents = new List<BaseContent> { new TextContent { Text = text } };
        var files = new Dictionary<string, byte[]>();
        if (_imageBytes != null && !string.IsNullOrEmpty(_imageFileName))
        {
            var uploadContent = new UploadContent { FileName = _imageFileName };
            contents.Add(uploadContent);
            files[_imageFileName] = _imageBytes;
        }
        var result = await App.ExecuteRequestAsync(new SendMessage(_receiverId, contents, files));
        if (result.IsSuccess)
        {
            await DisplayAlertAsync("성공", "쪽지가 전송되었습니다.", "확인");
            await App.PopModalAsync();
        }
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

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }
}
