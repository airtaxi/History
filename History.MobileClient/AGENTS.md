# History.MobileClient Copilot Guidelines

## Mandatory Skill

- Before starting any MAUI/WinUI-related work (.cs/.xaml/.csproj etc. — create, modify, review, refactor), you **must** load the `/csharp-winui-projects` skill.

History.MobileClient is a cross-platform mobile application built with .NET MAUI.

## Coding Standards

### Timeline Content Template Sync Rules

- Timeline/discover/repost/shared posts use the fixed-slot `TimelineContentsTemplate` (`Resources/Styles/Content.xaml`) instead of `BindableLayout`.
- When a new content type (an `IContentViewModel` implementation) handled by `ContentTemplateSelector` is added to the platform-specific post view models (`HistoryPostViewModel`, `KakaoPostViewModel`) derived from `BasePostViewModel`, you **must** add the corresponding type slot and `IsVisible` flag to `TimelineContentsViewModel` (`ViewModels/TimelineContentsViewModel.cs`) and add a `DataTemplatePresenter` slot to the `TimelineContentsTemplate` XAML.
- `PostContentTemplate` (PostPage detail) and `CommentTemplate` display the full content in order, so they keep `BindableLayout` + `ContentTemplateSelector`.

### Post Fetch Messenger Update Rules

- Whenever a post is fetched (`GetPost`/`KakaoStoryApiHandler.GetPost`) and new data is obtained — regardless of navigation (PostPage push) — you **must** notify via WeakReferenceMessenger.
  - History: `WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post));`
  - KakaoStory: `WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(post));`
- Send the update only inside the success branch, after confirming the fetch result is not null (never send a null message on failure).
- `RefreshAsync` already sends the above messages, so paths going through `RefreshAsync` need no extra send. Do not miss the direct-fetch paths that bypass `RefreshAsync` (app links, notification tabs, interaction list tabs, embedded card tabs, etc.).

### Platform Post/Comment ViewModel Separation Rules

- `BasePostViewModel`/`BaseCommentViewModel` (`History.MobileClient/ViewModels/`) hold only the common UI surface (`[ObservableProperty] protected set`) and the `[RelayCommand]` virtual command contract — no DTOs, update logic, or messengers.
- `HistoryPostViewModel`/`HistoryCommentViewModel` inherit them, own the DTO, fill the base surface via `UpdatePost`/`UpdateComment`, and handle messenger registration, API, and navigation logic.
- Commands (`[RelayCommand]`) are declared **only in the base**; derived classes purely `override` (re-declaring in a derived class causes the MVVMTK0023 duplicate command error).
- New platforms (e.g., KakaoStory) integrate directly by inheriting `Base*` and filling the surface with their own data — no separate DtoMapper.

### KakaoStory Implementation Symmetry Principle

- Implement KakaoStory features by following History's existing implementations as symmetrically as possible — match code style, line breaks, method position/order, naming, and structure (e.g., `KakaoPostViewModel` mirrors `HistoryPostViewModel`'s method order and structure).
- Keep templates unchanged when possible. Minimize template (`Post.xaml` etc.) modifications and preserve the existing template structure.
- Separate base and platform view models: `BasePostViewModel` (common UI surface/command contract) → `HistoryPostViewModel`/`KakaoPostViewModel` (platform implementations).
- Templates bind to the base view model type — e.g., `Post.xaml`'s `PostTemplate`/`PostContentTemplate`/`PostPreviewTemplate` share `x:DataType="vm:BasePostViewModel"` — and platform view models inherit and implement it. When adding a new platform view model, do not change the template's binding target type; extend only the base view model's surface.

### KakaoStory Profile Media Rules

- Never use the Kakao API `profile_video_url*` fields (animated profile videos). Using them in lists like the MAUI timeline overburdens the image decoder.
- Kakao profile image mapping (History media ID correspondence):
  - `ThumbnailMediaId` (list/thumbnail display) → `profile_image_url`
  - `MediaId` (profile detail/fullscreen display) → `profile_image_url2`
- TODO: Delete this mapping guidance once all `profile_image_url2` usages are implemented.

### App Links / Universal Links

