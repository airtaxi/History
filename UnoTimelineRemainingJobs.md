# History.Uno 타임라인 마이그레이션 — 1차 완료 및 2차 잔여 작업

> **목적**: MAUI `TimelinePage` → Uno 마이그레이션 1차 범위를 마무리하고, 1차에서 의도적으로 제외/대체한 항목과 후속 페이지 이전 시 연결해야 할 작업을 기록한다.

## 1차 완료 범위 (이번 작업)

- **페이지**: `Pages/TimelinePage.xaml(.cs)` — 헤더(검색 버튼), `RefreshContainer` + `ScrollViewer` + `ItemsRepeater` + `CommunityToolkit.WinUI.Controls.StaggeredLayout`(적응형 열 개수), 글쓰기/맨위로 FAB, 로딩 `ProgressRing`
- **ViewModels (20개)**: `PostViewModel`, `RepostViewModel`, `TimelineContentsViewModel`, `IContentViewModel` 체인(`TextTypeContentsViewModel`, `MediaContentViewModel`, `StickerContentViewModel`, `ExternalUriContentViewModel`, `PollContentViewModel`, `WrappedMediaContentsViewModel`, `PollOptionViewModel`, `ImageViewModel`, `VideoViewModel`, `TextContentRun`/`TextContentRunKind`), `CommentViewModel`, `InteractionViewModel`, `TimelineTemplateSelector`, `ContentTemplateSelector`, `MediaTemplateSelector`, `IMediaViewModel`
- **스타일 (8개)**: `Colors.xaml`(테마 브러시/컨버터), `Icons.xaml`(Fluent 글리프), `Media.xaml`(Image/Video 템플릿), `Content.xaml`(미디어/래핑미디어/스티커/텍스트/투표/외부링크/타임라인슬롯 템플릿), `Comment.xaml`, `SharedPost.xaml`, `Repost.xaml`, `Post.xaml` — App.xaml에 병합
- **컨트롤**: `Controls/TextTypeContentsView` — `RichTextBlock` + `Hyperlink`(URL/프로필/해시태그) 런 렌더링, 타임라인 텍스트 트렁케이션("... 더보기")
- **인프라**: `App.DisplayActionSheetAsync` / `App.DisplayPromptAsync` (ContentDialog 기반), `Utils.GenerateContentViewModels` / `GenerateTextContentRuns` / `GetDiscoveryOptionGlyph`(Fluent) / `GetGlobalAppTheme`, `BoolToVisibilityConverter` / `BoolToPrimaryColorConverter`
- **패키지**: `CommunityToolkit.WinUI.Controls.Primitives` 8.2.251219, `UnoFeatures: MediaPlayerElement`
- **빌드**: Android/iOS 0 에러

## 2차 잔여 작업

### 1. 미이전 페이지 네비게이션 연결 (TODO: 알럿 대신)

현재 아래 코드는 `App.DisplayAlertAsync("...아직 지원되지 않습니다...")` 알럿으로 대체되어 있다. 해당 페이지가 Uno로 이전되면 정식 네비게이션으로 교체한다.

| 위치 | TODO 내용 |
|---|---|
| `PostViewModel.HandleTapAsync` | `PostPage`로 이동 (새 `PostViewModel(Post, PostType.Unwrapped)` 생성 후 푸시) |
| `PostViewModel` 공개범위/수정/공유 | `EditPostPage` 이동 (수정: `EditPostPage(Post, false)`, 공유: `EditPostPage(Post, true)`) |
| `PostViewModel.HandleReactionTapAsync` / `HandleSharedTapAsync` / `HandleRepostTapAsync` | `InteractionsPage` 이동 (MAUI 원본의 FriendshipViewModel 변환 로직 참조) |
| `CommentViewModel` 댓글 수정 / 좋아요 목록 | `EditCommentPage` / `InteractionsPage` 이동 |
| `PollContentViewModel.ViewResultsAsync` | `PollResultsPage(_postId, PollId)` 이동 |
| `StickerContentViewModel.NavigateToStickerDetailAsync` | `StickerDetailPage(result.Value)` 이동 (GetSticker API 호출부는 이미 구현됨) |
| `MediaContentViewModel.HandleTapAsync` / `HandleOverlayTap`(전체화면 전환) | `FullScreenMediaViewerPage` 이동 (`FullScreenMediaContentViewModel`도 함께 이전 필요) |
| `TimelinePage` 검색 버튼 / 글쓰기 FAB | `SearchPostsPage` / `EditPostPage` 이동 |
| `TextTypeContentsView` 해시태그 | `EditPostPage([hashtag])` 이동 |
| `PostViewModel` "게시글 홍보" | `PublicPostsPage.ShouldRefresh = true` 연동 (PublicPostsPage 이전 후) |

### 2. 미디어 렌더링 개선

