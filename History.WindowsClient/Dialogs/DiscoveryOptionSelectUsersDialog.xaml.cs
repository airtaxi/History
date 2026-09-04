using History.WindowsClient.ViewModels;
using History.WindowsClient.ViewModels.DiscoveryOptions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

// Dialog for choosing specific friends for post discovery options.
public sealed partial class DiscoveryOptionSelectUsersDialog : ContentDialog
{
    public BaseDiscoveryOptionSelectUsersViewModel ViewModel { get; }

    public DiscoveryOptionSelectUsersDialog(BaseDiscoveryOptionSelectUsersViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Opened += OnDialogOpened;
        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private async void OnDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args) => await ViewModel.InitializeAsync();

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ViewModel.SelectedUsers.Count == 0)
        {
            args.Cancel = true;
            ValidationInfoBar.IsOpen = true;
        }
    }

    private void OnFriendItemClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BaseSelectUserViewModel user)
        {
            user.HandleTap();
            if (ViewModel.SelectedUsers.Count > 0) ValidationInfoBar.IsOpen = false;
        }
    }

    private void OnRemoveSelectedUserButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: BaseSelectUserViewModel user })
        {
            ViewModel.RemoveSelectedUser(user);
        }
    }

    private void OnLoadPresetButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DiscoveryOptionPresetItemViewModel preset })
        {
            ViewModel.LoadPreset(preset);
            if (ViewModel.SelectedUsers.Count > 0) ValidationInfoBar.IsOpen = false;
        }
    }

    private void OnDeletePresetButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DiscoveryOptionPresetItemViewModel preset })
        {
            ViewModel.DeletePreset(preset);
        }
    }
}
