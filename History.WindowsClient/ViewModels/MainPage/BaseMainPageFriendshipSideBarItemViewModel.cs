using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.WindowsClient.ViewModels.Friendship;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace History.WindowsClient.ViewModels.MainPage;

public abstract partial class BaseMainPageFriendshipSideBarItemViewModel(MainPageViewModel baseViewModel) : ObservableObject
{
    public MainPageViewModel BaseViewModel { get; } = baseViewModel;

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

    // Sort surface for the shared friendship side bar template; only the friend list tab enables the toggle.
    [ObservableProperty]
    public partial bool IsSortButtonVisible { get; protected set; }

    [ObservableProperty]
    public partial string SortText { get; protected set; } = "이름순";

    [ObservableProperty]
    public partial string SortGlyph { get; protected set; } = "\uE8CB";

    [RelayCommand]
    public virtual void HandleSortTap() { }

    public abstract Task RefreshAsync();

    public abstract void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args);
    public abstract void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args);
}
