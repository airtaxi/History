# History MAUI → Uno UI 요소 마이그레이션 매핑

`UnoMigration.md`의 단계적 마이그레이션을 수행할 때 각 XAML 컨트롤을 어떤 Uno/WinUI 컨트롤로 치환해야 하는지 기준을 제공한다. 공식 Uno Platform 문서(XF 마이그레이션 매핑, ItemsRepeater/RefreshContainer/ZoomContentControl/TabBar/AutoLayout/MediaPlayerElement/WebView2 등 개별 컨트롤 문서)와 WinUI/MAUI 지식을 결합해 정리했다.

## 이 문서의 작성 방법

`History.MobileClient`의 모든 XAML 파일(56개: Pages 45 + ContentViews 3 + Resources/Styles 13 + App.xaml)을 실제로 조사해 **실제 사용되는 컨트롤만** 기재했다. 사용되지 않는 MAUI 컨트롤(`AbsoluteLayout`, `RelativeLayout`, `TableView`, `Stepper`, `IndicatorView`, `FlyoutPage`, `ListView`(요소), `Frame`, `Slider`, `RadioButton`, `DatePicker`, `TimePicker`, `SwipeView`, `ToolbarItem`, `Menu`/`MenuFlyoutItem`, `Line`/`Path`/`Polygon`/`Polyline` Shapes, `Pan`/`Pinch`/`Swipe` `GestureRecognizer`, `DataTrigger`/`EventTrigger`/`MultiTrigger`)은 매핑에서 제외했다.

## 문서의 범위와 한계

- 본 문서는 **UI 컨트롤 단위의 1:1 대응 가능 여부**를 분류하는 것이 목적이다. 비즈니스 로직, ViewModel, 메시징, DI, 인증 흐름 등은 `UnoMigration.md`에서 다룬다.
- Uno 프로젝트는 `UnoFeatures: MauiEmbedding`을 켠 상태로 시작하며, **테마는 Uno Material(`MaterialToolkitTheme`)을 사용**(`TabBar` 사전 스타일이 Material/Cupertino 패키지에만 존재하므로 NavigationShell 패턴 적용을 위함). MAUI Embedding 영역에 남기는 컨트롤(`SuggestingBox`, `SfImageEditor`, `SfDateTimePicker`)은 "MAUI Embedding 유지"로 표기한다.
- 대상 플랫폼은 iOS/Android(`net10.0-android;net10.0-ios`, Skia 렌더링)이며, Windows는 별도 WinUI 3 네이티브 클라이언트로 분리한다(`UnoMigration.md` 11항 참조).
- **필수 사전 작업**: Uno/WinUI XAML·C# 작업은 WinUI와 동일한 API/패턴을 쓰므로, 마이그레이션 작업 전에 반드시 `/csharp-winui-projects` 스킬을 사전에 로드해야 한다(`$csharp-code-style` 스킬도 함께). XAML 작성 후 XAML 포맷터 실행, Fluent 아이콘은 `fluent-icons` MCP로 검색, `x:Uid`/`.resw` 지역화 규칙 등이 모두 이 스킬에 포함되어 있다.

## 분류 기호

| 기호 | 의미 |
|---|---|
| **1:1** | MAUI 컨트롤과 Uno/WinUI 컨트롤이 이름·API·동작이 거의 일치하여 XAML을 거의 그대로 옮겨도 됨. 프로퍼티명/일부 동작만 조정. |
| **대응** | 1:1은 아니지만 Uno에 명확한 대체 컨트롤이 존재. 매핑 규칙을 알면 기계적 변환이 가능. |
| **부분** | 핵심 기능은 대응되지만 MAUI 특유 프로퍼티/자식 요소 일부가 대응되지 않아 보조 구현이 필요. |
| **대응 없음** | Uno에 직접 대응 컨트롤이 없어 재설계·커스텀 구현·MAUI Embedding 중 택일해야 함. |

---

## 1. 레이아웃 컨트롤

| MAUI (사용처) | Uno / WinUI | 분류 | 비고 |
|---|---|---|---|
| `Grid` (49파일 213회) | `Grid` | **1:1** | `RowDefinitions`/`ColumnDefinitions`/`Grid.Row`/`Grid.Column` 문법 동일. `ColumnSpacing`/`RowSpacing`은 Grid 자체에 없으므로 자식 `Margin` 또는 `AutoLayout`으로 흡수. |
| `HorizontalStackLayout` (37파일 75회) | `StackPanel` (`Orientation="Horizontal"`) | **1:1** | `Spacing` 프로퍼티 유지. |
| `VerticalStackLayout` (28파일 65회) | `StackPanel` (`Orientation="Vertical"`) | **1:1** | 동일. |
| `FlexLayout` (`CreateStickerPage.xaml` 1회) | `Grid` + `Responsive` 트리거 / `VariableSizedWrapGrid` / `AutoLayout` | **대응 없음** | 공식 XF 마이그레이션 가이드 "직접 대응 없음". 프로젝트 유일 사용처는 `AssetsFlexLayout`(`Direction="Row" Wrap="Wrap"`)로 래핑 그리드 → `ItemsWrapGrid` 또는 `Grid`+반응형으로 재설계. 영향 범위 좁아 1파일 재설계로 종결 가능. |
| `ScrollView` (8파일 11회, `PostPage.xaml` 3회 등) | `ScrollViewer` | **1:1** | `Orientation`/`HorizontalScrollBarVisibility`/`VerticalScrollBarVisibility` 매핑. `RefreshView`를 `ScrollView` 위에 얹던 패턴은 `RefreshContainer` + `ScrollViewer`로 변경. |
| `Border` (26파일 54회) | `Border` | **1:1** | MAUI `Border`는 Wrapper 성격, Uno/WinUI `Border`가 더 기본적. 프로퍼티 거의 일치. |
| `ContentView` (상속: `DataTemplatePresenter`, `SquareView`, `TextContentView`, `StickerCollectionView`) | `UserControl` | **1:1** | 재사용 컴포넌트 단위. `x:Name` + 코드비하인드 접근 패턴 동일. |

> **권장 기준**: 페이지 뼈대는 `Grid`/`StackPanel`/`ScrollViewer` 조합이 1:1이므로 우선 이 세 컨트롤로 옮긴다. `FlexLayout` 1사용처(`CreateStickerPage`)는 `ItemsWrapGrid` 또는 반응형 `Grid`로 재설계한다. `AutoLayout`(Uno Toolkit)은 Figma Auto-Layout과 동일한 `Spacing`/`Padding`/`Justify`/`PrimaryAxisAlignment` 모델이라 복잡한 흐름을 대체하기 좋다(공식 `AutoLayoutControl.md`).

---

## 2. 텍스트 컨트롤

| MAUI (사용처) | Uno / WinUI | 분류 | 비고 |
|---|---|---|---|
| `Label` (47파일 234회) | `TextBlock` | **1:1** | `Text`/`TextColor`→`Foreground`/`FontFamily`/`FontSize`/`FontAttributes`→`FontWeight`/`FontStyle`/`HorizontalTextAlignment`→`TextAlignment` 매핑. `LineBreakMode`/`MaxLines`/`LineHeight` 존재. |
| `Label` + `FormattedString`/`Span` (5파일 14회: `Post.xaml` 6, `InviteCodeRequestsPage` 4, `Repost.xaml` 2, `Comment.xaml` 1, `InviteCodesPage` 1) | `RichTextBlock` + `Paragraph`/`Run`/`Span`/`Hyperlink` | **부분** | 텍스트 조각·하이퍼링크 표현은 `RichTextBlock`으로 옮김. **`Span.GestureRecognizers`는 Uno에 대응 없음** → `Hyperlink` 클릭 이벤트 또는 `TextBlock` 위에 투명 `Button` 오버레이로 재현. `Utils.GenerateSpanFromTextTypeContents` 재작성 필요. |
| `Span` + `TapGestureRecognizer` (`Repost.xaml` 1회 — `RepostedUserNickname` 탭 → `HandleRepostedUserTapCommand`) | `RichTextBlock` + `Hyperlink` + `Click` | **부분** | 프로젝트 유일의 `Span.GestureRecognizers` 사용처. `Hyperlink.NavigateUri`/`Click` 이벤트로 재현, 커맨드 바인딩은 `Hyperlink.Click` → `Command` 연결. |
| `Entry` (3파일 4회: `CreateStickerPage` 2, `StickersPage` 1, `RegisterPage` 1) | `TextBox` | **대응** | 단일줄 입력. `Placeholder`→`PlaceholderText`, `IsPassword`→`PasswordBox` 별도 컨트롤, `Keyboard`→`InputScope`. `Uno.Toolkit.UI.InputExtensions`로 키보드 동작·포커스 흐름 제어 권장. |
| `Editor` (2파일 2회: `CreateStickerPage`, `WriteMessagePage`) | `TextBox` (`AcceptsReturn="True"`, `TextWrapping="Wrap"`) | **대응** | 전용 `Editor` 없음. `TextBox` 멀티라인 설정으로 흡수. `AutoSize`는 별도 처리. |

