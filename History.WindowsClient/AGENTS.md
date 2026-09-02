# History.WindowsClient Copilot Guidelines

## Mandatory Skill

- Before starting any WinUI-related work (.cs/.xaml/.csproj etc. — create, modify, review, refactor), you **must** load the `/csharp-winui-projects` skill.

## Initial Load Pattern and XamlRoot Rules

- Pages perform **initial data loading in OnLoaded, not OnNavigatedTo** (run once with the `_isFirstLoad` guard — see the `Pages/MainPage.xaml.cs` and `Pages/TimelinePage.xaml.cs` pattern).
- **At OnNavigatedTo, the page's XamlRoot is null**, so view-model calls started at that point that touch XamlRoot-dependent behaviors (loading overlay, dialogs, pickers) can cause behavioral errors.
- Therefore, **view-model methods called from OnNavigatedTo must not call the XamlRoot-dependent methods of `BaseViewModel` (`ViewModels/BaseViewModel.cs`).** Those methods:
  - Loading: `ExecuteRequestAsync`/`ExecuteWithLoadingAsync`/`ShowLoading`/`HideLoading`
  - Dialogs: `ShowMessageDialogAsync`/`ShowInputDialogAsync`/`ShowContentDialogAsync`
  - Pickers: `PickFileAsync`/`PickFilesAsync`/`SaveFileAsync`/`PickFolderAsync`
  - Navigation: `RequestNavigation`
- Defer initial work that needs loading/dialogs until after OnLoaded; in OnNavigatedTo, run only XamlRoot-independent logic via `base.OnNavigatedTo` (event subscription, parameter setup, etc.).
- Loading request flow: `BaseViewModel` events (`LoadingStateRequested`/`ShowLoadingRequested`/`HideLoadingRequested`) → `BasePage`/`BaseControl` sends WRM messages → `MainWindow` receives them and shows/hides the overlay.

## Standard Navigation

- View models request navigation via `BaseViewModel.RequestNavigation(pageType, parameter)` (`ViewModels/BaseViewModel.cs`) — never reference `MainWindow.Frame` directly.
- Request flow: `BaseViewModel.NavigationRequested` event → `BasePage`/`BaseControl` forwards `NavigationRequestedMessage.Send(XamlRoot, ...)` → `BaseWindow` matches the XamlRoot and calls its abstract `Navigate` → each window navigates its own root frame (e.g., `MainWindow` → `AppFrame.Navigate`).
- `RequestNavigation` is XamlRoot-dependent, so it must not be called from `OnNavigatedTo` (see the XamlRoot rules above).
- Never navigate `this.Frame` from a page: frames are nested (`MainPage.MainFrame` hosts `TimelinePage`), so only the window knows its root frame. Any window that subclasses `BaseWindow` and overrides `Navigate` can open pages such as `PostPage`.
- Post tap navigation example: `HistoryPostViewModel.HandleTapAsync` calls `BaseViewModel.RequestNavigation(typeof(PostPage), Post)`.

## Build Notes

- Verify WindowsClient builds with: `dotnet build E:/Repos/History/History.WindowsClient/History.WindowsClient.csproj -p:Platform=ARM64`