- **래핑 미디어 높이 계산**: MAUI의 `CarouselViewHeight`(이미지 원본 비율 기반, `ResizeCarouselViewMessage`/`SpanChangedMessage` 연동) 로직이 제거되고 고정 `Height="400"`으로 대체됨. 1:1 비율 유지 + 원본 비율 적응형 높이 복원 필요.
- **FlipView 라운드 클리핑**: `Border CornerRadius="6"`으로 대체했으나 Uno에서 Border가 자식을 클리핑하지 않을 수 있음. 확인 후 `RectangleGeometry` Clip(`UIElement.Clip`은 Uno에서 `RectangleGeometry`만 허용) 또는 `Composition` 클리핑으로 대체.
- **MediaPlayerElement 재활용**: `HandleOverlayTap`으로 인라인 재생 전환 후 `ItemsRepeater` 가상화로 요소가 재활용될 때 재생 상태/정리 처리 미구현. `WrappedMediaContentsViewModel.Unloaded`류 훅 필요.
- **비디오 스포일러**: `UnloadedCommand`(MAUI)가 제거됨 — 스포일러 상태 초기화는 2차에서.
- **애니메이션 프로필 미디어**: `ImageViewModel.IsAnimated` 프로퍼티 제거 — Uno Image가 WebP 애니메이션을 네이티브 지원하므로 문제없음. 단 `PersonPicture.ProfilePicture`가 애니메이션을 재생하는지 확인 필요.

### 3. 텍스트 콘텐츠

- **롱프레스 복사**: MAUI `TouchBehavior LongPressCompleted`(텍스트 클립보드 복사) 미구현. `PointerPressed` 타이머 또는 `Microsoft.Xaml.Behaviors`로 재현.
- **링크 열기**: `Hyperlink.Click` → `Launcher.LaunchUriAsync`(시스템 브라우저)로 구현됨. `InAppBrowserPage`(앱 내 브라우저)로 열도록 하려면 교체.
- **`CommentViewModel.IsLongPressed` / `CommentTappedMessage`**: Uno에서는 롱프레스가 없어 항상 탭으로 처리됨(주석 참조). 롱프레스 구현 시 원래 분기(롱프레스 후 탭 무시) 복원.

### 3. 스타일 / 플랫폼

- **상태바**: MAUI `StatusBarBehavior`(주황색) → `utu:StatusBar.Background="{StaticResource PrimaryColor}"`로 대체됨. 다른 페이지에도 동일 패턴 적용.
- **진동(HapticFeedback)**: `PostViewModel.HandleReactionAsync`, `ExternalUriContentViewModel.HandleLongPressAsync`에서 제거됨. Uno 플랫폼별(Android `Vibrator`, iOS `UIImpactFeedbackGenerator`) 구현 시 복원.
- **기본 프로필 이미지**: `Constants.DefaultProfileImageFileName`("default_profile_image.jpg") 에셋이 Uno에 없음 → `PersonPicture`가 `DisplayName` 이니셜로 폴백함. 에셋 추가 또는 제거 결정 필요.
- **폰트**: `MaterialSymbols`/`FontAwesome` 폰트 파일이 Uno `Assets/Fonts`에 없음(현재 Fluent `SymbolThemeFontFamily`로 대체됨). 커스텀 폰트 유지가 필요하면 `Assets/Fonts` 추가.
- **`InteractionTemplate`(Interaction.xaml)**: `PostPage` 상세용 인터랙션 목록 템플릿 — `InteractionsPage` 이전 시 함께 이전. `InteractionViewModel.Glyph/Brush/IconSize`는 이미 Uno용(Fluent)으로 준비됨.
- **`PostContentTemplate` / `PostCommentTemplate` / 댓글 전체 렌더링**: `PostPage` 이전 시 필요한 상세 템플릿. 최신 댓글 미리보기(`CommentTemplate`)는 1차에 포함됨.
- **`ProfileTemplate`(Profile.xaml)**: `TimelineTemplateSelector`에서 제거됨(`ProfileViewModel`은 타임라인에 미사용). `UserPage` 이전 시 복원.

### 4. 네비게이션 / 동작

- **탭 전환 상태 유지**: `NavigationCacheMode="Required"`로 `TimelinePage` 인스턴스를 캐시함. `MainPage`의 `ContentFrame.Navigate`가 동일 타입 재탐색 시 캐시된 페이지를 재사용하는지 실제 기기에서 확인 필요(스크롤 위치 유지).
- **`TimelinePage.ShouldRefresh`**: 글 작성/수정 후 새로고침 트리거 — `EditPostPage` 이전 시 연동.
- **`SpanChangedMessage`**: 열 개수 변경 시 래핑 미디어 높이 재계산용 — 2-2 미디어 높이 로직 복원 시 함께 사용.

### 5. 검증 필요 항목

- **StaggeredLayout 열 개수**: `DesiredColumnWidth = width / span - 4` 산식을 실제 기기(폰/태블릿)에서 확인. Android 측정값 차이로 열 개수가 어긋나면 `madome-uno`처럼 Android 보정 필요.
- **테마**: Material 라이트/다크에서 카드 배경(`AppCardBackgroundBrush`), 텍스트, 투표 색상 대비 확인.
- **`RefreshContainer`**: 당김새로고침 동작, deferral 기반 종료 확인.
- **`PersonPicture`**: 프로필 이미지 로딩 실패 시 이니셜 폴백, 다크 테마 색 확인.
- **투표**: `ProgressBar Maximum="1"` + `Percentage`(0~1) 바인딩, 복수 선택 토글 동작 확인.
- **iOS**: `MediaPlayerElement` 인라인 재생 동작(MAUI에서 iOS는 전체화면 전용이었음) — Uno에서 인라인 재생이 안 되면 `HandleOverlayTap`을 전체화면 TODO 알럿으로 되돌릴 것.

## 관련 문서

- `UnoMigration.md` — 전체 마이그레이션 계획 (2단계 체크리스트의 TimelinePage/PostViewModel 항목이 본 문서의 1차로 완료됨)
- `UnoInterfaceMigration.md` — UI 컨트롤 매핑 기준