> **권장 기준**: 모든 텍스트 입력은 `TextBox`(비밀번호는 `PasswordBox`)로 통일하고 `Uno.Toolkit.InputExtensions` 부착 프로퍼티로 키보드·포커스·Return 동작을 잡는다. 텍스트 조각+링크는 `RichTextBlock`으로 옮기되, `Span.GestureRecognizers` 기반의 탭 인터랙션(`Repost.xaml` 1곳)은 `Hyperlink.Click` → 커맨드 연결 또는 투명 `Button` 오버레이로 재현한다.

---

## 3. 버튼 · 입력 컨트롤

| MAUI (사용처) | Uno / WinUI | 분류 | 비고 |
|---|---|---|---|
| `Button` (10파일 17회) | `Button` | **1:1** | `Text`→`Content`(문자열도 가능), `Command`/`Clicked` 존재. |
| `ImageButton` (`LoginPage.xaml` 2회) | `Button` + `Icon`/`Image` content | **대응** | 전용 `ImageButton` 없음. `Button`에 `ImageIcon`/`FontIcon`/`Image`를 `Content`로. `Uno.Toolkit.CommandExtensions.Icon`으로 아이콘 부착 권장(이 프로젝트 AGENTS.md 규칙). |
| `CheckBox` (2파일 4회: `RegisterPage` 3, `DiscoveryOptionSelectUsersPage` 1) | `CheckBox` | **1:1** | `IsChecked`/`Checked`/`Unchecked` 이벤트 동일. |
| `Switch` (2파일 5회: `EditPostPage` 4, `CreateStickerPage` 1) | `ToggleSwitch` | **대응** | `IsToggled`→`IsOn`. 토글 스위치 모양이 WinUI 네이티브. MAUI `Switch` 단순 토글과 달리 `OnContent`/`OffContent` 라벨 내장 — 사용처에서 라벨이 필요 없으면 `OnContent`/`OffContent`를 빈 문자열로. |
| `ProgressBar` (2파일 2회: `Content.xaml` 1, `PollResultsPage` 1) | `ProgressBar` | **1:1** | `Progress`(0~1)→`Value`(0~100, `Maximum="100"`). `IsIndeterminate` 동일. |
| `ActivityIndicator` (36파일 36회, 거의 모든 페이지 1회씩) | `ProgressRing` (`IsActive="True"`) | **1:1** | `IsRunning`→`IsActive`. `Color`→`Foreground`. |
| `Picker` (`EditPostPage.xaml` 2회) | `ComboBox` | **대응** | `ItemsSource`/`SelectedIndex`/`SelectedItem`/`SelectedIndexChanged`→`SelectionChanged` 매핑. |
| `SearchBar` (4파일 4회: `AddFriendsPage`, `DiscoveryOptionSelectUsersPage`, `FriendListPage`, `SearchPostsPage`) | `AutoSuggestBox` | **대응** | `SearchClicked`/`QuerySubmitted` 매핑. 자동완성 제안도 `AutoSuggestBox`가 더 네이티브. 단순 검색은 `TextBox`+`PlaceholderText`+`CommandExtensions` 패턴도 가능(Chefs 예제). |

> **권장 기준**: 버튼·체크·토글·피커는 WinUI 대응이 있으므로 프로퍼티명만 바꿔 기계적 변환. 아이콘 버튼은 반드시 `Button` + `CommandExtensions.Icon` 패턴(이 프로젝트 AGENTS.md 및 Uno 사용 규칙). `SearchBar` 4사용처는 `AutoSuggestBox`로 통일.

---

## 4. 컬렉션 · 리스트 컨트롤

| MAUI (사용처) | Uno / WinUI | 분류 | 비고 |
|---|---|---|---|
| `CollectionView` (25파일 26회, `DiscoveryOptionSelectUsersPage` 2회 등) | `ListView` 또는 `ItemsRepeater` | **부분** | 핵심 데이터 바인딩/템플릿은 대응. **`EmptyView` 직접 대응 없음** → 조건부 `Visibility`+빈 상태 UI 별도 구현(공식 XF 매핑 가이드). **`RemainingItemsThreshold` 직접 대응 없음** → `ISupportIncrementalLoading` 또는 스크롤 위치 모니터링. `ItemsLayout`은 `ItemsPanelTemplate`으로 변환(아래 `*ItemsLayout` 항목 참조). |
| `CollectionView` + `GridItemsLayout` (3파일 3회: `StickerCollectionView.xaml`, `StickerDetailPage`, `UserPage`) | `ListView` + `ItemsWrapGrid` 또는 `ItemsRepeater` + `ItemsWrapGrid` | **대응** | 그리드 레이아웃 매핑. `Span`/`Orientation` 프로퍼티는 `ItemsWrapGrid` `MaximumRowsOrColumns`/`Orientation`으로. |
| `CollectionView` + `LinearItemsLayout` (`DiscoveryOptionSelectUsersPage.xaml` 1회) | `ListView` + `ItemsStackPanel` 또는 `ItemsRepeater` + `ItemsStackPanel` | **대응** | 선형 레이아웃 매핑. |
| `CollectionView` + `staggered:StaggeredItemsLayout` (4파일 4회: `TimelinePage`, `EditPostPage`, `PublicPostsPage`, `SearchPostsPage`) | `ItemsRepeater` + `CommunityToolkit.WinUI.Controls.StaggeredLayout` (NuGet) | **대응** | Pinterest식 비규격 그리드. `madome-uno` 저장소 검증 결과 **Windows Community Toolkit 공식 패키지 `CommunityToolkit.WinUI.Controls.Primitives`**(8.2.251219, MIT)의 가상화 `StaggeredLayout`을 `ItemsRepeater.Layout`으로 사용하는 것이 정석. 커스텀 패널 작성 불필요. 자세한 패키지/XAML 패턴은 아래 "StaggeredLayout 마이그레이션 상세" 섹션 참조. 핵심 타임라인/발견/검색/편집 페이지가 영향권. |
| `BindableLayout.ItemsSource`/`ItemTemplate`/`ItemTemplateSelector` (5사용처, 전부 `VerticalStackLayout` 호스트: `Content.xaml` 1, `Comment.xaml` 1, `Post.xaml` 2, `ModerationRecordsPage` 1) | `ItemsRepeater` | **대응** | MAUI `BindableLayout`은 가상화 없는 간단 목록. Uno `ItemsRepeater`가 사실상 동일 역할(가상화·`ScrollViewer` 호스트 필요). `DataTemplateSelector` 그대로 사용 가능. |
| `CarouselView` (2파일 3회: `Content.xaml` 2 — `AndroidWrappedMediaContentsTemplate`/`AppleWrappedMediaContentsTemplate`, `FullScreenMediaViewerPage.xaml` 1 — `FullScreenMediaContentTemplate`) | `FlipView` (단일 표시) + `PipsPager` | **부분** | MAUI `CarouselView`는 데이터 바인딩된 컬렉션 회전+`CurrentItem`/`Position` 양방향 바인딩. `FlipView`는 단일 표시+스와이프는 되지만 `CurrentItem` 양방향 바인딩·`Position` 인덱스 API가 다름. `PipsPager`로 인디케이터 흉내(공식 `ReactiveCarousel.md`). 이 프로젝트 래핑 미디어 캐러셀 + 풀스크린 미디어 뷰어가 영향권. `ResizeCarouselViewMessage`/`ResizeMediaCarouselViewMessage` 메시지도 함께 재설계. |
| `RefreshView` (17파일 19회, `PostPage.xaml` 3회 포함) | `RefreshContainer` | **1:1** | 당김새로고침. `RefreshContainer` + 내부 `ScrollViewer` 조합. **iOS/Android/Windows에서 터치 당기기 지원, 기타 타겟은 `RequestRefresh()` 수동 호출**(공식 `RefreshContainer.md`). MAUI `IsRefreshing` 양방향 → `RefreshContainer`는 `Command`/`RequestRefresh()` API. `Refreshing="OnRefreshing"` 이벤트 핸들러는 `RefreshContainer.RefreshRequested` 또는 `RequestRefresh()` 호출 패턴으로 변환. **래핑 패턴 2종**: (1) `RefreshView`>`CollectionView`(13회) → `RefreshContainer`>`ScrollViewer`>`ListView`/`ItemsRepeater`. (2) `RefreshView`>`ScrollView`(3회, `PostPage` 전용) → `RefreshContainer`>`ScrollViewer`. (3) `RefreshView`>`Grid`(7회, 친구 관리 페이지) → `RefreshContainer`>`ScrollViewer`>`Grid`. |
| `DataTemplate` (24파일 42회) | `DataTemplate` | **1:1** | XAML `DataTemplate` 문법 동일. |
| `DataTemplateSelector` 서브클래스 (`vm:TimelineTemplateSelector` 2, `vm:ContentTemplateSelector` 1, `vm:MediaTemplateSelector` 1, `vm:FullScreenMediaContentViewModelSelector` 1) | `DataTemplateSelector` | **1:1** | WinUI `DataTemplateSelector` 오버라이드 `SelectTemplateCore(object, DependencyObject)` 패턴. `ContentTemplateSelector`/`ItemTemplateSelector` 프로퍼티로 부착. |

