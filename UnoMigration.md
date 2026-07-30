# History MAUI -> Uno 마이그레이션 계획

단계적 마이그레이션 전략. `History.Uno` 프로젝트는 MAUI Embedding(`UnoFeatures: MauiEmbedding`)을 활성화한 상태로 시작하며, 플랫폼별 기본 서비스부터 페이지 하나씩 점진적으로 Uno 네이티브로 옮긴다.

## 전략 원칙

- **MAUI Embedding 유지 항목**: `SuggestingBox.MAUI`, `Syncfusion.Maui.ImageEditor`는 Uno 대체품이 없으므로 `History.Uno.MauiControls` 프로젝트에 MAUI ContentView로 남기고 `MauiHost`로 호스팅한다.
- **Uno 네이티브로 이전하는 항목**: 페이지, ViewModel, 스타일, 네비게이션, 서비스 대부분은 WinUI/Uno Toolkit 컨트롤로 재작성한다.
- **History.Commons 재사용**: `ApiHandler`, `Result`, DTO, Enums, Interfaces는 플랫폼 독립적이므로 Uno 프로젝트에서 그대로 참조한다.
- **대상 플랫폼**: iOS, Android만 타겟. `net10.0-android;net10.0-ios` (Skia 렌더링). Windows/Desktop은 별도 네이티브 클라이언트로 분리 (아래 참조).
- **Windows 네이티브 클라이언트 분리**: Windows는 Uno 프로젝트에 포함시키지 않고, MAUI Embedding이 불필요해진 시점에 별도 WinUI 3 / WinAppSDK 네이티브 프로젝트로 제작. History.Commons를 공유하여 API/DTO/비즈니스 로직은 재사용하되 UI는 WinUI 컨트롤로 처음부터 작성. 사유: (1) Uno.Templates 6.6.33 + .NET 10 조합에서 WinAppSDK 타겟 빌드 에러(`CS0619: TemplatedView.Children obsolete`) 발생, (2) Windows는 Uno Skia 렌더링보다 WinUI 네이티브가 성능/통합 면에서 유리, (3) 모바일과 데스크톱은 UI 패턴 자체가 다름.
- **네비게이션**: 초기엔 WinUI Frame 네비게이션(`-nav blank`)으로 시작, 이후 Uno Extensions Region Navigation로 업그레이드 가능.
- **테마**: Uno Fluent(`-theme fluent`) - Material 대비 빌트인 컨트롤 커버리지가 넓고 WinUI 네이티브와 일치. MAUI의 UraniumUI.Material과 시각적 차이는 있지만 컨트롤 안정성 우선.

## 현재 MAUI 프로젝트 구성 (이전 대상 인벤토리)

### 페이지 (38)
AddFriendsPage, AppleLoginPage, AppShell, BlockedFriendsPage, BookmarkedPostsPage, CreateStickerPage, DiscoveryOptionSelectUsersPage, EditCommentPage, EditPostPage, FriendListPage, FullScreenMediaViewerPage, IgnoredFriendsPage, ImageEditorPage, InAppBrowserPage, InteractionsPage, KakaoStoryLoginPage, KakaoStoryRewritePage, LoginPage, MainPage, MessagePage, MessagesPage, ModerationRecordsPage, MorePage, NotificationsPage, PendingFriendRequestsPage, PollResultsPage, PollVotersPage, PostPage, PublicPostPage, RegisterPage, SearchPostsPage, SettingsPage, StickerDetailPage, StickersPage, TimelinePage, UserPage, WaitingFriendRequestsPage, WriteMessagePage

### ViewModels (35)
CommentViewModel, ContentTemplateSelector, ExternalUriContentViewModel, FriendshipViewModel, FullScreenMediaContentViewModel, FullScreenMediaContentViewModelSelector, IContentViewModel, ImageViewModel, IMediaViewModel, InteractionViewModel, MediaAttachmentViewModel, MediaContentViewModel, MediaTemplateSelector, MentionStickerViewModel, MentionsViewModel, MentionUserViewModel, MessageViewModel, ModerationRecordViewModel, NotificationViewModel, PollContentViewModel, PollOptionViewModel, PollResultOptionViewModel, PollVoterViewModel, PostViewModel, ProfileViewModel, PublicPostViewModel, RepostViewModel, SelectUserViewModel, StickerAssetViewModel, StickerContentViewModel, StickerViewModel, TextTypeContentsViewModel, TimelineTemplateSelector, VideoViewModel, WrappedMediaContentsViewModel

