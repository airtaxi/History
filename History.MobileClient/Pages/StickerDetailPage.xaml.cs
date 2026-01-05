using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Sticker;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class StickerDetailPage : ContentPage
{
    private readonly StickerResponseDto _sticker;
    private readonly ObservableCollection<StickerAssetViewModel> _assets = [];
    private bool _isInForeground;
    private bool _isSubscribed;

    public StickerDetailPage(StickerResponseDto sticker)
    {
        _sticker = sticker;
        _isSubscribed = sticker.IsSubscribed;
        InitializeComponent();

        TitleLabel.Text = sticker.Name;
        NameLabel.Text = sticker.Name;
        CategoryLabel.Text = sticker.Category;
        AuthorLabel.Text = sticker.Author?.Nickname ?? "알 수 없음";
        PrivateBorder.IsVisible = sticker.IsPrivate;
        IconImage.Source = Utils.GenerateMediaUri(sticker.IconMediaId);

        if (!string.IsNullOrWhiteSpace(sticker.Description))
        {
            DescriptionStackLayout.IsVisible = true;
            DescriptionLabel.Text = sticker.Description;
        }

        // Check delete permission (owner or moderator+)
        var canDelete = sticker.Author?.UserId == Shared.UserId || Shared.MyRank >= Rank.Moderator;
        DeleteImage.IsVisible = canDelete;

        // Show subscribe button (not own sticker and not private)
        var canSubscribe = sticker.Author?.UserId != Shared.UserId && !sticker.IsPrivate;
        SubscribeButton.IsVisible = canSubscribe;
        UpdateSubscribeButtonState();

        AssetsCollectionView.ItemsSource = _assets;

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void UpdateSubscribeButtonState()
    {
        if (_isSubscribed)
        {
            SubscribeButton.Text = "구독 취소";
            SubscribeButton.BackgroundColor = Colors.Gray;
        }
        else
        {
            SubscribeButton.Text = "구독하기";
            SubscribeButton.BackgroundColor = Color.FromArgb("#FF6B35"); // Primary color
        }
    }

    private async Task LoadAssetsAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetStickerAssets(_sticker.Id));
        if (result.IsSuccess)
        {
            _assets.Clear();
            foreach (var asset in result.Value)
            {
                _assets.Add(new StickerAssetViewModel(asset));
            }
        }
    }

    private async void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
        await LoadAssetsAsync();
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
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnDeleteImageTapped(object sender, TappedEventArgs e)
    {
        var confirm = await DisplayAlertAsync("스티커 삭제", "정말로 이 스티커를 삭제하시겠습니까?\n삭제된 스티커는 복구할 수 없습니다.", "삭제", "취소");
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new DeleteSticker(_sticker.Id));
        if (result.IsSuccess)
        {
            await DisplayAlertAsync("안내", "스티커가 삭제되었습니다.", "확인");
            await App.PopAsync();
        }
    }

    private async void OnSubscribeButtonClicked(object sender, EventArgs e)
    {
        SubscribeButton.IsEnabled = false;
        try
        {
            if (_isSubscribed)
            {
                var result = await App.ExecuteRequestAsync(new UnsubscribeSticker(_sticker.Id));
                if (result.IsSuccess)
                {
                    _isSubscribed = false;
                    UpdateSubscribeButtonState();
                }
            }
            else
            {
                var result = await App.ExecuteRequestAsync(new SubscribeSticker(_sticker.Id));
                if (result.IsSuccess)
                {
                    _isSubscribed = true;
                    UpdateSubscribeButtonState();
                }
            }
        }
        finally
        {
            SubscribeButton.IsEnabled = true;
        }
    }

    private async void OnAuthorLabelTapped(object sender, TappedEventArgs e)
    {
        if (_sticker.Author == null) return;

        var page = new UserPage(_sticker.Author.UserId);
        await App.PushAsync(page);
    }
}