> **권장 기준**: 목록은 용도별로 4종으로 정리한다. (1) 큰 가상화 목록 → `ListView`. (2) 단순 목록/`BindableLayout` 대체 → `ItemsRepeater`+`ScrollViewer`. (3) 당김새로고침 → `RefreshContainer`+`ScrollViewer`. (4) 래핑 그리드 → `ListView`+`ItemsWrapGrid`. **`StaggeredItemsLayout` 4사용처는 위험도 최상**이므로 2~3단계에서 커스텀 패널 프로토타입을 먼저 검증한다. `EmptyView`/`RemainingItemsThreshold`는 별도 보조 구현.

---

## 5. 미디어 · 시각 컨트롤

| MAUI (사용처) | Uno / WinUI | 분류 | 비고 |
|---|---|---|---|
| `Image` (45파일 151회) | `Image` | **1:1** | `Source`→`Source`(`BitmapImage`/`Uri`/`ms-appx`). `Aspect`→`Stretch`(`Fill`/`AspectFill`→`UniformToFill`, `AspectFit`→`Uniform`). |
| `FontImageSource` (42파일 140회, 아이콘 글리프) | `FontIcon` (`Glyph`/`FontFamily`) 또는 `ImageIcon` | **대응** | MAUI `FontImageSource`는 `FontFamily`+`Glyph`(+ `x:Static`로 `m:MaterialSharp.*`/`fa:Solid.*` 참조). Uno `FontIcon`에 `Glyph`/`FontFamily`로 매핑. **Uno Material은 Material 아이콘 폰트를 별도 제공하지 않으므로**(Roboto 폰트만 번들), MAUI의 `MaterialSharp.*`/`FontAwesome` 글리프는 동일한 플랫 디자인의 Fluent 아이콘으로 대체한다. `fluent-icons` MCP(`fluent-icons_search_fluent_icons`)로 적절한 아이콘을 검색해 `FontIcon.Glyph`에 사용하고, 폰트는 내장 Fluent 심볼 폰트(`{ThemeResource SymbolThemeFontFamily}`)를 그대로 쓴다. 커스텀 폰트 파일(`MaterialSymbolsOutlined.ttf`/`fa-solid-900.ttf`)을 꼭 유지해야 하면 `Assets/Fonts`로 이동. |
| `ffimageloading:CachedImage` (9파일 12회: `StickerDetailPage` 3, `Media.xaml` 2, `EditPostPage` 1, `PostPage` 1, `EditCommentPage` 1, `CreateStickerPage` 1, `PollVotersPage` 1, `Content.xaml` 1, `StickersPage` 1) | `Image` | **대응** | Uno `Image`는 이미지 로딩 성능·캐싱이 우수하고 애니메이션 WebP(`AnimatedWebP.FFImageLoading.Maui` 대체)도 이미 지원하므로 별도 캐싱 서비스·보조 구현 없이 `CachedImage`를 `Image`로 그냥 교체하면 된다. FFImageLoading의 플레이스홀더·에러 폴백·다운샘플링은 별도로 신경쓰지 않는다. |
| `WebView` (3파일 3회: `AppleLoginPage`, `InAppBrowserPage`, `KakaoStoryLoginPage`) | `WebView2` (`UnoFeature: WebView`) | **대응** | 신규는 `WebView2` 권장, 모든 Uno 타겟 지원. 쿠키 접근·JS 인터롭은 `WebView2` API로 재작성. `WebViewCookieHelper`/`InAppBrowserPage`가 영향권. |
| `Rectangle` (Shape, 14파일 48회: `SettingsPage` 25, `MorePage` 6, `EditPostPage` 3 등) | `Rectangle` (WinUI Shapes) | **1:1** | `Fill`→`Fill`, `Stroke`→`Stroke`, `StrokeThickness`/`RadiusX`/`RadiusY` 매핑. |
| `Ellipse` (Shape, 2파일 2회: `Content.xaml` 1, `PollVotersPage` 1) | `Ellipse` (WinUI Shapes) | **1:1** | 동일. |
| `BoxView` (`FullScreenMediaViewerPage.xaml` 1회) | `Rectangle` (WinUI Shapes) | **1:1** | MAUI `BoxView`는 단순 색상 사각형. WinUI `Rectangle`+`Fill`로 매핑. `CornerRadius`는 `Rectangle.RadiusX`/`RadiusY`. |
| `EllipseGeometry` (12파일 14회) | `EllipseGeometry` | **1:1** | WinUI `Geometry` 체계 동일. 클리핑 마스크(`Image.Clip`)로 사용. |
| `RoundRectangleGeometry` (3파일 3회: `Content.xaml` 2, `CreateStickerPage` 1) | `RectangleGeometry` + `RadiusX`/`RadiusY` 또는 `Path` | **대응** | WinUI에 `RoundRectangleGeometry` 별도 없음. `RectangleGeometry`+`RadiusX`/`RadiusY`(WinUI 3+) 또는 `Path`로 재현. |
| `Shadow` (6파일 9회: `UserPage` 2, `TimelinePage` 2, `StickersPage` 2 등) | `ThemeShadow` + `Translation` Z값 | **대응** | MAUI `Shadow`(`Brush`/`Radius`/`Opacity`/`Offset`)는 Uno `Border.Shadow`에 `ThemeShadow`+`Translation="0,0,Z"` Z값 조합(이 프로젝트 AGENTS.md 규칙). `ThemeShadow`는 크로스플랫폼 무제한 지원. |
| `ppc:PanPinchContainer` (`Media.xaml` 1회) | `ZoomContentControl` (Uno Toolkit) | **대응** | 핀치줌·팬을 `ZoomContentControl`로 흡수(공식 `ZoomContentControl.md`). `IsPanEnabled`/`IsZoomEnabled`/`MinZoomFactor`/`MaxZoomFactor`/`ZoomFactor`/`Reset()` API. `UnoMigration.md` 5단계에 반영. `FullScreenMediaViewerPage` 이미지 핀치줌 영향권. |
| `syncfusion:imageEditor:SfImageEditor` (`ImageEditorPage.xaml` 1회) | **MAUI Embedding 유지** | **대응 없음** | Uno 대체품 없음. `History.Uno.MauiControls` 프로젝트에 MAUI `ContentView`로 남기고 `MauiHost` 호스팅. `ImageEditorPage` 통째로 Embedding 영역. |
| `syncfusion:picker:SfDateTimePicker` + `DateTimePickerHeaderView` + `PickerFooterView` (`EditPostPage.xaml` 1세트) | **MAUI Embedding 유지** 또는 WinUI `DatePicker`/`TimePicker` 재설계 | **대응 없음** | `SfDateTimePicker`는 Syncfusion 고급 컨트롤(헤더/푸터 커스텀). Uno에 1:1 대응 없음. 단순 날짜만 필요하면 WinUI `DatePicker`로, 헤더/푸터 커스텀까지 필요하면 MAUI Embedding 유지. 투표 마감 시간 설정이 영향권. |
| `suggestingBox:SuggestingBox` (`TextContentView.xaml` 1회) | **MAUI Embedding 유지** | **대응 없음** | 멘션/해시태그 자동완전 박스 Uno 대체품 없음. `TextContentView`·`MentionsViewModel` 연동 부분은 Embedding 영역. `EditPost/StickerCollectionView`와 함께 `History.Uno.MauiControls`에 격리. |
| `views:DataTemplatePresenter` (18파일 47회) | `ContentControl` + `ContentTemplateSelector` 또는 커스텀 `UserControl` | **대응** | 프로젝트 자체 컨트롤(`ContentView` 상속). `ContentControl`의 `ContentTemplateSelector`로 흉내, 또는 Uno 네이티브 `UserControl`로 재작성. 타임라인 콘텐츠 슬롯(`TimelineContentsTemplate`)이 영향권. |
| `views:SquareView` (`Post.xaml` 1회) | `UserControl` 또는 `Border` + 바인딩 | **1:1** | 커스텀 정사각형 `ContentView`. 로직만 `UserControl`로 옮기면 됨. |