- `https://historyweb.cc/post/{postId}` / `https://historyweb.cc/u/{userId}` links are handled as native app links in addition to external URL content tabs (`Utils.OpenLinkAsync`). `Utils.OpenLinkAsync` navigates to posts/profiles; everything else opens the browser.
- Server-hosted verification files: `https://historyweb.cc/.well-known/assetlinks.json` (Android, `com.airtaxi.history` + local/store keystore SHA256 fingerprints) / `https://historyweb.cc/apple-app-site-association` (iOS, `UP6EXS2HJJ.com.airtaxi.history`, paths `/post/*`, `/u/*`). Both must respond 200 with a JSON MIME type.
- Receive path: Android — `MainActivity` `[IntentFilter(Action.View, AutoVerify)]` → `Intent.DataString` in `HandleIntent` → `App.HandleAppLinkAsync` → `Utils.OpenLinkAsync`. iOS — `Entitlements.plist` `applinks:historyweb.cc` (site needs the Associated Domains capability) → `AppDelegate.ContinueUserActivity` (NSUserActivity.WebPageUrl) → same.
- On cold start (before login), store the URL under the `AppLinkUrlPending` key in `Preferences` and replay it via `App.ReplayPendingAppLinkUrl()` in `LoginPage.AfterLogin`, mirroring the `App.HandleKakaoStoryNotificationAsync` pattern.

## Architecture

- **Pattern**: MVVM (Model-View-ViewModel) with CommunityToolkit.Mvvm
- **Navigation**: Shell-based tab navigation
- **Messaging**: WeakReferenceMessenger
- **Styling**: XAML resource dictionaries, UraniumUI, Syncfusion themes
- **Platform-specific code**: iOS/Android implementations in the Platforms folder

## Key Components

- **Pages**: XAML-based UI pages (e.g., TimelinePage, UserPage, StickersPage, StickerDetailPage)
- **ViewModels**: CommunityToolkit.Mvvm ObservableObject subclasses, commands and property bindings
- **ContentViews**: Reusable UI components (UserCollectionView, StickerCollectionView)
- **Behaviors**: User interaction handling (e.g., SwipeToCloseBehavior)
- **Helpers**: Platform utilities (media picker, webview cookies)
- **DataTypes**: Messaging and data transfer classes
- **Enums**: Interaction types, post types, etc.
- **KakaoStoryNotificationPoller**: Common KakaoStory notification polling logic (configurable foreground interval, default 5s / Android background 15-min JobScheduler / iOS BGAppRefreshTask shared). Periodically polls the full notification list and shows only new notifications after the latest notification ID baseline as local notifications. Since the notification fetch API does not deliver mail events, the mail list (`GetMails`) is also scanned with a separate baseline; only new mails with `type == "receive"` && `read_at == null` are shown as local notifications. Never shows the login UI on 401. Tab bar badges are independent of this poller and are managed by `TabBarBadgePoller` and the list pages. The foreground loop checks `App.IsForeground` each cycle, so background polling is impossible even if window events do not fire; with `IsPollLoggingEnabled` (true) it logs `[HH:mm:ss.fff]` timestamps to ADB (logcat). The foreground interval is set via `KakaoStoryForegroundPollIntervalSeconds` (double?, seconds, default 5) in SettingsPage; `PeriodicTimer.Period` is re-evaluated each cycle so changes apply from the next cycle.
- **TabBarBadgePoller**: Tab bar badge foreground poller (10s). Polls History notifications (`HistoryUnreadNotificationCount`), received mails (`HistoryUnreadMailCount`), friend requests (`HistoryPendingFriendRequestCount`) and KakaoStory notifications/mails/invitations (`KakaoStory*Count`) to update badge counts. Independent of the KakaoStory notification setting (`KakaoStoryNotificationEnabled`), so badges keep updating even when notifications are off. The Kakao section uses `IsBackgroundMode = true` + `MaxRetryCount = 2` so it never shows the login modal. KakaoStory notification/mail/friend-request badge aggregation can be toggled independently via `KakaoStoryNotificationBadgeEnabled`/`KakaoStoryMailBadgeEnabled`/`KakaoStoryFriendRequestBadgeEnabled` in SettingsPage; disabled categories are not polled by the badge poller. The foreground loop checks `App.IsForeground` each cycle to block background polling; with `IsPollLoggingEnabled` (true) it logs `[HH:mm:ss.fff]` timestamps to ADB (logcat).
- **KakaoStoryNotificationPoster** (Android/iOS): Shows KakaoStory notifications as local notifications (Android: existing push channel `{PackageName}.push` / iOS: UNUserNotificationCenter, no Firebase). Tapping navigates to posts/profiles via scheme. Mail notifications use the custom scheme `kakaostory://messages/{id}` to open mail detail (MessagePage), titled `{sender}님이 쪽지를 보냈습니다`.
- **KakaoStoryNotificationRefreshService** (Android): Background notification polling JobService (15-min interval, `KakaoStoryNotificationJobId = 2`).
- **KakaoStoryNotificationDelegate** (iOS): UNUserNotificationCenter delegate owner. Handles Kakao notifications directly (foreground banner/tap navigation); forwards other callbacks to the Firebase plugin to preserve existing FCM push behavior. Replaces the delegate set by the Firebase plugin in AppDelegate.FinishedLaunching.
- **KakaoStoryBackgroundRefresh** (iOS): Background notification polling BGAppRefreshTask (`com.airtaxi.history.kakaostoryrefresh`, ~15-min interval decided by the system). Schedules the next run when the app enters background (Window.Stopped).