### DataTypes / Messaging (23)
ApplePostViewModelTapMessage, AppleVideoUnloadedMessage, CommentTappedMessage, FullScreenMediaTappedMessage, FullScreenPageNavigationMessage, KeyboardSizeMessage, LoadingStateChangedMessage, MediaFile, MentionEditorNewLineMessage, NotificationFriendUserReadMessage, NotificationMessageReadMessage, NotificationPostReadMessage, NotificationsMessage, NotificationsReadAllMessage, PostDraft, PostPinnedMessage, PostUnbookmarkedMessage, ResizeCarouselViewMessage, ResizeMediaCarouselViewMessage, SelectUserSelectionMessage, SpanChangedMessage, UserUnselectedMessage, ValueDeletedMessage

### Enums (2)
InteractionType, PostType

### Helpers (9)
AesCryptoHelper, AndroidMediaPickerHelper, AppleSwipeGestureHelper, CollectionViewHelper, KoreanHelper, LayoutHelper, MentionHelper, ProfanityFilterHelper, WebViewCookieHelper

### Converters (2)
PollConverters, UnreadBackgroundConverter

### Behaviors (1)
SwipeToCloseBehavior

### Auth (3) - Google OAuth
GoogleAuthService.Android.cs, GoogleAuthService.iOS.cs, IGoogleAuthService.cs

### KakaoStory (3)
DataTypes.cs, KakaoStoryApiHandler.cs, KakaoStoryUtils.cs

### ThirdParty (2)
PanPinchContainer, StaggeredLayout

### Resources
- AppIcon, Fonts, Images, Raw, Splash
- Styles (22): Colors, Styles, Comment, Content, Friendship, Interaction, Media, Post, Profile, PublicPost, Repost, SharedPost

### Platforms
- **Android**: MainActivity, MainApplication, BootCompletedReceiver, TokenRefreshService, AndroidManifest, GarbageCollector.txt
- **iOS**: AppDelegate, CustomShellRenderer, CustomTabBarAppearanceTracker, Program, Entitlements.plist, Info.plist, StaggeredItemsViewLayout, TabBarHeightChangedMessage

### 의존성
| 패키지 | 버전 | Uno 대응 |
|---|---|---|
| CommunityToolkit.Maui | 14.1.0 | MAUI Embedding 영역만 유지, Uno 본체는 CommunityToolkit.Mvvm |
| CommunityToolkit.Maui.MediaElement | 9.0.0 | Uno `MediaPlayerElement` (`UnoFeature: MediaPlayerElement`) |
| CommunityToolkit.Mvvm | 8.4.2 | 그대로 사용 (GlobalUsings에 이미 포함) |
| AnimatedWebP.FFImageLoading.Maui | 1.4.1 | Uno `Image` + Skia / WebP 지원 검토 |
| Microsoft.Maui.Controls | 10.0.41 | MAUI Embedding용으로만 유지 |
| Plugin.Firebase.CloudMessaging | 4.0.1 | 플랫폼별 FCM 직접 연동 또는 Uno 플러그인 검토 |
| Syncfusion.Maui.ImageEditor | 34.1.32 | **MAUI Embedding 유지** |
| Syncfusion.Maui.Toolkit | 1.0.10 | **MAUI Embedding 유지** |
| UraniumUI.Icons.FontAwesome | 3.0.0 | Uno `FontIcon` + Fluent Icons로 대체 |
| UraniumUI.Icons.MaterialSymbols | 3.0.0 | Uno `FontIcon` + Material Symbols 폰트 |
| UraniumUI.Material | 2.15.0 | Uno Material 테마로 대체 |
| Xamarin.Build.Download | 0.11.4 | MAUI Embedding 영역만 |
| Xamarin.MediaGallery | 3.0.0 | WinUI `FileOpenPicker` / 플랫폼별 Media Picker |
| SuggestingBox.Maui | (외부) | **MAUI Embedding 유지** |
| History.Commons | (프로젝트) | 그대로 참조 |

