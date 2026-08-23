# History.MobileClient Windows 출시 준비 — 공백 분석 보고서

> 대상: `History.MobileClient/History.MobileClient.csproj` (net10.0-android / net10.0-ios 전용, **Windows 타깃 미포함**)
> 분석 기준: 프로젝트 소스, csproj, NuGet 패키지 TFM, 플랫폼 분기 코드 전수 조사

## 요약

| 구분 | 상태 |
|---|---|
| Windows 타깃(`net10.0-windows*`) | **미존재** — 최상위 차단 |
| `Platforms\Windows` 폴더 | **미존재** |
| 빌드 차단 컴파일 오류 후보 | **10개 이상 파일** (GoogleAuthService 부재, TabBarBadge partial 미구현, 미디어 픽커 분기 누락 등) |
| 패키지 | `Xamarin.MediaGallery`(Windows TFM 없음 → NU1202), `Plugin.Firebase.CloudMessaging`(Windows에서 전부 NotImplementedException) |
| 기능 | 푸시 알림·미디어 첨부·Google 로그인·업데이트 체크 전부 모바일 전제 |

---

## 1. 빌드 차단 (치명 · 수정 필수)

### 1-1. `TargetFrameworks`에 Windows 타깃 부재 (csproj L4-5)
```xml
<TargetFrameworks>net10.0-android</TargetFrameworks>
<TargetFrameworks Condition="...osx or windows">$(TargetFrameworks);net10.0-ios</TargetFrameworks>
```
- `net10.0-windows10.0.*` 타깃이 없어 Windows 빌드가 아예 불가능합니다.
- **권장**: `net10.0-windows10.0.26100.0` — 참조 중인 `SuggestingBox.Maui`가 `net10.0-windows10.0.26100.0`으로 빌드되며(위치: `E:\Repos\SuggestingBox.Maui\SuggestingBox.Maui\SuggestingBox.Maui.csproj` L6), 참조 프로젝트보다 낮은 Windows SDK 버전을 쓰면 불일치 오류가 나므로 **26100으로 통일**해야 합니다. (다른 패키지들은 `net10.0-windows10.0.19041` 제공이라 26100 하위 호환.)

### 1-2. `Platforms\Windows` 폴더 부재
- `App.xaml`, `MainWindow.xaml`, `Package.appxmanifest`, `app.manifest`, `launchSettings.json` 등 Windows 플랫폼 프로젝트가 전혀 없습니다(glob 결과 0건).
- `WindowsPackageType=None`(L35)만 선언되어 있어 unpackaged 배포 의도로 보이나, 실제 Windows 프로젝트 파일 자체가 없어 효력이 없습니다.

### 1-3. `GoogleAuthService` 클래스 부재 → LoginPage 컴파일 오류 확정
- `Auth/` 폴더에는 `GoogleAuthService.Android.cs`(`#if ANDROID`)와 `GoogleAuthService.iOS.cs`(`#if IOS`)만 있습니다. **공통/Windows 구현이 없습니다.**
- `Pages/LoginPage.xaml.cs` L145에서 `new GoogleAuthService()` 호출 → Windows 타깃에서는 타입 미정의 **CS0246 오류**.
- **해결**: Windows용 구현 추가(WebView/브라우저 기반 OAuth 코드 플로우) 또는 `#if` 가드. 참고로 Apple 로그인은 `AppleLoginPage`(WebView + localhost redirect) 방식이라 Windows에서 그대로 동작 가능합니다.

