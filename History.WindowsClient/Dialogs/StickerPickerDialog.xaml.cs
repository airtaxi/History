using History.Commons.DataTypes.Contents;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

// Sticker selection dialog: hosts the sticker picker view model and returns the chosen
// sticker through SelectedStickerContent (the dialog closes when an asset is tapped).
public sealed partial class StickerPickerDialog : ContentDialog
{
    public StickerPickerViewModel ViewModel { get; }
    public StickerContent SelectedStickerContent { get; private set; }

    public StickerPickerDialog(StickerPickerViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.AssetSelected += OnAssetSelected;
        Opened += OnDialogOpened;
        Closing += OnDialogClosing;
    }

    // Loads the sticker tabs and the recent-usage assets on the first open.
    private async void OnDialogOpened(object sender, ContentDialogOpenedEventArgs args) => await ViewModel.InitializeAsync();

    private void OnDialogClosing(object sender, ContentDialogClosingEventArgs args) => ViewModel.AssetSelected -= OnAssetSelected;

    private async void OnStickerTabClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not StickerPickerTabViewModel tab) return;
        if (tab.IsRecent) await ViewModel.SelectRecentAsync();
        else await ViewModel.SelectStickerAsync(tab);
    }

    private void OnAssetItemClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is StickerPickerAssetViewModel assetViewModel)
        {
            ViewModel.SelectAsset(assetViewModel);
        }
    }

    private void OnAssetSelected(object sender, StickerContent stickerContent)
    {
        SelectedStickerContent = stickerContent;
        Hide();
    }
}