---

## 단계별 체크리스트

### 1단계: 기본 서비스 마이그레이션

Uno 프로젝트에 MAUI 앱의 공통 인프라를 올린다. 이 단계가 끝나면 API 호출, 공유 상태, 설정, 네비게이션 뼈대가 동작한다.

- [ ] `History.Commons` 프로젝트 참조 추가
- [ ] `Constants.cs` 이전 (플랫폼 독립 상수)
- [ ] `Shared.cs` 이전 (`ApiHandler`, `UserId`, `MyRank`, `Friends`, `LastUsedPostDiscoveryOption`)
- [ ] `Configuration` 래퍼 이전 (MAUI `Configuration.GetValue<string>` → Uno `IOptions<AppConfig>` / `UseConfiguration`)
- [ ] `App.ExecuteRequestAsync` / `ExecuteRequestAsync<T>` 재구현 (Uno `Dispatcher` / `CoreDispatcher` 사용)
- [ ] `LoadingStateChangedMessage` 메시징 채널 설정 (CommunityToolkit.Mvvm `WeakReferenceMessenger` 그대로 사용 가능)
- [ ] `ErrorType` → HTTP 상태 매핑(`StatusCodeToErrorType`) 이전
- [ ] `Utils.cs` 중 플랫폼 독립 부분 이전 (`GenerateMediaUri`, `GenerateFriendlyTimestamp`, `GenerateTextPreviewFromContents`, `GenerateThumbnailUrlFromContents`, `SanitizeContents`, `GenerateSpanFromTextTypeContents`는 Uno `RichTextBlock`/`TextBlock` Inlines로 재작성 필요)
- [ ] 네비게이션 인프라: `App.PushAsync` / `PopAsync` / `PushModalAsync` / `PopModalAsync`를 WinUI `Frame.Navigate` + 세마포어 기반으로 재구현
- [ ] `App.Page` / `App.TopPage` / `App.Navigation` 정적 접근자 재구현 (Uno `Window.Current.Content` 기반)
- [ ] `MainPage`를 로그인 게이트로 교체 (`CreateWindow` → `Frame.Navigate(typeof(LoginPage))`)
- [ ] `appsettings.json`에 `ApiEndpoint`, `AccessToken`, `RefreshToken`, `Theme` 키 마이그레이션
- [ ] `DataTypes/` 메시지 클래스 23개 이전 (대부분 `ValueChangedMessage<T>` 상속, 플랫폼 독립)
- [ ] `Enums/` 2개 이전 (`InteractionType`, `PostType`)
- [ ] 빌드 통과 (Windows 타겟)

### 2단계: 핵심 페이지 마이그레이션

로그인 → 탭 셸 → 타임라인 → 유저 페이지까지 동작하는 최소 루프를 만든다.

- [ ] `LoginPage.xaml/.cs` → Uno `Page` + WinUI 컨트롤로 재작성
- [ ] `RegisterPage.xaml/.cs` 이전
- [ ] `AppShell` 탭 네비게이션 → Uno `NavigationView` 또는 `TabBar` (Uno Toolkit)로 재구현 (5개 탭: 타임라인, 알림/쪽지, 친구, 더보기, 프로필)
- [ ] `TimelinePage.xaml/.cs` 이전 (당김새로고침, 페이지네이션, 포스트 아이템 템플릿)
- [ ] `PostViewModel` 이전 (가장 복잡한 ViewModel, `IContentViewModel` 체계 포함)
- [ ] `IContentViewModel` 인터페이스 + 구현체들 (`TextTypeContentsViewModel`, `MediaContentViewModel`, `StickerContentViewModel`, `ExternalUriContentViewModel`, `PollContentViewModel`, `WrappedMediaContentsViewModel`)
- [ ] `TimelineTemplateSelector` → Uno `DataTemplateSelector` 재작성
- [ ] `UserPage.xaml/.cs` 이전 (프로필, 미디어, 포스트 목록)
- [ ] `ProfileViewModel` 이전
- [ ] 스타일: `Colors.xaml`, `Styles.xaml`, `Post.xaml`, `Profile.xaml`, `Content.xaml` → Uno `ResourceDictionary`로 변환
- [ ] 이미지 로딩: FFImageLoading → Uno `Image` (Skia) 마이그레이션 패턴 확립
- [ ] 빌드 + Windows에서 핵심 루프 동작 확인