> **권장 기준**: 이미지·웹뷰는 Uno 내장 컨트롤로 그대로 옮긴다. **FFImageLoading 캐싱은 신경쓰지 않는다** — Uno `Image`는 이미지 로딩 성능이 뛰어나고 캐싱·애니메이션 WebP를 이미 지원하므로 `CachedImage`를 `Image`로 그냥 교체하면 된다. `ZoomContentControl`로 `PanPinchContainer` 대체(1:1 대응 후보). `SfImageEditor`·`SuggestingBox`·`SfDateTimePicker`는 MAUI Embedding 영역에 남겨 재작성 비용을 피한다. **아이콘 정책**: Uno Material은 Material 아이콘 폰트를 별도 제공하지 않으므로(공식 `material-getting-started.md` — Roboto 폰트만 번들) 별도 폰트 추가 없이 동일한 플랫 디자인의 Fluent 아이콘을 사용한다. 아이콘 선택 시 `fluent-icons` MCP로 이름·태그·코드(`E7xx`)를 검색해 가장 적절한 아이콘을 찾고, `FontIcon`에 `FontFamily="{ThemeResource SymbolThemeFontFamily}"` + `Glyph="&#xE7xx;"` 형태로 적용한다.

---

## 6. 네비게이션 · 셸

| MAUI (사용처) | Uno / WinUI | 분류 | 비고 |
|---|---|---|---|
| `ContentPage` (38파일, 거의 모든 페이지) | `Page` | **1:1** | WinUI `Page`가 MAUI `ContentPage`에 대응. `Content` 단일 자식. |
| `Shell` + `TabBar` + `Tab` + `ShellContent` (`AppShell.xaml` 1회, `Tab` 5/`ShellContent` 11) | **Uno Toolkit NavigationShell 패턴**: `TabBar`(`BottomTabBarStyle`) + `Region.Attached` + `Region.Navigator="Visibility"` + 중첩 `Grid` | **대응** | 공식 `NavigationShell.md`가 제시하는 Uno Toolkit의 Shell 대응 패턴. **Material 테마 전제**(`BottomTabBarStyle`은 `Uno.Toolkit.UI.Material` 패키지에만 존재). **Uno 셸 페이지명은 `MainPage`**(`AppShell` 이름으로 이전하지 않음) — `MainPage`를 `Grid`(메인 콘텐츠 영역 + 하단 `TabBar`)로 구성하고 `uen:Region.Attached="True"`로 네비게이션 활성화, 각 `TabBarItem`에 `uen:Region.Name` 부여. 단순 Frame 네비게이션보다 Shell에 가까운 구조 재현. **초기 단계**(`UnoMigration.md` 전략)는 WinUI `Frame` 네비게이션(`-nav blank`)으로 시작, **이후** `Uno.Extensions.Navigation` Region Navigation로 업그레이드하여 NavigationShell 패턴 적용. |
| `TabbedPage` (`MainPage.xaml` 1회 — 루트, 자식 `local:TimelinePage`/`NotificationsPage`/`UserPage`) | `NavigationView` (`PaneDisplayMode="Bottom"`) 또는 `TabBar` | **대응** | MAUI `TabbedPage`는 3개 자식 페이지를 탭으로 전환. **`AppShell`과 역할 중복**(이 프로젝트는 `AppShell` Shell + `MainPage` TabbedPage 혼용). 마이그레이션 시 하나로 통일 — `AppShell`(Uno에서는 `MainPage` 이름으로 이전)의 5탭 체계를 `TabBar`로 옮기고 MAUI `MainPage` TabbedPage는 제거 후, Uno `MainPage`(셸)의 타임라인/알림/프로필 탭이 각 페이지를 직접 호스팅하도록 재설계. |
| `NavigationPage` (요소로는 미사용, 단 `NavigationPage.HasNavigationBar="False"` 부착 프로퍼티가 `FullScreenMediaViewerPage` 1회 사용) | `Frame` 네비게이션 + `AppBar`/`CommandBar` 표시 제어 | **대응** | `HasNavigationBar="False"`는 페이지 헤더를 숨기는 용도. Uno에서는 `Page`에 `AppBar`/`CommandBar`를 아예 두지 않거나 `Visibility="Collapsed"`로. `NavigationPage` 요소 자체는 미사용이므로 네비게이션 컨테이너 매핑은 불필요. |
| 모달 (`PushModalAsync`) | `ContentDialog` (모달) 또는 `Flyout` | **대응** | 전체화면 모달은 `ContentDialog`, 경량 팝업은 `Flyout`. `Uno.Extensions.Navigation`의 `Qualifiers.Dialog`로 Flyout/Modal 자동 분류(공식 `HowTo-ShowDialog.md`). MAUI `DisplayAlertAsync`→`ContentDialog`(`UnoMigration.md` 메모). |

