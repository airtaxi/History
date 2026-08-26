using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace History.WindowsClient.ViewModels;

public abstract partial class BaseMainPageFriendshipSideBarItemViewModel(MainPageFriendshipSideBarViewModel parentViewModel) : ObservableObject
{
    public MainPageFriendshipSideBarViewModel Parent { get; } = parentViewModel;

    [ObservableProperty]
    public partial string SearchAutoSuggestBoxPlaceholderText { get; set; }

    [ObservableProperty]
    public partial string RightHeaderText { get; set; }

    [ObservableProperty]
    public partial string EmptyText { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial string Query { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BaseFriendshipViewModel> Items { get; set; }

    public abstract Task RefreshAsync();

    public abstract void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args);
    public abstract void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args);
}