### 3단계: 나머지 페이지 마이그레이션

포스트 상세/편집, 친구, 메시지, 스티커, 설정, 발견, 검색 등 모든 페이지를 옮긴다.

- [ ] 포스트: `PostPage`, `PublicPostPage`, `EditPostPage`, `EditCommentPage`, `SearchPostsPage`, `BookmarkedPostsPage`
- [ ] 포스트 부가: `PollResultsPage`, `PollVotersPage`, `InteractionsPage`, `FullScreenMediaViewerPage`, `ImageEditorPage`(**MAUI Embedding 유지**), `InAppBrowserPage`
- [ ] 친구: `FriendListPage`, `AddFriendsPage`, `WaitingFriendRequestsPage`, `PendingFriendRequestsPage`, `IgnoredFriendsPage`, `BlockedFriendsPage`, `DiscoveryOptionSelectUsersPage`
- [ ] 메시지: `MessagesPage`, `MessagePage`, `WriteMessagePage`
- [ ] 알림: `NotificationsPage`
- [ ] 스티커: `StickersPage`, `StickerDetailPage`, `CreateStickerPage` (스티커 선택 UI 포함)
- [ ] 설정/더보기: `SettingsPage`, `MorePage`, `ModerationRecordsPage`
- [ ] 나머지 ViewModel 30개 이전
- [ ] 나머지 스타일: `Comment.xaml`, `Friendship.xaml`, `Interaction.xaml`, `Media.xaml`, `PublicPost.xaml`, `Repost.xaml`, `SharedPost.xaml`
- [ ] `ContentViews/`: `DataTemplatePresenter`, `EditPost/StickerCollectionView`(**MAUI Embedding 유지 - SuggestingBox**), `EditPost/TextContentView`, `SquareView`
- [ ] `MentionsViewModel` / `MentionStickerViewModel` 이전 (SuggestingBox 연동 부분은 MAUI Embedding)
- [ ] `Converters/`, `Behaviors/` 이전
- [ ] 빌드 + 전체 페이지 동작 확인

### 4단계: 플랫폼별 기능 마이그레이션

iOS/Android 네이티브 기능을 Uno 플랫폼 폴더로 옮긴다.

- [ ] **Firebase Cloud Messaging**: Android `BootCompletedReceiver`, `TokenRefreshService`, `MainActivity` FCM 초기화 → Uno `Platforms/Android` MainActivity 재구현; iOS `AppDelegate` FCM 초기화 → Uno `Platforms/iOS` AppDelegate 재구현
- [ ] **Google OAuth**: `Auth/GoogleAuthService` Android/iOS 분할 구현을 Uno 플랫폼별로 이전 (`Platforms/Android`, `Platforms/iOS`)
- [ ] **Apple OAuth**: `AppleLoginPage` + `Platforms/iOS` Apple Sign-In
- [ ] **카카오스토리**: `KakaoStory/` 3파일, `KakaoStoryLoginPage`, `KakaoStoryRewritePage` 이전
- [ ] **미디어 피커**: `AndroidMediaPickerHelper` → Uno 플랫폼별 Media Picker; `Xamarin.MediaGallery` 대체
- [ ] **키보드 감지**: `KeyboardSizeMessage` (Android `WindowInsetsListener`, iOS `UIKeyboard` 알림) → Uno 플랫폼별 재구현
- [ ] **공유 인텐트 (Android)**: `MainActivity.HandleIntent` (ActionSend/ActionSendMultiple) → Uno `Platforms/Android` MainActivity
- [ ] **iOS 커스텀 렌더러**: `CustomShellRenderer`, `CustomTabBarAppearanceTracker`, `StaggeredItemsViewLayout` → Uno 네이티브 컨트롤 또는 플랫폼별 구현
- [ ] **백 버튼 처리**: `AppShell.OnBackButtonPressed` (Android 더블탭 종료) → Uno `Platforms/Android` BackButton 처리
- [ ] **앱 아이콘/스플래시**: `Resources/AppIcon`, `Resources/Splash` → Uno `Assets`, `app.manifest`, `Package.appxmanifest`
- [ ] **폰트**: `Resources/Fonts` (OpenSans, MaterialSymbols, FontAwesome) → Uno `Assets/Fonts`
- [ ] **AndroidManifest.xml**, **Info.plist**, **Entitlements.plist** 마이그레이션
- [ ] **Firebase 설정**: `google-services.json` (Android), `GoogleService-Info.plist` (iOS) 연동
- [ ] **서명**: Android `signing.keystore`, iOS 인증서/프로비저닝 설정
- [ ] 빌드 + iOS/Android 디바이스 동작 확인

