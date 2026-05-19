using FFImageLoading.Maui;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace History.MobileClient.ContentViews.EditPost;

public partial class StickerCollectionView : ContentView
{
    private TextContentView _textContentView;
    private MentionsViewModel _viewModel;
    private readonly Dictionary<string, Border> _stickerTabs = [];

    public StickerCollectionView() => InitializeComponent();

    public void SetTextContentView(TextContentView textContentView)
    {
        _textContentView = textContentView;
        _viewModel = _textContentView.MentionsViewModel;

        BindingContext = _viewModel;

        _viewModel.StickersLoaded += OnStickersLoaded;
        _viewModel.StickerTabSelected += OnStickerTabSelected;
    }

    private void OnStickersLoaded(object sender, EventArgs e)
    {
        Dispatcher.Dispatch(BuildStickerTabs);
    }

    private void OnStickerTabSelected(object sender, StickerResponseDto sticker)
    {
        Dispatcher.Dispatch(() => UpdateTabSelection(sticker?.Id));
    }

    private void BuildStickerTabs()
    {
        // Remove existing tabs (except recent usage tab)
        var tabsToRemove = StickerTabBar.Children
            .Where(c => c != RecentTab)
            .ToList();
        foreach (var tab in tabsToRemove)
        {
            StickerTabBar.Children.Remove(tab);
        }
        _stickerTabs.Clear();

        if (_viewModel.AvailableStickers == null) return;

        // Add sticker tabs
        foreach (var sticker in _viewModel.AvailableStickers)
        {
            var tab = CreateStickerTab(sticker);
            StickerTabBar.Children.Add(tab);
            _stickerTabs[sticker.Id] = tab;
        }

        // Select recent usage tab
        UpdateTabSelection(null);
    }

    private Border CreateStickerTab(StickerResponseDto sticker)
    {
        var iconImage = new CachedImage
        {
            Source = Utils.GenerateMediaUri(sticker.IconMediaId),
            Aspect = Aspect.AspectFill,
            DownsampleToViewSize = true,
            HeightRequest = 32,
            WidthRequest = 32,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var border = new Border
        {
            Padding = new Thickness(4),
            HeightRequest = 44,
            WidthRequest = 44,
            StrokeThickness = 0,
            BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#333333")
                : Color.FromArgb("#FFFFFF"),
            Content = iconImage
        };
        border.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await _viewModel.SelectStickerAsync(sticker);
        };
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private void UpdateTabSelection(string selectedStickerId)
    {
        var selectedColor = Application.Current.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#555555")
            : Color.FromArgb("#E0E0E0");
        var normalColor = Application.Current.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#333333")
            : Color.FromArgb("#FFFFFF");

        // Recent usage tab
        RecentTab.BackgroundColor = selectedStickerId == null ? selectedColor : normalColor;

        // Sticker tabs
        foreach (var kvp in _stickerTabs)
        {
            kvp.Value.BackgroundColor = kvp.Key == selectedStickerId ? selectedColor : normalColor;
        }
    }

    private async void OnRecentTabTapped(object sender, TappedEventArgs e)
    {
        await _viewModel.SelectRecentTabAsync();
    }

    private async void OnStickerGridTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as MentionStickerViewModel;
        if (viewModel == null) return;

        var inserted = await _textContentView.InsertStickerAsync(viewModel);
        if (!inserted)
        {
            await App.TopPage.DisplayAlertAsync("오류", "스티커 이미지를 불러올 수 없습니다.", Constants.PromptOk);
            return;
        }

        // Hide sticker UI after selection
        _viewModel.HideStickerDisplay();
        Dispatcher.Dispatch(_textContentView.FocusEditor);
    }

    /// <summary>
    /// Toggles the sticker collection view visibility.
    /// </summary>
    public async Task ToggleAsync()
    {
        await _viewModel.ToggleStickerDisplayAsync();
    }
}
