using Microsoft.UI.Xaml.Controls;

namespace History.Uno.Pages;

/// <summary>
/// Main shell page — replaces MAUI AppShell.
/// Hosts the 5 bottom tabs; each tab shows a placeholder page until the
/// corresponding MAUI page is migrated (phase 2/3 of the Uno migration).
/// </summary>
public sealed partial class MainPage : Page
{
    private static readonly Type[] TabPageTypes = [typeof(TimelinePage), typeof(NotificationsPage), typeof(FriendListPage), typeof(MorePage), typeof(UserPage)];

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnTabsSelectionChanged(object sender, TabBarSelectionChangedEventArgs e)
    {
        var selectedIndex = Tabs.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= TabPageTypes.Length) return;
        if (ContentFrame.CurrentSourcePageType == TabPageTypes[selectedIndex]) return;

        ContentFrame.Navigate(TabPageTypes[selectedIndex]);
    }
}