### 5단계: 최종화

MAUI Embedding 잔재 정리, 서드파티 정리, 품질 점검.

- [ ] MAUI Embedding 사용 항목 정리: `SuggestingBox.MAUI`는 `History.Uno.MauiControls`에 ContentView로 유지, `Syncfusion.Maui.ImageEditor`도 동일
- [ ] MAUI Embedding 불필요 항목(이미 Uno 네이티브로 이전 완료)은 `MauiControls` 프로젝트에서 제거
- [ ] `ThirdParty/StaggeredLayout` → Uno `ItemsRepeater` / `ItemsView` 커스텀 레이아웃으로 대체 검토
- [ ] `ThirdParty/PanPinchContainer` → Uno `ZoomContentControl` (Uno Toolkit) 또는 커스텀 조작 처리
- [ ] Windows 네이티브 클라이언트 별도 프로젝트 생성 (WinUI 3 / WinAppSDK, `History.Commons` 공유, UI는 WinUI로 처음부터 작성)
- [ ] `Directory.Packages.props` Central Package Management 정리
- [ ] 속도 제한, CORS, 인증 미들웨어 점검
- [ ] 성능 프로파일링 (Skia 렌더링, 이미지 캐싱)
- [ ] `History.slnx`에 `History.Uno` 프로젝트 추가
- [ ] `AGENTS.md` 업데이트 (History.Uno 섹션 추가)
- [ ] 최종 iOS/Android 출시 빌드 확인

---

## 메모

- **Uno 템플릿 버전**: 6.6.33 설치됨. WinAppSDK(`net10.0-windows10.0.26100`) 타겟에서 MAUI Embedding + .NET 10 조합 빌드 에러(`CS0619: TemplatedView.Children obsolete`) 발생하여 Windows 타겟 제외. 모바일(iOS/Android)만 타겟.
- **`FormattedString`/`Span`**: MAUI의 `Span` + `GestureRecognizers` 패턴은 Uno `RichTextBlock` + `Hyperlink`/`Inline`로 재작성 필요 (`Utils.GenerateSpanFromTextTypeContents`).
- **`Shell` → `NavigationView`**: MAUI Shell의 `TabBar`/`Tab`/`ShellContent` 계층은 Uno `NavigationView` (`PaneDisplayMode="Bottom"`) + `Frame` 네비게이션으로 매핑.
- **Fluent 테마**: `MaterialToolkitTheme` 대신 `XamlControlsResources` + `ToolkitResources`만 로드. 추가 컬러 오버라이드는 `Styles/` 폴더에서 관리.
- **`Preferences`**: MAUI `Preferences.Set/Get` → Uno `ApplicationData.Current.LocalSettings` 또는 `IConfiguration` + 파일 저장.
- **`MainThread.BeginInvokeOnMainThread`**: Uno `Dispatcher.RunAsync` / `CoreDispatcher.RunAsync`.
- **`CommunityToolkit.Maui.Alerts` (Toast)**: Uno `InfoBar` 또는 플랫폼별 Toast.
- **`DisplayAlertAsync`**: Uno `ContentDialog`.
- **`AppInfo.Current.VersionString`**: Uno `Package.Current.Id.Version`.