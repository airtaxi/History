using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Api.Sticker;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.ViewModels;

public partial class MentionsViewModel : ObservableObject
{
    public event EventHandler<string> ImageInputRequested;
    public event EventHandler StickersLoaded;
    public event EventHandler<StickerResponseDto> StickerTabSelected;

    public MentionsViewModel()
    {
        ImageInputCommand = new Command(OnImageInput);
    }

    private void OnImageInput(object obj)
    {
        if (obj is string imagePath)
        {
            ImageInputRequested?.Invoke(this, imagePath);
        }
    }

    [ObservableProperty]
    public partial List<MentionUserViewModel> UserViewModels { get; set; }

    [ObservableProperty]
    public partial List<MentionStickerViewModel> StickerViewModels { get; set; }

    [ObservableProperty]
    public partial List<StickerResponseDto> AvailableStickers { get; set; }

    [ObservableProperty]
    public partial StickerResponseDto SelectedSticker { get; set; }

    [ObservableProperty]
    public partial string CurrentStickerName { get; set; }

    [ObservableProperty]
    public partial bool IsRecentTabSelected { get; set; }

    [ObservableProperty]
    public partial bool IsDisplayingMentions { get; set; }

    [ObservableProperty]
    public partial bool IsDisplayingUserMentions { get; set; }

    [ObservableProperty]
    public partial bool IsDisplayingStickerMentions { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InvertIsLoadingStickerMentions))]
    public partial bool IsLoadingStickerMentions { get; set; }

    public bool InvertIsLoadingStickerMentions => !IsLoadingStickerMentions;

    public Command ImageInputCommand { get; }

    private bool _stickersInitialized;

    /// <summary>
    /// Loads the user's sticker tab list. (subscribed + own stickers)
    /// </summary>
    public async Task LoadStickerTabsAsync()
    {
        if (_stickersInitialized && AvailableStickers?.Count > 0) return;

        try
        {
            var stickers = new List<StickerResponseDto>();

            // Load subscribed stickers
            var subscribedResult = await App.ExecuteRequestAsync(new GetSubscribedStickers());
            if (subscribedResult.IsSuccess)
            {
                stickers.AddRange(subscribedResult.Value);
            }

            // Load own stickers
            var myStickersResult = await App.ExecuteRequestAsync(new GetStickersByUserId(Shared.UserId));
            if (myStickersResult.IsSuccess)
            {
                // Remove duplicates (if already in subscribed list)
                foreach (var sticker in myStickersResult.Value)
                {
                    if (!stickers.Any(s => s.Id == sticker.Id))
                    {
                        stickers.Add(sticker);
                    }
                }
            }

            AvailableStickers = stickers;
            _stickersInitialized = true;
            StickersLoaded?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            AvailableStickers = [];
        }
    }

    /// <summary>
    /// Selects a specific sticker tab.
    /// </summary>
    public async Task SelectStickerAsync(StickerResponseDto sticker)
    {
        if (sticker == null) return;

        SelectedSticker = sticker;
        CurrentStickerName = sticker.Name;
        IsRecentTabSelected = false;
        StickerTabSelected?.Invoke(this, sticker);

        IsLoadingStickerMentions = true;
        try
        {
            var assetsResult = await App.ExecuteRequestAsync(new GetStickerAssets(sticker.Id));
            if (assetsResult.IsSuccess)
            {
                var viewModels = new List<MentionStickerViewModel>();
                foreach (var asset in assetsResult.Value)
                {
                    var stickerContent = new StickerContent
                    {
                        StickerId = sticker.Id,
                        StickerContentId = asset.Id,
                        StickerMediaId = asset.MediaId
                    };
                    viewModels.Add(new MentionStickerViewModel(stickerContent));
                }
                StickerViewModels = viewModels;
            }
        }
        finally
        {
            IsLoadingStickerMentions = false;
        }
    }

    /// <summary>
    /// Selects the recent usage tab.
    /// </summary>
    public async Task SelectRecentTabAsync()
    {
        SelectedSticker = null;
        CurrentStickerName = "최근 사용";
        IsRecentTabSelected = true;
        StickerTabSelected?.Invoke(this, null);

        IsLoadingStickerMentions = true;
        try
        {
            var recentResult = await App.ExecuteRequestAsync(new GetRecentStickerAssets(50));
            if (recentResult.IsSuccess)
            {
                var viewModels = new List<MentionStickerViewModel>();
                foreach (var asset in recentResult.Value)
                {
                    var stickerContent = new StickerContent
                    {
                        StickerId = asset.StickerId,
                        StickerContentId = asset.Id,
                        StickerMediaId = asset.MediaId
                    };
                    viewModels.Add(new MentionStickerViewModel(stickerContent));
                }
                StickerViewModels = viewModels;
            }
            else
            {
                StickerViewModels = [];
            }
        }
        finally
        {
            IsLoadingStickerMentions = false;
        }
    }

    /// <summary>
    /// Toggles the sticker UI visibility.
    /// </summary>
    public async Task ToggleStickerDisplayAsync()
    {
        if (IsDisplayingStickerMentions)
        {
            // Hide sticker UI
            IsDisplayingStickerMentions = false;
            IsDisplayingMentions = false;
        }
        else
        {
            // Show sticker UI
            IsDisplayingMentions = true;
            IsDisplayingStickerMentions = true;
            IsDisplayingUserMentions = false;

            // Load sticker tabs if not initialized
            if (!_stickersInitialized)
            {
                IsLoadingStickerMentions = true;
                await LoadStickerTabsAsync();

                // Start with recent usage tab
                await SelectRecentTabAsync();
            }
        }
    }

    /// <summary>
    /// Hides the sticker UI.
    /// </summary>
    public void HideStickerDisplay()
    {
        IsDisplayingStickerMentions = false;
        if (!IsDisplayingUserMentions)
        {
            IsDisplayingMentions = false;
        }
    }

    /// <summary>
    /// Sends sticker usage record to the server.
    /// </summary>
    public async Task RecordStickerUsageAsync(string stickerId, string assetId)
    {
        try
        {
            await App.ExecuteRequestAsync(new RecordStickerUsage(stickerId, assetId));
        }
        catch
        {
            // Ignore if recording usage fails
        }
    }
}