### 1-4. `TabBarBadge` Windows partial 미구현 → 컴파일 오류
- `ShellTabBarBadge/TabBarBadge.cs`(공통)가 `ShowImpl`/`HideImpl` **partial 메서드**를 호출하지만, 구현은 `Platforms/Android/TabBarBadge.Android.cs`, `Platforms/iOS/TabBarBadge.iOS.cs`뿐입니다.
- static partial 메서드는 구현이 필수(C# 9+ 규칙)이므로 Windows 빌드 시 **CS8795 계열 오류**가 납니다.
- **해결**: `Platforms/Windows/TabBarBadge.Windows.cs` 추가(no-op 구현 또는 WinUI `NavigationView/Shell` 대응). 다만 `App.xaml.cs`의 폴링 연결(`#if ANDROID || IOS` L266·L275)은 Windows에서 실행되지 않으므로 동작 연동은 선택입니다.

### 1-5. 미디어 픽커 `#if IOS ... #elif ANDROID ... #endif` 패턴 → 변수 미선언 오류
`#if/#elif/#endif` 구조라 Windows에서는 **두 블록 모두 잘려**, 블록 내부에서만 선언되던 로컬 변수(`fileName`, `bytes`, `image` 등)가 이후 코드에서 참조되면서 **CS0103/CS0165 오류**가 발생합니다. 해당 패턴은 다음 파일에 있습니다:

| 파일 | 위치 |
|---|---|
| `Pages/EditPostPage.xaml.cs` | L343-453 (이미지/비디오 첨부 여러 곳) |
| `Pages/EditCommentPage.xaml.cs` | L200-217 |
| `Pages/CreateStickerPage.xaml.cs` | L89-99, L159-179 |
| `Pages/PostPage.xaml.cs` | L136-156, L296-312 |
| `Pages/WriteMessagePage.xaml.cs` | L61-71 |
| `ViewModels/HistoryProfileViewModel.cs` | L230-242, L280-292 |
| `ViewModels/KakaoProfileViewModel.cs` | L332-344, L397-409 |

- **해결**: 각 위치에 `#elif WINDOWS` 분기를 추가하고 MAUI Essentials `FilePicker`로 대체합니다.
- **근본 원인**: `Xamarin.MediaGallery 3.0.0`이 Windows TFM을 제공하지 않기 때문(아래 2-2)입니다.

---

## 2. 패키지 의존성

### 2-1. `Plugin.Firebase.CloudMessaging 4.0.1` — Windows 알림 미지원
- TFM: `net9.0` / `net9.0-android35` / `net9.0-ios18`만 제공.
- `net9.0` 포터블 어셈블리는 모든 API가 **NotImplementedException을 던지는 reference assembly**입니다(저장소 코드 `CrossFirebaseCloudMessaging.cs`의 `NotImplementedInReferenceAssembly()` 확인). 빌드는 통과하지만 **런타임 전면 무력화**.
- 영향: `MauiProgram.cs`의 `RegisterFirebaseServices()`(L113-131, iOS/Android 블록만 있음), `Utils.RefreshFirebaseToken()`(L753-773, 예외 catch로 삼켜짐), 푸시 알림 수신 전체.
- **해결**: Windows에서는 로컬 알림(예: `CommunityToolkit.Maui` 알림 또는 WNS)으로 대체하거나 알림 기능 제외 처리 필요.

### 2-2. `Xamarin.MediaGallery 3.0.0` — **복원 자체가 실패 (NU1202)**
- TFM: `net8.0-android34` / `net8.0-ios18`만 존재 → `net10.0-windows*` 타깃에서 **호환성 오류로 NuGet 복원 실패 → Windows 타깃 추가와 동시에 빌드 차단**.
- csproj 공통 ItemGroup(L163)에 있어 모든 타깃에 참조됩니다.
- **해결**: 패키지 참조를 `net10.0-android`/`net10.0-ios` 조건부로 분리 + Windows는 `FilePicker`/`FileSaver`(Essentials) 사용.
- 참고: Android에서는 `AndroidMediaPickerHelper`를 우회 경로로 쓰므로 실질 영향은 iOS+Windows 경로의 `MediaGallery.PickAsync/SaveAsync` 호출(`FullScreenMediaViewerPage`, `PostImageRendererHelper` 등)입니다.

### 2-3. Windows TFM 보유로 문제없는 패키지
- `CommunityToolkit.Maui 15.0.0`, `CommunityToolkit.Maui.MediaElement 10.0.0` (`net10.0-windows10.0.19041`) ✅
- `Syncfusion.Maui.ImageEditor/Toolkit 34.2.4/1.0.10` (`net10.0-windows10.0.19041`) ✅ — 라이선스는 `MauiProgram.cs` L40에서 이미 등록
- `UraniumUI.Material 3.0.0`, `AnimatedWebP.FFImageLoading.Maui 1.4.4` (`net10.0-windows10.0.19041`) ✅
- `SuggestingBox.Maui` (ProjectReference, `net10.0-windows10.0.26100`) ✅ — 단 **Windows SDK 버전 26100 맞춤 필요**(1-1)
- `dccon.NET`, `InvenSticker.NET` (netstandard2.0) ✅
- `Xamarin.GooglePlayServices.Auth` / `AdamE.Google.iOS.SignIn` — 플랫폼 조건부 참조(L137-143)라 Windows에는 포함 안 됨(1-3의 GoogleAuthService 부재는 별개 문제)

---

## 3. 런타임 오류 / 기능 미동작 (Windows 타깃 추가 후)

| 항목 | 파일:줄 | 현상 |
|---|---|---|
| 업데이트 체크가 iOS/TestFlight로 감 | `Utils.cs` L801-822 | `#if ANDROID ... #else`라 Windows에서 `version_ios` URL을 읽고 "TestFlight에서 업데이트" 안내 표시. `#elif WINDOWS` 분기 + 서버에 `version_windows` 파일 추가 필요 |
| FCM 푸시 알림 전체 무력화 | `MauiProgram.cs` L102-108, L116-127 | Windows에는 이벤트 등록 경로 자체가 없고, 플러그인도 NotImplementedException |
| 카카오 웹 로그인 쿠키 유실 가능 | `WebViewCookieHelper.cs` L26-28 | Windows에서 `#else` → **빈 쿠키 목록 반환**. 카카오스토리 로그인/이모티콘 인증 흐름에 영향(WebView2 CookieManager로 구현 필요) |
| 타임라인 Staggered 레이아웃 기능 저하 | `ThirdParty/StaggeredLayout/StaggeredStructuredItemsViewHandler.cs` L17-46 | `MapItemsLayout`(Android), `SelectLayout`(iOS) 모두 전용. Windows는 기본 `ItemsViewLayout` → `StaggeredItemsLayout`(Span=10000)이 **일반 그리드로 렌더링**(XAML 타임라인/발견/검색/글쓰기 첨부). 단 Blazor 타임라인이 주력이라 실사용 영향은 제한적 |
| StatusBarBehavior no-op | `Behaviors/StatusBarBehavior.cs` | Windows partial 없음 — 컴파일 통과, 동작 없음. 타이틀바 색/테마 통합 작업 필요 |
| 상태 표시줄·백버튼 UX | `LoginPage.xaml.cs` L93-103, `AppShell.xaml.cs` | "뒤로 두 번 누르면 종료" 패턴이 Windows에서는 의미 없음(Alt+← 등). UX 검토 필요 |

---

## 4. 기능 공백 (Windows에서 제공 불가 상태)

1. **푸시 알림**: FCM 전제 — Windows용 대체(로컬 알림 등) 미구현.
2. **미디어 첨부**: 게시글/댓글/쪽지/스티커 아이콘·에셋/프로필·배경 이미지 모두 모바일 픽커 전제 — `FilePicker` 이식 필요(1-5와 동일 작업).
3. **Google 로그인**: Android(GoogleSignInClient)/iOS(Google.SignIn) 전용이므로 Windows 로그인 경로 0개(1-3).
4. **앱 링크**: `historyweb.cc` Universal Links/App Links는 Android/iOS 전용 — Windows 진입 경로 없음(`App.HandleAppLinkAsync`는 모바일 경로에서만 호출).
5. **카카오 이모티콘 로그인**: `KakaoEmoticonWebViewClient`(Android)/`KakaoEmoticonUrlSchemeHandler`(iOS) 전용 — Windows에서 이모티콘 인증 미동작.

---

## 5. 배포 · 운영 공백

- `WindowsPackageType=None`(unpackaged)만 있고 **MSIX/스토어 패키징 구성**(Package.appxmanifest, 인증서, 시그니처, 파워셸 스크립트) 없음. WindowsPackageType 값 결정(스토어 출시 시 `MSIX` 필요) 및 `Publisher`/`PublisherDisplayName` 누락.
- 스토어용 자산(로고 300x300, 배너 등) 부재 — `MauiIcon`은 플랫폼별 조건부(L111-118)라 Windows용 별도 아이콘 지정이 없어 공용 appicon.png로 대체되는지 확인 필요.
- 서버 측: `kagamine-rin.com/History/version_android|version_ios`만 존재 — **`version_windows` 추가 필요**(3번 항목의 업데이트 체크와 연동).
- 클라이언트 식별: `MauiProgram.cs` L43 `ApiHandler.Platform = DeviceInfo.Platform.ToString()` → "WinUI"로 전송됨. 서버에서 플랫폼별 로직/통계 처리 시 확인 필요(예: FCM 토큰 등록 플랫폼 게이트).
- 업데이트 안내 문구(`App.xaml.cs` 등)에 Play 스토어/TestFlight URL 하드코딩 다수 — Windows 경로 분기 필요.

---

## 6. 권장 로드맵 (우선순위순)

1. **csproj**: `net10.0-windows10.0.26100.0` 타깃 추가 + `Platforms\Windows` 기본 파일 생성 → 첫 빌드로 오류 목록 확정.
2. **의존성 정리**: `Xamarin.MediaGallery` 조건부 분리(Windows = FilePicker), `Plugin.Firebase.CloudMessaging` Windows 가드, `GoogleAuthService` Windows 구현.
3. **컴파일 오류 해소**: 1-4(TabBarBadge), 1-5(미디어 픽커 분기 7개 파일) 순으로 `#elif WINDOWS` 보강.
4. **기능 보완**: 업데이트 체크 분기 + `version_windows`, Windows 로컬 알림, WebView2 쿠키 헬퍼, Staggered 레이아웃 Windows 대응.
5. **배포 준비**: MSIX 패키징(스토어) 또는 sideload, 인증서, 스토어 자산, 서버 버전 파일, 실기기 QA.

---

## 근거(조사 출처 요약)

- `History.MobileClient.csproj` L1-165 (TargetFrameworks, PackageReference, 플랫폼 조건부 그룹)
- `MauiProgram.cs` L28-131 (Firebase 등록, 플랫폼 분기)
- `App.xaml.cs` L266-295 (`#if ANDROID || IOS` 폴링/포그라운드 처리)
- `Auth/` 폴더 3개 파일 (GoogleAuthService 공백)
- `ShellTabBarBadge/TabBarBadge.cs` + Platforms 분할 구현
- NuGet 패키지 nuspec/lib 목록 조회(TFM 판정)
- `Utils.cs` L797-829, `WebViewCookieHelper.cs` 전역, `CollectionViewHelper.cs` 전역, `StaggeredStructuredItemsViewHandler.cs` 전역
- 미디어 픽커 분기 전수: `EditPostPage`, `EditCommentPage`, `CreateStickerPage`, `PostPage`, `WriteMessagePage`, `HistoryProfileViewModel`, `KakaoProfileViewModel`
- `E:\Repos\SuggestingBox.Maui\SuggestingBox.Maui\SuggestingBox.Maui.csproj` (Windows TFM 26100)
