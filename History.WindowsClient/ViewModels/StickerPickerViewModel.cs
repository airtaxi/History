using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Api.Sticker;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Sticker selection dialog surface: loads the user's sticker tabs (subscribed + own,
// deduplicated) plus the recent-usage tab, and the asset grid of the active tab.
public partial class StickerPickerViewModel(BaseViewModel baseViewModel) : ObservableObject
{
    private const int RecentAssetLimit = 50;

    private readonly BaseViewModel _baseViewModel = baseViewModel;

    public event EventHandler<StickerContent> AssetSelected;

    [ObservableProperty]
    public partial ObservableCollection<StickerPickerTabViewModel> StickerTabs { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<StickerPickerAssetViewModel> AssetViewModels { get; set; } = [];

    [ObservableProperty]
    public partial string CurrentStickerName { get; set; } = "최근 사용";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public bool IsNotLoading => !IsLoading;

    public StickerContent SelectedStickerContent { get; private set; }

    // Loads the sticker tabs (subscribed + own stickers) and starts on the recent-usage tab.
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var stickers = new List<StickerResponseDto>();

            var subscribedResult = await _baseViewModel.ExecuteRequestAsync(new GetSubscribedStickers());
            if (subscribedResult.IsSuccess) stickers.AddRange(subscribedResult.Value);

            var myStickersResult = await _baseViewModel.ExecuteRequestAsync(new GetStickersByUserId(CommonShared.UserId));
            if (myStickersResult.IsSuccess)
            {
                foreach (var sticker in myStickersResult.Value)
                {
                    if (!stickers.Any(x => x.Id == sticker.Id))
                    {
                        stickers.Add(sticker);
                    }
                }
            }

            StickerTabs = new ObservableCollection<StickerPickerTabViewModel>(stickers.Select(x => new StickerPickerTabViewModel(x, false)).Prepend(new StickerPickerTabViewModel(null, true)));

            await SelectRecentAsync();
        }
        catch { AssetViewModels = []; IsEmpty = true; }
        finally { IsLoading = false; }
    }

    // Selects the recent-usage tab and loads its assets.
    public async Task SelectRecentAsync()
    {
        CurrentStickerName = "최근 사용";
        UpdateTabSelection(StickerTabs.FirstOrDefault(x => x.IsRecent));

        await LoadAssetsAsync(new GetRecentStickerAssets(RecentAssetLimit));
    }

    // Selects a sticker tab and loads its assets.
    public async Task SelectStickerAsync(StickerPickerTabViewModel tab)
    {
        if (tab?.Sticker == null) return;

        CurrentStickerName = tab.Sticker.Name;
        UpdateTabSelection(tab);

        await LoadAssetsAsync(new GetStickerAssets(tab.Sticker.Id));
    }

    // Applies the tapped asset: remembers the selection and notifies the dialog to close.
    public void SelectAsset(StickerPickerAssetViewModel assetViewModel)
    {
        if (assetViewModel == null) return;

        SelectedStickerContent = assetViewModel.StickerContent;
        AssetSelected?.Invoke(this, assetViewModel.StickerContent);
    }

    private async Task LoadAssetsAsync(IBaseRequest<List<StickerAssetResponseDto>> request)
    {
        IsLoading = true;
        try
        {
            var assetsResult = await _baseViewModel.ExecuteRequestAsync(request);
            AssetViewModels = assetsResult.IsSuccess ? CreateAssetViewModels(assetsResult.Value) : [];
            IsEmpty = AssetViewModels.Count == 0;
        }
        finally { IsLoading = false; }
    }

    private static ObservableCollection<StickerPickerAssetViewModel> CreateAssetViewModels(List<StickerAssetResponseDto> assets) => new(assets.Select(asset => new StickerPickerAssetViewModel(new StickerContent
    {
        StickerId = asset.StickerId,
        StickerContentId = asset.Id,
        StickerMediaId = asset.MediaId,
        IsAnimated = asset.IsAnimated
    })));

    private void UpdateTabSelection(StickerPickerTabViewModel selectedTab)
    {
        foreach (var tab in StickerTabs)
        {
            tab.IsSelected = ReferenceEquals(tab, selectedTab);
        }
    }
}