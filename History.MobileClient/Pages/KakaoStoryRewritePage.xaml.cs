using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;

namespace History.MobileClient.Pages;

public partial class KakaoStoryRewritePage : ContentPage
{
    private bool _isInForeground;
    private readonly string _originalText;
    private readonly TaskCompletionSource<string> _taskCompletionSource = new();

    public KakaoStoryRewritePage(string originalText)
    {
        _originalText = originalText;
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
        StickerCollectionView.SetTextContentView(MainTextContent);
    }

    /// <summary>
    /// Returns the rewritten text, or null if the user cancelled.
    /// </summary>
    public Task<string> GetResultAsync() => _taskCompletionSource.Task;

    private async Task LoadOriginalTextAsync()
    {
        var textContent = new TextContent { Text = _originalText };
        await MainTextContent.SetContentsAsync([textContent]);
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e)
    {
        _taskCompletionSource.TrySetResult(null);
        await App.PopAsync();
    }

    private async void OnSubmitButtonClicked(object sender, EventArgs e)
    {
        var text = MainTextContent.GetTextWithImageTokenReplacement("(스티커)").Trim();

        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlertAsync("오류", "빈 내용은 게시할 수 없습니다.", Constants.PromptOk);
            return;
        }

        if (text.Length > 4000)
        {
            await DisplayAlertAsync("오류", $"카카오스토리의 글자 수 제한은 4,000자입니다. 현재 {text.Length}자로 제한을 초과합니다.", Constants.PromptOk);
            return;
        }

        var profanityWords = ProfanityFilterHelper.FindProfanity(text);
        if (profanityWords.Count > 0)
        {
            var wordList = string.Join(", ", profanityWords.Take(20));
            if (profanityWords.Count > 20) wordList += $" 외 {profanityWords.Count - 20}개";

            var proceed = await DisplayAlertAsync(
                "욕설 감지",
                $"수정된 글에서 여전히 다음 욕설이 감지되었습니다:\n\n{wordList}\n\n이대로 카카오스토리에 게시하시겠습니까? 자동화된 계정 정지가 발생할 수 있습니다.",
                "그래도 게시",
                Constants.PromptCancel);
            if (!proceed) return;
        }

        _taskCompletionSource.TrySetResult(text);
        await App.PopAsync();
    }

    private async void OnMainTextContentLoaded(object sender, EventArgs e) => await LoadOriginalTextAsync();

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

        if (!_taskCompletionSource.Task.IsCompleted) _taskCompletionSource.TrySetResult(null);
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
        _taskCompletionSource.TrySetResult(null);
        _ = App.PopAsync();
        return true;
    }
}
