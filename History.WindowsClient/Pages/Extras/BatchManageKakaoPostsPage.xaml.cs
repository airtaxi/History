using History.WindowsClient.Enums;
using History.WindowsClient.ThirdParty.SelectorBarSegmented;
using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class BatchManageKakaoPostsPage : BasePage
{
    protected override BatchManageKakaoPostsPageViewModel ViewModel { get; }

    public BatchManageKakaoPostsPage()
    {
        ViewModel = App.Services.GetRequiredService<BatchManageKakaoPostsPageViewModel>();

        InitializeComponent();
    }

    // Stores the requested mode only (XamlRoot-independent, called from OnNavigatedTo); the
    // page wording and the confirmation flow follow it.
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is KakaoStoryBatchManageMode mode) ViewModel.Initialize(mode);
    }

    // Stops a running scan and keeps its result dialog and refresh from targeting a page
    // that is no longer on screen.
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.OnNavigatedAway();

        base.OnNavigatedFrom(e);
    }

    // The selector can clear its selection while its items are rebuilt, so only a real
    // index is applied to the filter.
    private void OnFilterSelectorBarSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs e)
    {
        if (sender is not SelectorBarSegmented segmented) return;
        if (segmented.SelectedIndex < 0) return;

        ViewModel.SelectedFilter = (KakaoStoryPostFilter)segmented.SelectedIndex;
    }
}