> **권장 기준**: 셸은 3단계로 간다. (1) **1단계** — WinUI `Frame` 네비게이션(`-nav blank`)으로 `App.PushAsync`/`PopAsync` 재구현, MAUI `MainPage` TabbedPage 제거하고 `AppShell`(Uno `MainPage`) 5탭을 `Frame`+수동 탭 전환으로 임시 구성. (2) **3~4단계** — `Uno.Extensions.Navigation` Region Navigation 활성화(`uen:Region.Attached="True"`)하고 Uno Toolkit `TabBar` NavigationShell 패턴 적용(아래 상세 섹션 참조). `AppShell`의 다중 `ShellContent` 하위 섹션 구조는 `TabBarItem`당 1 `Region.Name` + 상위 탭에서 `NavigationView`/`Pivot`로 하위 전환하는 2단계 매핑. (3) 모달은 `ContentDialog`·`Flyout`로 통일. Shell의 URI 라우팅은 1:1 대응이 아니므로 Frame 기반으로 먼저 안정화한 뒤 Region Navigation로 자연스럽게 전환.

---

## 7. 제스처 · 동작 · 트리거

MAUI의 `GestureRecognizer` 체계는 Uno/WinUI에 직접 대응이 없다. 이벤트 기반·`Behavior` 기반·전용 컨트롤로 분산 매핑된다. **프로젝트에서 실제 사용하는 제스처는 `Tap`(160회), `Drag`(1회), `Drop`(1회) 세 종뿐**이다. `Pan`/`Pinch`/`Swipe` GestureRecognizer는 미사용(핀치/팬은 `ppc:PanPinchContainer`가 내부 처리).

| MAUI (사용처) | Uno / WinUI | 분류 | 비고 |
|---|---|---|---|
| `TapGestureRecognizer` (42파일 160회, 거의 모든 인터랙티브 요소) | `Tapped` 이벤트 / `Button.Click` / `Image.Tapped` | **대응** | `Tapped` 이벤트 + `TappedRoutedEventArgs`. 단일 탭은 `Tapped`로 직매핑. `NumberOfTapsRequired`(더블탭)는 프로젝트 미사용. `Command` 바인딩은 `Button.Command` 또는 `Uno.Toolkit.CommandExtensions`로. |
| `DragGestureRecognizer` (`EditPostPage.xaml` 1회 — `CanDrag="True"` `DragStarting="OnMediaDragStarting"`) | `UIElement.CanDrag` + `DragItemsStarting` / `DragStarting` 이벤트 | **대응** | WinUI 드래그앤드롭 API. `UIElement.CanDrag="True"`+`DragStarting` 이벤트. 미디어 드래그로 순서 변경이 영향권. |
| `DropGestureRecognizer` (`EditPostPage.xaml` 1회 — `AllowDrop="True"` `Drop="OnMediaDrop"`) | `UIElement.AllowDrop` + `DragOver`/`Drop` 이벤트 | **대응** | WinUI `AllowDrop="True"`+`DragOver`+`Drop` 이벤트. |
| `toolkit:StatusBarBehavior` (33파일 66회, 거의 모든 페이지 — `CommunityToolkit.Maui`) | Uno 플랫폼별 상태바 처리 (`Platforms/Android`/`Platforms/iOS`) | **대응 없음** | MAUI `StatusBarBehavior`는 상태바 색상/스타일 제어. Uno에 부착 Behavior 대응 없음. `Platforms/Android` `OnCreate`에서 `Window.SetStatusBarColor`, iOS `AppDelegate` 또는 `PlatformEffect`로 재구현. 이 프로젝트 AGENTS.md에 "상태 바 스타일링을 위한 StatusBarBehavior(주황색: TimelinePage, 검정색: ...)" 명시됨. |
| `toolkit:TouchBehavior` (`Content.xaml` 2, `Profile.xaml` 1 — `CommunityToolkit.Maui`) | `Tapped`/`PointerPressed`/`PointerReleased` 이벤트 | **대응** | 터치/롱프레스 감지. `PointerPressed`/`PointerReleased`+`Pointer` 상태로 흉내, 또는 `Uno.Toolkit.InputExtensions`. |
| `toolkit:EventToCommandBehavior` (`Content.xaml` 1 — `CommunityToolkit.Maui`) | `Microsoft.Xaml.Behaviors` 또는 `Uno.Toolkit.CommandExtensions` | **대응** | 이벤트→커맨드 브릿지. `CommandExtensions` 또는 `Microsoft.Xaml.Behaviors.EventToCommand`. |
| `behaviors:SwipeToCloseBehavior` (`Media.xaml` 2회 — 프로젝트 자체 `SwipeToCloseBehavior.cs`) | `SwipeControl` 또는 `Manipulation` 커스텀 / `ZoomContentControl` | **부분** | MAUI `Behavior<T>` 모델은 Uno에 직접 대응 없음. `Microsoft.Xaml.Behaviors`로 재작성하거나, `SwipeControl`/`Manipulation` 이벤트로 재설계. `FullScreenMediaViewerPage` 스와이프 종료가 영향권. |

> **권장 기준**: 제스처는 (1) 탭(160회)은 `Tapped` 이벤트로 직매핑, (2) 드래그/드롭(각 1회)은 WinUI `CanDrag`/`AllowDrop` API로 옮긴다. `Pan`/`Pinch`/`Swipe` GestureRecognizer는 미사용이므로 매핑 불필요(핀치/팬은 `ZoomContentControl`이 흡수). `StatusBarBehavior`(66회, 거의 모든 페이지)는 플랫폼별 코드로 이전해야 하므로 4단계에서 일괄 처리. `SwipeToCloseBehavior` 자체 Behavior는 `Microsoft.Xaml.Behaviors` 또는 컨트롤 기반으로 재작성.

> **이벤트 핸들러 명명 규칙 (필수)**: MAUI에서 그대로 복사해 온 이벤트 핸들러 이름은 `/csharp-winui-projects` 스킬의 `On{ControlName}{EventName}` 규칙에 맞게 반드시 재명명한다. 특히 MAUI 컨트롤 타입명(`Label`, `Entry`)이 이름에 남은 경우 Uno/WinUI 컨트롤 타입명(`TextBlock`, `TextBox`)으로 교체한다. XAML 이벤트 바인딩과 코드비하인드 양쪽을 함께 수정해야 한다. `Click` 이벤트는 `Clicked` 접미사를 사용한다.
>
> - `OnViewTermsLabelTapped` → `OnViewTermsTextBlockTapped` (Label → TextBlock)
> - `OnTermsLabelTapped` → `OnTermsTextBlockTapped` (Label → TextBlock)
> - `OnInviteCodeTextChanged` → `OnInviteCodeTextBoxTextChanged` (Entry → TextBox)
> - `OnCheckBoxChecked` / `OnRegisterButtonClicked` 처럼 컨트롤 타입명이 이미 맞는 이름은 유지
>
> **컨트롤 이름(x:Name) 명명 규칙 (필수)**: `x:Name`은 `{의미 있는 이름}{WinUI 컨트롤 타입}` 형식(PascalCase)으로 작성한다. MAUI에서 그대로 가져온 x:Name 중 MAUI 컨트롤 타입명(`Entry`, `Label`, `Editor`, `Picker`, `Switch`, `ImageButton`, `ActivityIndicator`, `CollectionView`, `RefreshView` 등)이 포함된 것은 WinUI 컨트롤 타입명으로 교체한다. 이벤트 핸들러 명명 규칙(`On{ControlName}{EventName}`)의 ControlName이 곧 이 x:Name이므로, 컨트롤명을 바꾸면 핸들러명도 함께 맞춘다.
>
> - `InviteCodeEntry` → `InviteCodeTextBox` (Entry → TextBox)
> - `LoginVerticalStackLayout` → `LoginStackPanel` (VerticalStackLayout → StackPanel)
> - `MainActivityIndicator` → `MainProgressRing` (ActivityIndicator → ProgressRing)
> - `TermsCheckBox` / `RegisterButton` / `MainWebView` 처럼 이미 WinUI 타입명인 경우 유지
>
> 이미 수정 완료된 사례: `RegisterPage.xaml` — `InviteCodeEntry` → `InviteCodeTextBox`, `LoginPage.xaml` — `LoginPanel` → `LoginStackPanel`.

---

## 8. 마이그레이션 우선순위 · 위험도 요약

### 1:1 대응(낮은 위험, 기계적 변환 가능)