### Blazor Timeline (BlazorTimelinePage)

`BlazorTimelinePage` is an alternative implementation rendering the timeline feed with BlazorWebView (port of the `TimelinePage` → `TimelineViewModel` logic):

- **BlazorTimelinePage** (`Pages/BlazorTimelinePage.xaml`): `BlazorWebView` host page. The Android handler sets `HapticFeedbackEnabled = false` (blocks native long-press haptics), `MediaPlaybackRequiresUserGesture = false` (allows inline video autoplay), and a themed WebView background.
- **TimelineViewModel** (`ViewModels/TimelineViewModel.cs`): Post load/refresh/mode-switch logic, ported from `TimelinePage.xaml.cs`.
- **Components/Timeline/**: Blazor components. `Timeline.razor` (feed root), `PostCard.razor`, `CommentPreview.razor`, `MediaCarousel.razor`, `TextContents.razor`, `PollCard.razor`, `ExternalUrlCard.razor`. `MvvmCardBase<T>` replaces MAUI's BindingContext (PropertyChanged subscription → re-render).
- **MasonryFeed.razor**: Shared renderer for timeline feeds (feed markup/infinite scroll/interop init/Dispose/scroll-top/pull-to-refresh). Works with any data source via the `IFeed` (IBlazorFeedViewModel) parameter; provides `Header` (content above the scroll area, timeline pills), `ItemTemplate` (XAML ItemTemplateSelector counterpart, default PostCard), and `EnablePullToRefresh` (false for the search page). `Timeline.razor` is reduced to pills + MasonryFeed.
- **wwwroot/timeline-interop.js**: DOM-only logic (theme, infinite scroll, carousel indicators/height, pull-to-refresh, long-press copy, IntersectionObserver-based video viewport reset, forced feed video muting, Masonry staggered layout).
- **wwwroot/masonry.pkgd.min.js**: Locally bundled Masonry.js 4.2.2 (works offline). Staggered layout uses `floor(innerWidth / 700) + 1` columns like `TimelinePage.OnSizeChanged` — 1 column is normal document flow (LinearItemsLayout counterpart), 2+ columns activate Masonry (`#masonry-grid.masonry-active`).
- **wwwroot/timeline.css**: Feed styles with `data-theme`-based dark/light themes. The inline script in `index.html` pre-applies the theme from `prefers-color-scheme` before first paint to prevent a white flash in dark mode.

### Blazor Profile (BlazorUserPage)

`BlazorUserPage` is an alternative implementation rendering the user profile with BlazorWebView (port of the `UserPage` → `UserProfileViewModel` logic). The existing `UserPage` is kept as dead code holding the static `ShouldRefresh`/`ShouldRefreshKakaoStory` flags set by other pages:

- **BlazorUserPage** (`Pages/BlazorUserPage.xaml`): Keeps native chrome: header (back/title/layout toggle/mail/memo/friends/block/settings icons), `BlazorWebView`, compose FAB, scroll-top button. Header icon visibility binds to `UserProfileViewModel` INPC properties. Android handler settings (haptics/autoplay/theme background/`ApplyWebViewSize`) are the same as BlazorTimelinePage.
- **UserProfileViewModel** (`ViewModels/UserProfileViewModel.cs`): Load/refresh/mode-switch/layout-toggle/header-action logic ported from `UserPage.xaml.cs`. Owns `ProfileVm` (`BaseProfileViewModel`) and `Items` (a `BasePostViewModel` collection). Separately owns `IsMyProfileTab` (parameterless constructor = my-profile tab) and `ShowPillGrid`.
- **Components/Profile/**: `Profile.razor` (root), `ProfileCard.razor` (port of `ProfileTemplate` — background/profile media, favorite star, action buttons; profile image long-press uses `attachLongPress`), `PostPreviewCard.razor` (port of `PostPreviewTemplate` — 3-column grid cell).
- **Grid mode ↔ timeline mode**: `UseGridLayout` toggles between `.preview-grid` (CSS grid 3 columns, GridItemsLayout Span=3/Spacing=1 counterpart) and `#masonry-grid` + reused `PostCard`. The layout toggle only changes the DOM without resizing, so `Profile.razor` explicitly calls `timelineInterop.initMasonry`/`destroyMasonry`.
- **wwwroot/profile.css**: Profile card/preview grid styles (loaded after timeline.css in `index.html`).
- Note: XAML `CollectionView`-based features (RecyclerView virtualization settings, iOS scroll position save/restore, 1s scroll polling) are replaced by Blazor patterns (IntersectionObserver/scroll events).

### Blazor Sub-feed Pages (Discover/Search/Bookmarks)

`BlazorPublicPostsPage` (discover), `BlazorSearchPostsPage` (post search), and `BlazorBookmarkedPostsPage` (bookmarks) are Blazor ports of `PublicPostsPage`/`SearchPostsPage`/`BookmarkedPostsPage` respectively. All three share `MasonryFeed` as the common feed and keep native page chrome (header/search bar/empty state/scroll-top/indicator):

- **View models**: `PublicPostsViewModel` (`GetPublicPosts` paging, `PublicPostsPage.ShouldRefresh` check), `SearchPostsViewModel` (`SearchAsync(query)`/LoadMore, `IsEmptyVisible`), `BookmarkedPostsViewModel` (`GetBookmarkedPosts` 20-item paging, `PostUnbookmarkedMessage` handling, `IsEmptyVisible`, scroll-top surface) — all implement `IBlazorFeedViewModel`, ported from the code-behind (pages).
- **Root components**: `PublicPosts.razor` (ItemTemplate selector: `HistoryPublicPostViewModel` → PublicPostCard, reposts → PostCard), `SearchPosts.razor` (pull-to-refresh disabled), `BookmarkedPosts.razor`.
- **PublicPostCard.razor**: Port of `PublicPostTemplate` — card tap navigates to profile (`HandleProfileTapCommand`), the more button is `HandlePublicPostMoreTapCommand`, no action row/comment preview, keeps the share section.
- **Native chrome**: The search page keeps the native SearchBar (keyboard hidden on search, iOS soft-keyboard SafeAreaEdges). The bookmarks page has a native empty-state overlay + **scroll-top button** (fixes the missing-button bug in the legacy XAML pages).
- The 3 legacy pages stay as dead code (`PublicPostsPage.ShouldRefresh` is set by HistoryPostViewModel/BulkPostManagePage/MorePage, so it must be kept).

### Legacy XAML Pages (No Handling Needed)

The following XAML pages are replaced by Blazor versions and are considered **legacy**. Exclude them from code modification/refactoring/style references; use the Blazor versions as the baseline for new features:

- `Pages/TimelinePage.xaml` → `Pages/BlazorTimelinePage.xaml` (timeline)
- `Pages/UserPage.xaml` → `Pages/BlazorUserPage.xaml` (profile)
- `Pages/PublicPostsPage.xaml` → `Pages/BlazorPublicPostsPage.xaml` (discover)
- `Pages/SearchPostsPage.xaml` → `Pages/BlazorSearchPostsPage.xaml` (post search)
- `Pages/BookmarkedPostsPage.xaml` → `Pages/BlazorBookmarkedPostsPage.xaml` (bookmarks)

However, `UserPage` (static `ShouldRefresh`/`ShouldRefreshKakaoStory` flags) and the 3 discover/search/bookmarks pages (static `PublicPostsPage.ShouldRefresh` flag) are kept as dead code because other pages set these static flags — **do not delete them**. Keep only the static flag properties and the basic code-behind skeleton; do not handle any other logic.

### App Structure

AppShell.xaml consists of the following tabs:
- **Timeline**: friends' post feed
- **Notifications/Mails**: notifications and private messages
- **Friends**: friend management (list, add, requests, etc.)
- **More**: discover (public posts), stickers, and other extras
- **Profile**: user profile

### Sticker System

Stickers are custom image assets usable in posts and comments:
- **StickersPage**: sticker list and search
- **StickerDetailPage**: sticker details, asset viewing, subscribe/unsubscribe buttons
- **CreateStickerPage**: new sticker creation (icon, name, category, asset upload)
- **StickerCollectionView**: sticker picker UI in compose/comments (tab bar + asset grid)
- **MentionsViewModel**: `%`-based sticker display, tab selection, recent usage loading

Sticker characteristics:
- Anyone can create; private option supported
- Max 384x384 size; static images and GIFs (GIF/WebP) supported (no video)
- Max 50 assets, each file up to 5MB
- Deletion allowed only by the owner or moderators
- **Subscription**: subscribe to other users' public stickers for quick access
- **Recent usage**: automatically recorded when a sticker asset is used, max 50 saved

Sticker picker UI (StickerCollectionView):
- Top tab bar: recent usage (clock icon) + subscribed/my sticker icon tabs
- Sticker name label: shows the currently selected sticker name
- Asset grid: 4-column grid of sticker assets
- Selecting a sticker automatically sends usage history

## Coding Standards (MAUI)

- **XAML**: named styles, explicit binding modes, FontImageSource icons, reusable UI via DataTemplate, typed bindings via x:DataType
- **ViewModels**: ObservableProperty, RelayCommand, async methods for API calls (always reference HistoryPostViewModel.cs)
- **Navigation**: App.PushAsync/PopAsync static methods
- **API calls**: App.ExecuteRequestAsync, loading state management
- **Messaging**: WeakReferenceMessenger.Default.Send/Receive
- **Collections**: ObservableCollection for dynamic lists
- **CollectionView**: Header for profile sections, RemainingItemsThreshold for pagination, ItemTemplateSelector for different types (always reference PostPage)
- **Data Templates**: DataTemplateSelector for conditional rendering of posts, comments, media
- **Behaviors**: custom behaviors for interactions like swipe or tap
- **ContentViews**: reusable components accessed from code-behind via x:Name

### Common Patterns (MAUI)

- **Page structure**: XAML for layout, code-behind for initialization and event handling, Loaded event for setup
- **ViewModel structure**: properties for data binding, commands for actions, async methods for API calls, ObservableCollection for lists
- **Data templates**: DataTemplateSelector for different item types (posts, profiles, comments), BindableLayout for dynamic content
- **Collection views**: RefreshView when pull-to-refresh is needed (always reference TimelinePage)
- **Modals**: PushModalAsync for overlays (login, editor), PushAsync for navigation
- **Toast/alert**: CommunityToolkit.Maui.Alerts
- **Floating button**: Border with TapGestureRecognizer for actions like composing posts (always reference TimelinePage)
- **Headers**: Grid with image and labels for navigation/actions, StatusBarBehavior for status bar styling (orange: reference TimelinePage, black:
  - Header with buttons: Grid with back button, title, action buttons (e.g., search) (always reference TimelinePage)
  - Header without buttons: Grid with back button and title only (SettingsPage example)
- **Media**: CachedImage for images, platform-specific handling for videos (always reference Content.xaml and Media.xaml)
- **Text input**: text input (always reference TextContentView.xaml, EditPostPage)
- **Media attachment**: always reference both EditPostPage.xaml.cs and EditCommentPage.xaml.cs implementations
- **Responsive design**: SizeChanged events for phone/tablet adaptive layouts (always reference TimelinePage)

### Dependencies (MAUI)

- CommunityToolkit.Maui/Mvvm for MVVM support
- UraniumUI.Icons/Material for icons
- Syncfusion.Maui.Toolkit for advanced controls
- FFImageLoading.Maui for image caching
- Plugin.Firebase.CloudMessaging for push notifications

## Build Notes

- Ignore iOS build errors on Windows.
- `XALNS7015` errors (Writing mixed-mode assemblies is not supported) during `net10.0-android` builds can be ignored — the build still succeeds.
- For build verification, always use the `net10.0-android` target framework.