`Grid` · `StackPanel`(↔`HorizontalStackLayout`/`VerticalStackLayout`) · `ScrollViewer`(↔`ScrollView`) · `TextBlock`(↔`Label`) · `Button` · `CheckBox` · `ProgressRing`(↔`ActivityIndicator`) · `ProgressBar` · `Image` · `Rectangle`(↔`Rectangle`/`BoxView`) · `Ellipse` · `EllipseGeometry` · `DataTemplate` · `DataTemplateSelector` · `UserControl`(↔`ContentView`) · `Page`(↔`ContentPage`) · `VisualStateManager`(↔`VisualState`/`VisualStateGroup`) · `Border` · `RefreshContainer`(↔`RefreshView`)

→ 이 컨트롤들로 이루어진 페이지는 1단계/2단계에서 우선 옮긴다. 프로퍼티명 매핑표만 준비하면 기계적 변환 가능.

### 대응(중간 위험, 매핑 규칙 학습 후 변환)

`TextBox`(↔`Entry`/`Editor`) · `ToggleSwitch`(↔`Switch`) · `ComboBox`(↔`Picker`) · `AutoSuggestBox`(↔`SearchBar`) · `Button`+`Icon`(↔`ImageButton`) · `FontIcon`/`ImageIcon`(↔`FontImageSource`) · `WebView2`(↔`WebView`) · `NavigationView`/`TabBar`(↔`Shell`/`TabbedPage`) · `Frame` 네비게이션(↔`NavigationPage` 부착 프로퍼티) · `ContentDialog`/`Flyout`(↔모달) · `ThemeShadow`(↔`Shadow`) · `ItemsRepeater`(↔`BindableLayout`) · `ListView`+`ItemsWrapGrid`/`ItemsStackPanel`(↔`CollectionView`+`GridItemsLayout`/`LinearItemsLayout`) · `ContentControl`+`ContentTemplateSelector`(↔`DataTemplatePresenter`) · `ZoomContentControl`(↔`PanPinchContainer`) · `Tapped`(↔`TapGestureRecognizer`) · `CanDrag`/`AllowDrop`(↔`Drag`/`DropGestureRecognizer`) · `RoundRectangleGeometry`→`RectangleGeometry`+Radius

→ 2단계/3단계에서 매핑 규칙을 적용. `CarouselView`/`Shell`은 API 모델 자체가 달라 재설계 비용 수반.

### 부분(높은 위험, 보조 구현 필요)

`CollectionView`(`EmptyView`/`RemainingItemsThreshold` 부재) · `FlipView`+`PipsPager`(↔`CarouselView`, `CurrentItem`/`Position` API 상이) · `RichTextBlock`(↔`Span`+`GestureRecognizers`, 제스처 없음, `Repost.xaml` 1곳) · `Shell` URI 라우팅(초기 — NavigationShell 패턴으로 해소) · `SwipeToCloseBehavior` 재작성 · `StatusBarBehavior` 플랫폼별 이전

### 대응 없음(재설계 또는 MAUI Embedding)

| `FlexLayout`(`CreateStickerPage` 1회 → `ItemsWrapGrid`/반응형 `Grid`) · `SfImageEditor`(`ImageEditorPage` 1회 → Embedding 유지) · `SfDateTimePicker`(`EditPostPage` 1세트 → Embedding 유지 또는 `DatePicker` 재설계) · `SuggestingBox`(`TextContentView` 1회 → Embedding 유지)

→ `FlexLayout`은 `ItemsWrapGrid`로 재설계. `StaggeredItemsLayout`은 공식 NuGet 패키지로 해결(위 섹션 참조, 위험도 하락). Embedding 유지 3종은 `History.Uno.MauiControls` 프로젝트에 격리.

---

## 9. 변환 작업 시 체크리스트

각 페이지/컨트롤을 옮길 때 아래 순서로 적용한다.

0. **스킬 사전 로드**: 작업 시작 전 `/csharp-winui-projects`(+ `$csharp-code-style`) 스킬을 반드시 로드한다. WinUI와 동일한 API·코딩 규칙(이벤트 핸들러 `On{Control}{Event}` 명명, XAML 포맷터, Fluent 아이콘 `fluent-icons` MCP 검색, `.resw` 지역화 등)이 스킬에 정의되어 있다.
1. **네임스페이스 교체**: `xmlns`을 WinUI 스키마(`http://schemas.microsoft.com/winfx/2006/xaml/presentation`)로, `x:`은 `http://schemas.microsoft.com/winfx/2006/xaml`로. `xmlns:toolkit`은 `using:Uno.Toolkit.UI`/`using:Uno.UI.Controls`로. `xmlns:utu`(`Uno.Toolkit.UI`) 권장.
2. **컨트롤명·프로퍼티명 매핑**: 위 표 기준. `x:DataType`은 그대로 유지(CommunityToolkit.Mvvm 호환).
3. **리소스 키 재매핑**: MAUI `Styles/*.xaml`을 Uno `ResourceDictionary`로 옮길 때 `StaticResource` 키는 유지하되, WinUI 컨트롤 템플릿 바인딩(`TemplateBinding`) 문법으로 조정.
4. **`{Binding StringFormat=...}` 금지**: 이 프로젝트 AGENTS.md 규칙. 다중 `Run`으로 문자열 조합.
5. **바인딩 모드 명시**: `TwoWay`/`OneWay`/`OneTime` 명시(이 프로젝트 AGENTS.md).
6. **`Shell`/`TabbedPage` 의존 제거**: `App.PushAsync`/`PopAsync` 재구현 후, 코드비하인드의 `Navigation.PushAsync`/`PopAsync`를 `App.PushAsync`/`App.PopAsync` 정적 호출로 교체. `AppShell`(Uno `MainPage`) Shell + MAUI `MainPage` TabbedPage 이중 구조를 `TabBar` NavigationShell 단일 구조로 통합(아래 "AppShell · TabbedPage 마이그레이션 상세" 섹션 참조).
7. **제스처/Behavior 재작성**: `TapGestureRecognizer`→`Tapped`, `Drag`/`Drop`→`CanDrag`/`AllowDrop`, `Span.GestureRecognizers`(`Repost.xaml` 1곳)→`Hyperlink.Click`, `Behavior<T>`→`Microsoft.Xaml.Behaviors` 또는 Uno.Toolkit attached property. `StatusBarBehavior`→플랫폼별 코드. **MAUI에서 복사한 이벤트 핸들러 이름은 `/csharp-winui-projects` 규칙(`On{ControlName}{EventName}`)에 맞게 재명명** — `Label`/`Entry` 등 MAUI 컨트롤 타입명이 남은 핸들러(`OnTermsLabelTapped`, `OnInviteCodeTextChanged`)는 Uno 컨트롤 타입명(`TextBlock`/`TextBox`)으로 교체하고 XAML·코드비하인드 양쪽을 함께 수정한다 (7장 "이벤트 핸들러 명명 규칙" 참조). **x:Name 컨트롤명도 동일 규칙 적용** — MAUI 컨트롤 타입명이 남은 컨트롤명(`InviteCodeEntry`)은 WinUI 타입명(`InviteCodeTextBox`)으로 교체한다 (7장 "컨트롤 이름(x:Name) 명명 규칙" 참조).
8. **MAUI Embedding 경계**: `SuggestingBox`/`SfImageEditor`/`SfDateTimePicker`가 포함된 페이지는 `MauiHost`로 호스팅되는 `History.Uno.MauiControls` `UserControl`로 분리.
9. **`StaggeredItemsLayout` → 공식 NuGet 패키지 적용**: `CommunityToolkit.WinUI.Controls.Primitives`(8.2.251219 이상, MIT)를 `History.Uno` csproj에 추가하고 `<UnoFeatures>`에 `Toolkit` 포함. `CollectionView`+`staggered:StaggeredItemsLayout`을 `ItemsRepeater`+`controls:StaggeredLayout`으로 교체. Android에서 `DesiredColumnWidth` 측정값 차이 보정 필요(아래 상세 섹션 참조).
10. **플랫폼별 코드**: `OnPlatform`/`DeviceInfo.Platform` 분기를 `Platforms/Android`/`Platforms/iOS` 폴더의 부분 클래스로 이전. `CustomShellRenderer`/`CustomTabBarAppearanceTracker`/`StaggeredItemsViewLayout` iOS 커스텀 렌더러는 Uno 네이티브 컨트롤 또는 플랫폼별 구현로 재작계.
11. **빌드 검증**: 각 단계 종료 시 `net10.0-android`/`net10.0-ios` 빌드 통과 확인(`UnoMigration.md` 각 단계 마지막 체크리스트).

---

## StaggeredLayout 마이그레이션 상세

`madome-uno` 저장소(`E:\Repos\madome-uno`) 조사 결과, 해당 프로젝트는 커스텀 `StaggeredLayout` 구현을 작성하지 않고 **Windows Community Toolkit 공식 NuGet 패키지**를 사용한다. `History.Uno` 마이그레이션에서도 동일 방식 적용.

### 패키지 / 설정

- **NuGet 패키지**: `CommunityToolkit.WinUI.Controls.Primitives` (8.2.251219 이상, MIT 라이선스, .NET Foundation)
- **의존 패키지**(트랜지티브): `CommunityToolkit.WinUI.Extensions`, `Uno.WinUI`(비Windows 타겟)
- **UnoFeatures**: csproj `<UnoFeatures>`에 `Toolkit` 포함 필수(`CommunityToolkit.WinUI.Controls` 네임스페이스 활성화)
- **Central Package Management** 사용 시 `Directory.Packages.props`에 버전 중앙화, csproj에는 버전 없이 `<PackageReference>`만 선언

### API (프로젝트에서 쓰는 프로퍼티만)

| 프로퍼티 | 타입 | 비고 |
|---|---|---|
| `DesiredColumnWidth` | `double` | 열 폭 기준값. **열 개수는 (가용 폭 / 이 값)으로 자동 산출** — MAUI의 고정 `SpanCount`/`Columns`와 다른 적응형 모델. |
| `ColumnSpacing` | `double` | 열 간 간격. |
| `RowSpacing` | `double` | 행 간 간격. |
| `ItemsStretch` | `StaggeredLayoutItemsStretch` | `None`(폭=DesiredColumnWidth) / `Fill`(가용 폭 균등 분배, 최소=DesiredColumnWidth). |

**MAUI `StaggeredItemsLayout`과 API 차이**: `Orientation`/`SpanCount`/`Columns`/`Padding` 프로퍼티 없음. 열 개수는 `DesiredColumnWidth`로 간접 제어. `Padding`은 호스트 `ItemsRepeater`/`ScrollViewer`/부모 컨테이너에 적용.

### XAML 사용 패턴 (`madome-uno` 실제 사용 예)

```xml
xmlns:controls="using:CommunityToolkit.WinUI.Controls"

<ItemsRepeater
    x:Name="HistoryBookListItemRepeater"
    Margin="20,10,20,5"
    ItemTemplate="{StaticResource BookSkeleton}">
    <ItemsRepeater.Layout>
        <controls:StaggeredLayout
            ColumnSpacing="10"
            DesiredColumnWidth="300"
            ItemsStretch="Fill"
            RowSpacing="10" />
    </ItemsRepeater.Layout>
</ItemsRepeater>
```

### Android 측정값 차이 보정 (중요)

`madome-uno` 5개 페이지(`HistoryPage`, `MetaDetailPage`, `SearchPage`, `FavoritePage`, `BookListPage`) 모두 코드비하인드에서 `#if ANDROID` 블록으로 `DesiredColumnWidth`를 300 → 150으로 절반 조정한다. Windows와 Android 간 열 폭 측정값 차이로 같은 XAML 값이 다른 열 개수를 만들기 때문.

```csharp
#if ANDROID
    HistoryBookListItemRepeater.Layout = new StaggeredLayout() {
        ColumnSpacing = 10, DesiredColumnWidth = 150,
        ItemsStretch = StaggeredLayoutItemsStretch.Fill, RowSpacing = 10
    };
#endif
```

`History.Uno`에서도 동일 보정 필요 예상. 마이그레이션 시 실제 Android 기기에서 열 개수 확인 후 `DesiredColumnWidth` 조정.

### MAUI 고정 열 개수 매핑

MAUI `StaggeredItemsLayout`이 고정 열 개수(예: 2열)를 썼다면, `DesiredColumnWidth`를 (전화 폭 - 간격) / 열 수로 역산:
- ~400px 폰, `ColumnSpacing=10`, 2열 → `DesiredColumnWidth ≈ 195` (Fill 모드에서 열 폭 균등 분배)
- 1열(세로 스택) → `DesiredColumnWidth`를 폰 폭 이상으로 설정

### 대체 패널 (비가상화)

같은 패키지에 비가상화 `StaggeredPanel`(`Panel` 서브클래싱, `Padding` 지원)도 포함. `EditPostPage`처럼 항목 수가 적고 가상화 불필요한 곳은 `StaggeredPanel`을 직접 자식 컨테이너로 쓸 수도 있음. 단, `madome-uno`는 `StaggeredLayout`(가상화)만 사용.

### 왜 커스텀 패널 작성이 불필요한가

- `StaggeredLayout`은 `VirtualizingLayout` 기반이라 `ItemsRepeater`와 결합 시 가상화 무료 제공 → MAUI `VirtualizingStackLayout` 기반 `CollectionView`와 동등
- Microsoft/.NET Foundation이 MIT로 유지보수하므로 직접 구현보다 안정
- `madome-uno` 검증 완료(Uno 6.5 / .NET 10 조합에서 정상 동작 확인)

---

## AppShell · TabbedPage 마이그레이션 상세

MAUI `AppShell.xaml`은 Shell에 5개 `Tab` + 다중 `ShellContent` 하위 섹션 구조, `MainPage.xaml`은 별도 `TabbedPage`(3탭)로 이중 셸이다. Uno Toolkit의 `NavigationShell` 패턴(공식 `external/uno.chefs/doc/toolkit/NavigationShell.md`)으로 단일 구조로 통합한다. **셸 페이지는 Uno에서 `MainPage`라는 이름으로 이전한다** — `AppShell` 이름은 MAUI 소스에서만 존재하고, MAUI `MainPage`(TabbedPage)는 제거되어 Uno `MainPage`(셸)가 `AppShell`을 대체한다.

### MAUI 현재 구조 분석

`AppShell.xaml` 5탭:
1. **타임라인** — `ShellContent` 1개 → `TimelinePage`
2. **알림/쪽지** — `ShellContent` 2개 → `NotificationsPage`(알림), `MessagesPage`(쪽지)
3. **친구** — `ShellContent` 6개 → `FriendListPage`, `AddFriendsPage`, `WaitingFriendRequestsPage`, `PendingFriendRequestsPage`, `IgnoredFriendsPage`, `BlockedFriendsPage`
4. **더보기** — `ShellContent` 1개 → `MorePage`
5. **프로필** — `ShellContent` 1개 → `UserPage`

`MainPage.xaml` TabbedPage 3탭(Shell과 중복): `TimelinePage`, `NotificationsPage`, `UserPage`

### Uno Toolkit NavigationShell 패턴

공식 Chefs 예제의 `MainPage.xaml` 구조(공식 `NavigationShell.md`):

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI">
    <Grid>
        <Grid x:Name="MainGrid"
              uen:Region.Attached="True">
            <Grid.RowDefinitions>
                <RowDefinition />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- 콘텐츠 영역: 선택된 페이지 표시 -->
            <Grid x:Name="NavigationGrid"
                  Grid.Row="0"
                  uen:Region.Attached="True"
                  uen:Region.Navigator="Visibility" />

            <!-- 하단 탭 바 -->
            <utu:TabBar x:Name="Tabs"
                        Grid.Row="1"
                        uen:Region.Attached="True"
                        Style="{StaticResource BottomTabBarStyle}">
                <utu:TabBarItem uen:Region.Name="Home"
                                IsSelectable="True"
                                Content="Home">
                    <utu:TabBarItem.Icon>
                        <FontIcon Glyph="{StaticResource Icon_Home}" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
                <!-- 추가 TabBarItem... -->
            </utu:TabBar>
        </Grid>
    </Grid>
</Page>
```

핵심 구성:
- `uen:Region.Attached="True"` — 영역 네비게이션 활성화
- `uen:Region.Navigator="Visibility"` — 자식 페이지 Visibility 전환으로 네비게이션
- `utu:TabBarItem`의 `uen:Region.Name` — 탭 선택 시 해당 이름의 페이지로 전환

### History.Uno 매핑 (5탭 + 하위 섹션)

**1단계: 단순 `TabBar` + `Frame` (Region Navigation 도입 전)**

초기엔 `TabBar` 선택 이벤트 → `Frame.Navigate(typeof(Page))`로 수동 전환:
```xml
<utu:TabBar SelectionChanged="OnTabChanged">
    <utu:TabBarItem Content="타임라인">
        <utu:TabBarItem.Icon><FontIcon Glyph="&#xE700;" /></utu:TabBarItem.Icon>
    </utu:TabBarItem>
    <!-- ... -->
</utu:TabBar>
<Frame x:Name="ContentFrame" />
```

**3~4단계: Region Navigation NavigationShell (권장)**

5탭을 `TabBarItem`에 `uen:Region.Name` 매핑. MAUI의 다중 `ShellContent` 하위 섹션(알림/쪽지 2개, 친구 6개)은 2가지 패턴 중 택일:

- **패턴 A — 각 `ShellContent`를 별도 `Region.Name`으로**: 탭 내에서 하위 페이지를 각각 독립 Region으로 등록. `친구` 탭 클릭 시 기본 `FriendListPage` 표시, 하위 메뉴(`AddFriendsPage` 등)는 `Frame` 푸시 또는 상단 `Pivot`/`NavigationView`로 전환.
- **패턴 B — 상위 탭은 `TabBar`, 하위 섹션은 `Pivot` 또는 `NavigationView` (`PaneDisplayMode="Top"`)**: `알림/쪽지` 탭은 상단 `Pivot`(알림 | 쪽지) + 콘텐츠, `친구` 탭은 상단 `NavigationView` 또는 리스트 메뉴로 6개 하위 전환. MAUI Shell의 `Tab`+다중 `ShellContent` UI(상단 서브탭)와 가장 유사.

MAUI `AppShell.xaml` 5탭 → Uno `MainPage.xaml`(셸) 매핑 예시(패턴 B 기준):
```
TabBar(하단)
  ├─ 타임라인  → Region.Name="Timeline"        (ShellContent 1개)
  ├─ 알림/쪽지 → Region.Name="Notifications"  + 상단 Pivot(알림|쪽지)
  ├─ 친구      → Region.Name="Friends"          + 상단 NavigationView(6개 하위)
  ├─ 더보기    → Region.Name="More"             (ShellContent 1개)
  └─ 프로필    → Region.Name="Profile"          (ShellContent 1개)
```

### `MainPage` TabbedPage 제거 (Uno `MainPage`는 셸 페이지)

MAUI `MainPage.xaml`의 3탭(`TimelinePage`/`NotificationsPage`/`UserPage`)은 `AppShell` 5탭에 완전 포함되므로 제거. **Uno의 `MainPage`라는 이름은 `AppShell`(셸 페이지)이 차지**하며, 셸의 타임라인/알림/프로필 탭이 직접 해당 페이지 호스팅.

### 반응형 Shell (선택)

공식 `NavigationShell.md`는 `utu:Responsive` 마크업으로 `Normal`(하단 `TabBar`) / `Wide`(좌측 `TabBar`) 전환 제시. `History.Uno`는 모바일 전용(iOS/Android)이므로 반응형 Shell 불필요 — 하단 `TabBar` 고정.

### 의존성

- **UnoFeatures**: `Toolkit`(TabBar + Material 사전 스타일) + `Navigation`(Region Navigation) csproj에 추가
- **테마 전제**: **Uno Material 테마**(`MaterialToolkitTheme`) — `TabBar` 사전 스타일(`BottomTabBarStyle`/`TopTabBarStyle`/`VerticalTabBarStyle`)은 `Uno.Toolkit.UI.Material`/`Uno.Toolkit.UI.Cupertino` 패키지에만 존재하고 Fluent에는 없으므로(공식 `TabBarAndTabBarItem.md` 명시), NavigationShell 패턴의 `Style="{StaticResource BottomTabBarStyle}"`을 쓰려면 Material 테마 필수. `UnoMigration.md` 테마 전략도 이에 맞춰 Material로 전환.
- `xmlns:utu="using:Uno.Toolkit.UI"` (TabBar/TabBarItem/Responsive)
- `xmlns:uen="using:Uno.Extensions.Navigation.UI"` (Region.Attached/Region.Name/Region.Navigator)

### 왜 NavigationShell 패턴이 Shell 대응인가

- MAUI Shell의 `Tab`/`ShellContent` 계층 + 하단 탭 + 콘텐츠 영역 구조를 `TabBar` + `Region.Attached` `Grid`로 1:1 재현
- URI 라우팅(Shell `GoToAsync`)은 Region Navigation의 `navigator.NavigateViewAsync("RegionName")`로 대응
- `NavigationBarIsVisible`/`TabBarIsVisible`(`MainPage.xaml`)은 `TabBar`/`NavigationView` `Visibility`로 제어
- Chefs 예제(`Uno.Recipes.NavigationShell`)가 검증한 공식 패턴이므로 커뮤니티 예제 의존 불필요
- Material 테마 전제하에 `BottomTabBarStyle` 등 사전 스타일 그대로 사용 가능(MAUI `UraniumUI.Material`과 시각적 일관성 유지)

---

## 참고 문서

- 공식 XF→Uno 컨트롤 매핑: `guides/xf-migration/control-mappings.md`
- `StaggeredLayout`: Windows Community Toolkit `CommunityToolkit.WinUI.Controls.Primitives` NuGet 패키지(8.2.251219, MIT) — `madome-uno` 저장소(`E:\Repos\madome-uno`) 사용 패턴 검증. 소스: https://github.com/CommunityToolkit/Windows
- `ItemsRepeater`: `implemented/microsoft-ui-xaml-controls-itemsrepeater.md` + `external/uno.toolkit.ui/doc/helpers/itemsrepeater-extensions.md`
- `RefreshContainer`: `controls/RefreshContainer.md`
- `FlipView`+`PipsPager`: `implemented/microsoft-ui-xaml-controls-flipview.md` + `external/uno.toolkit.ui/doc/helpers/FlipView-extensions.md`
- `ZoomContentControl`: `external/uno.toolkit.ui/doc/controls/ZoomContentControl.md`
- `AutoLayout`: `external/uno.toolkit.ui/doc/controls/AutoLayoutControl.md`
- `TabBar`: `external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.md`
- `NavigationShell` 패턴: `external/uno.chefs/doc/toolkit/NavigationShell.md` + `external/uno.chefs/doc/toolkit/NavigateTabBar.md`
- `MediaPlayerElement`: `controls/MediaPlayerElement.md`
- `WebView2`: `controls/WebView.md`
- `ListView` 내부: `uno-development/listviewbase-internals.md`
- `Frame` 네이티브 네비게이션: `features/native-frame-nav.md`
- `InputExtensions`/`CommandExtensions`: `external/uno.toolkit.ui/doc/helpers/Input-extensions.md`
- Dialog(Flyout/Modal): `external/uno.extensions/doc/Learn/Navigation/HowTo-ShowDialog.md`