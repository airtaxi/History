# History 프로젝트 Copilot 지침

## 중요: CSharpStyleGuard 실행 금지
- 이 레포에서는 CSharpStyleGuard(`--fix`/`--check`)를 실행하지 마세요. 가드가 삼항 연산자(CSG0001) 등을 무조건 한 줄로 합쳐버려서 320자를 초과하는 가독성 없는 줄이 만들어지는 문제가 있습니다. 수동 포맷팅만 사용하세요.

## 중요 코드 스타일
- 모든 주석은 영어로 작성해주세요. (TODO 주석만 예외적으로 한국어 허용)
- 기존 코드베이스의 코딩 컨벤션과 스타일을 따라주세요.
- 한줄짜리 if 문이나 for 문 같이 괄호를 사용하는 경우, if (condition) myValue = true; 처럼 중괄호 없이 작성하며, 너무 긴 경우에는 (약 100자 이상) 한줄 넘김을 해주세요.
- 한줄짜리 메소드는 => 표현식을 사용해주세요.
- try-catch-finally도 마찬가지로 한줄인 경우 각 구문별로 한줄로 작성해주세요.
예:
try { /* code */ }
catch (Exception exception) { /* handle */ }
- 변수는 가능하면 줄임말을 사용하지 말아주세요. 예: ex -> exception, req -> request
- 가능하면 반드시 primary constructor를 사용해주세요.
- 가능하면 반드시 collection expression을 사용해주세요.
- C# 최신 문법을 적극 활용해주세요.
- 써드파티에서 이식한 소스(예: `ShellTabBarBadge` 플러그인)는 원본의 코드 스타일을 유지합니다. 이식 소스는 우리 코드 스타일의 기준이 아니므로, 새 코드를 작성할 때 이식 소스의 스타일을 참고하지 마세요.

## 프로젝트 개요

History.ApiService는 "History"라는 소셜 미디어 애플리케이션의 백엔드로 사용되는 .NET ASP.NET Core Web API 프로젝트입니다. MongoDB를 데이터베이스로 사용하며, JWT 인증을 구현하고, Firebase를 통해 푸시 알림과 Google/Apple OAuth 로그인을 통합합니다.

## 아키텍처

- **프레임워크**: .NET 10, ASP.NET Core
- **데이터베이스**: 공식 MongoDB.Driver를 사용한 MongoDB
- **인증**: JWT Bearer 토큰, Firebase, Apple/Google OAuth
- **아키텍처 패턴**: Controller -> Service -> Repository (MongoDB 컬렉션)
- **종속성 주입**: Program.cs에서 인터페이스로 서비스 등록
- **결과 패턴**: Result<T> 및 Result 타입을 사용한 작업 결과

## 주요 구성 요소

- **컨트롤러**: 표준 HTTP 상태 코드를 사용하는 REST API 엔드포인트
- **서비스**: 인터페이스를 구현한 비즈니스 로직
- **DataTypes**: 요청/응답 DTO 및 내부 데이터 구조
- **헬퍼**: 외부 통합을 위한 유틸리티 클래스 (Apple OAuth, 미디어 처리)
- **Enums/Constants**: History.Commons에 정의됨

## 컨트롤러

API에는 다음과 같은 컨트롤러가 있으며, 각 컨트롤러는 특정 도메인을 처리합니다:

- **PostController**: 포스트 CRUD 작업, 타임라인/공개 포스트, 리액션, 리포스트, 공유, 발견 옵션, 검색, 외부 URL 콘텐츠 채우기, 투표 기능, 관심글 (북마크)
- **GoogleController**: Google OAuth 인증 흐름 (로그인 URL 생성, 콜백 처리)
- **ReportController**: 신고 기록 관리 (생성, 보기, 삭제) 조정자 액세스 포함
- **CommentController**: 댓글 CRUD 작업, 좋아요, 포스트 권한 기반 액세스 제어
- **MediaController**: 캐싱 헤더를 사용한 미디어 파일 제공, 특별 생일 미디어 처리
- **FriendshipController**: 친구 요청 관리 (보내기/수락/거절/취소), 차단/무시, 즐겨찾기 친구, 친구 목록 검색
- **UserController**: OAuth를 통한 사용자 등록/로그인, 프로필 관리 (닉네임, 설명, 미디어), JWT 갱신, 사용자 검색, 알림, 조정자 기능
- **AppleController**: Apple OAuth 인증 흐름, JWT 토큰 생성
- **ModerationController**: 조정자에 의한 포스트/댓글 삭제, 조정 기록 검색
- **MessageController**: 개인 메시징 (보내기, 검색, 읽음 상태, 권한 확인)
- **StickerController**: 스티커 CRUD 작업 (생성, 조회, 검색, 삭제), 스티커 에셋 관리, 구독/구독취소, 최근 사용 기록

모든 컨트롤러는 속도 제한, 적절한 권한 부여, 일관된 오류 처리 패턴을 사용합니다.

## 서비스

서비스는 비즈니스 로직을 구현하며, 인터페이스를 통해 추상화됩니다. 주요 서비스는 다음과 같습니다:

- **UserService**: 사용자 CRUD, 프로필 업데이트 (닉네임, 설명, 생일, 미디어), 검색 허용 설정, 핸들 관리, 메모, 푸시 알림 권한, 메시지 수신 권한, 회원 탈퇴 처리
- **MediaService**: 미디어 업로드/변환/저장 (GridFS 사용), 썸네일 생성, 파일 삭제, 사용자별 미edia 관리
- **FriendshipService**: 친구 요청/수락/거절/취소, 차단/무시, 친구 목록 검색, 친구 관계 확인, 즐겨찾기 친구 관리
- **PostService**: 포스트 CRUD, 타임라인/공개 포스트 검색, 리액션/리포스트 처리, 발견 옵션 변경, 액세스 제어, 검색, 외부 URL 채우기, 투표 기능 (PollVote 관리), 관심글 (북마크) 관리
- **CommentService**: 댓글 CRUD, 좋아요, 액세스 제어, 응답 DTO 생성
- **NotificationService**: 푸시 알림 전송/삭제, Firebase 토큰 관리, 알림 필터링
- **MessageService**: 메시지 전송/검색/읽음 처리, 권한 확인, 응답 DTO 생성
- **ReportService**: 신고 생성/처리/삭제, 신고 기록 검색
- **ModerationService**: 포스트/댓글 삭제, 조정 기록 관리
- **RefreshTokenService**: JWT 리프레시 토큰 관리
- **BirthdayService**: 생일 알림 처리 (호스티드 서비스)
- **StickerService**: 스티커 CRUD, 스티커 에셋 관리, 스티커 검색, 비공식 스티커 액세스 제어, 구독/구독취소, 최근 사용 기록

서비스는 Result 패턴을 사용하며, 데이터베이스 작업은 async/await로 처리됩니다.

## 코딩 표준

### 명명 규칙

- 클래스: PascalCase
- 메서드: PascalCase
- 속성: PascalCase
- 프라이빗 필드: _camelCase (밑줄 접두사)
- 인터페이스: IPascalCase
- 열거형: PascalCase

### 코드 스타일

- 암시적 using 사용 (`ImplicitUsings` 활성화)
- nullable 참조 타입 비활성화 (`Nullable` disable)
- C# 12 기능 허용 (`LangVersion` preview)
- 모든 I/O 작업에 async/await 사용
- LINQ를 사용한 데이터 조작
- 문자열 보간 `$"{variable}"`

### 오류 처리

- 서비스 메서드에 Result<T> 사용
- 컨트롤러는 Result.Error에 따라 적절한 HTTP 상태 코드 반환
- 오류 로그 기록하지만 클라이언트에 내부 세부 사항 노출하지 않음

### 데이터베이스 작업

- MongoDB.Driver를 사용한 타입화된 컬렉션
- 복잡한 쿼리에 Builders 사용
- 모든 작업에 async 사용
- Program.cs에서 열거형을 문자열로 직렬화 구성

### 삭제 로직 동기화 규칙

- `History.ApiService`의 `PostService.DeletePostAsync(...)`는 단건 삭제, `PostService.DeletePostsAsync(...)`는 대량 삭제(배치) 경로입니다.
- 단건 삭제 로직(`DeletePostAsync`)을 수정할 때는 **반드시** 대량 삭제 로직(`DeletePostsAsync`)도 함께 업데이트하여,
  삭제 대상 컬렉션/외부 리소스(미디어/알림/신고/관심글 등) 정리 범위가 서로 일치하도록 유지합니다.

### 타임라인 콘텐츠 템플릿 동기화 규칙

- 타임라인/발견/리포스트/공유 포스트는 `BindableLayout` 대신 고정 슬롯 기반의 `TimelineContentsTemplate`(`Resources/Styles/Content.xaml`)을 사용합니다.
- `BasePostViewModel`에서 파생된 플랫폼별 포스트 뷰모델(`HistoryPostViewModel`, `KakaoPostViewModel`)에서 `ContentTemplateSelector`가 처리하는 새 콘텐츠 타입(`IContentViewModel` 구현)이 추가되면, **반드시** `TimelineContentsViewModel`(`ViewModels/TimelineContentsViewModel.cs`)에 해당 타입 슬롯과 `IsVisible` 플래그를 추가하고, `TimelineContentsTemplate` XAML에 `DataTemplatePresenter` 슬롯을 추가해야 합니다.
- `PostContentTemplate`(PostPage 상세)과 `CommentTemplate`은 전체 콘텐츠를 순서대로 표시해야 하므로 `BindableLayout` + `ContentTemplateSelector`를 유지합니다.

### 게시글 조회 메신저 갱신 규칙

- 히스토리든 카카오스토리든 게시글을 조회(`GetPost`/`KakaoStoryApiHandler.GetPost`)해서 새 데이터를 얻게 되면, 네비게이션(PostPage 푸시) 여부와 무관하게 **반드시** WeakReferenceMessenger로 갱신을 알립니다.
  - 히스토리: `WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post));`
  - 카카오스토리: `WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(post));`
- 갱신 전송은 성공 분기 안에서만 하며, 조회 결과가 null이 아님이 확인된 뒤에 보냅니다(실패 시 null 메시지 전송 금지).
- 이미 `RefreshAsync`가 위 메시지를 전송하므로, `RefreshAsync`를 경유하는 경로는 추가 전송이 불필요합니다. `RefreshAsync`를 경유하지 않고 직접 조회하는 경로(앱 링크, 알림 탭, 상호작용 목록 탭, 임베디드 카드 탭 등)에서 누락하지 않아야 합니다.

### 플랫폼 포스트/댓글 ViewModel 분리 규칙

- `BasePostViewModel`/`BaseCommentViewModel`(`History.MobileClient/ViewModels/`)은 **DTO·업데이트 로직·메신저 없이** 공통 UI 표면(`[ObservableProperty] protected set`)과 `[RelayCommand]` virtual 명령 계약만 보유합니다.
- `HistoryPostViewModel`/`HistoryCommentViewModel`이 이들을 상속하며 DTO 보유, `UpdatePost`/`UpdateComment`로 베이스 표면 채우기, 메신저 등록, API/네비게이션 로직을 담당합니다.
- 명령(`[RelayCommand]`)은 **베이스에만** 선언하고 파생은 순수 `override`합니다(파생에 재선언 시 MVVMTK0023 중복 명령 오류).
- 신규 플랫폼(예: 카카오스토리)은 별도 DtoMapper 없이 `Base*` 상속 + 자기 데이터로 표면 채우기로 바로 통합합니다.

### 카카오스토리 구현 대칭 원칙

- 카카오스토리 기능을 구현할 때는 히스토리의 기존 기능 구현을 **최대한 대칭적으로** 따릅니다. 코드 스타일, 개행, 메소드 위치/순서, 네이밍, 구조까지 히스토리 구현과 일치시키는 것을 지향합니다. 예: `KakaoPostViewModel`은 `HistoryPostViewModel`과 메소드 순서·구조가 대칭이 되도록 작성합니다.
- 가능하면 템플릿은 그대로 유지합니다. 템플릿(`Post.xaml` 등) 수정이 필요하면 최소화하고, 기존 템플릿의 구조를 유지하는 방향으로 구현합니다.
- 베이스 뷰모델과 플랫폼별 뷰모델을 분리합니다: `BasePostViewModel`(공통 UI 표면/명령 계약) → `HistoryPostViewModel`/`KakaoPostViewModel`(플랫폼별 구현).
- `Post.xaml`의 `PostTemplate`/`PostContentTemplate`/`PostPreviewTemplate` 등이 `x:DataType="vm:BasePostViewModel"`로 베이스 뷰모델을 공유하는 사례와 같이, 템플릿은 베이스 뷰모델 타입에 바인딩하고 플랫폼별 뷰모델이 이를 상속·구현하는 방식을 지향합니다. 신규 플랫폼 뷰모델 추가 시 템플릿의 바인딩 대상 타입은 바꾸지 않고 베이스 뷰모델의 표면만 확장합니다.

### 카카오스토리 프로필 미디어 규칙

- 카카오 API의 `profile_video_url*` 필드(움직이는 프로필 영상)는 절대 사용하지 마세요. MAUI에서 타임라인 등 리스트에 사용하면 이미지 디코더가 버티지 못하는 이슈가 있습니다.
- 카카오 프로필 이미지 매핑 규칙 (히스토리 미디어 ID 대응):
  - `ThumbnailMediaId`(목록/썸네일 표시) → `profile_image_url`
  - `MediaId`(프로필 상세/풀스크린 표시) → `profile_image_url2`
- TODO: `profile_image_url2` 사용처를 모두 구현하고 나면 이 매핑 규칙 안내는 삭제하세요.

### 앱 링크 (App Links / Universal Links)

- `https://historyweb.cc/post/{postId}` / `https://historyweb.cc/u/{userId}` 링크는 외부 URL 내용 탭(`Utils.OpenLinkAsync`)뿐만 아니라 네이티브 앱 링크로도 처리됩니다. `Utils.OpenLinkAsync`가 게시글/프로필 이동, 그 외는 브라우저를 엽니다.
- 서버 측 검증 파일: `https://historyweb.cc/.well-known/assetlinks.json` (Android, `com.airtaxi.history` + 로컬/스토어 키스토어 SHA256 지문 2개) / `https://historyweb.cc/apple-app-site-association` (iOS, `UP6EXS2HJJ.com.airtaxi.history`, paths `/post/*`, `/u/*`). 두 파일 모두 JSON MIME 타입으로 200 응답해야 합니다.
- 수신 경로: Android는 `MainActivity`의 `[IntentFilter(Action.View, AutoVerify)]` → `HandleIntent`의 `Intent.DataString` → `App.HandleAppLinkAsync` → `Utils.OpenLinkAsync`. iOS는 `Entitlements.plist`의 `applinks:historyweb.cc` (사이트에 Associated Domains capability 필요) → `AppDelegate.ContinueUserActivity`(NSUserActivity.WebPageUrl) → 동일.
- 콜드 스타트(로그인 전)에는 URL을 `Preferences`의 `AppLinkUrlPending` 키에 보관 후 `LoginPage.AfterLogin`의 `App.ReplayPendingAppLinkUrl()`로 재생하며, `App.HandleKakaoStoryNotificationAsync` 패턴을 미러링합니다.

### 보안

- 미들웨어에서 JWT 검증
- User.FindFirst(ClaimTypes.NameIdentifier)를 통한 사용자 클레임 액세스
- 서비스에서 액세스 제어 확인
- DotNet.RateLimiter를 사용한 속도 제한

### 미디어 처리

- 폼 데이터를 통한 미디어 업로드
- 고유 ID로 처리 및 저장
- 비디오/이미지에 썸네일 생성
- MIME 타입 검증
- MediaId만 있으면 URL 생성 가능 /api/{mediaId}

### 알림

- Firebase Cloud Messaging을 통한 푸시 알림
- 열거형으로 정의된 알림 타입
- 작업 후 비동기 전송

### 검증

- Utils.cs에서 입력 정화
- 서비스에서 비즈니스 규칙 검증
- 길이 제한 및 콘텐츠 확인

## 일반 패턴

### 서비스 메서드 구조

```csharp
public async Task<Result<T>> MethodNameAsync(params)
{
    // 검증
    if (invalid) return (ErrorType.BadRequest, "message");

    // 데이터베이스 작업
    var data = await _collection.Find(filter).ToListAsync();

    // 비즈니스 로직
    // ...

    return result;
}
```

### 컨트롤러 메서드 구조

```csharp
[HttpVerb("route")]
[ProducesResponseType<T>(200)] // T: result.Value 타입
[ProducesResponseType<string>(400)] // 발생할 수 있는 모든 오류 매핑
public async Task<IActionResult> MethodName(params)
{
    var requesterId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (requesterId == null) return Unauthorized();

    var result = await _service.MethodAsync(params);
    if (result.IsSuccess) return Ok(result.Value);
    else if (result.Error == ErrorType.NotFound) return NotFound(result.ErrorMessage);
    // ... 발생할 수 있는 모든 다른 오류 매핑
    else return StatusCode(500, result.FullErrorMessage);
}
```

### MongoDB 쿼리

- 필터, 업데이트, 정렬에 Builders 사용
- 성능을 위해 필요한 필드만 프로젝션
- fromPostId 및 CreatedAt 비교로 페이지네이션 처리

### 액세스 제어

- 프라이버시 설정에 대한 친구 관계 확인
- 조정자 (Rank >= Moderator)는 제한 우회
- CheckAccessAsync 메서드를 사용한 권한

## 종속성

- Microsoft.AspNetCore.* 웹 프레임워크용
- MongoDB.Driver 데이터베이스용
- FirebaseAdmin 알림용
- System.IdentityModel.Tokens.Jwt JWT용
- RestSharp HTTP 클라이언트용
- BouncyCastle 암호화용

## 테스트

- 단위 테스트는 서비스 및 데이터베이스 모킹
- 통합 테스트는 전체 API 흐름
- 테스트 MongoDB 인스턴스 사용

## 배포

- Docker/Aspire용 구성
- appsettings.json의 환경별 설정
- Firebase 서비스 계정 키 필요
- MongoDB 연결 문자열 구성 필요

## 중요한 참고 사항

- 보안을 위해 모든 텍스트 콘텐츠 정화
- 미디어 ID는 GUID 문자열
- 타임스탬프는 DateTime.UtcNow 사용
- 사용자 대면 응답에 한국어 오류 메시지
- 남용 방지를 위한 속도 제한 적용
- 특정 출처에 CORS 구성
- 새로운 컨트롤러 또는 서비스를 추가할 때는 이 지침 파일(AGENTS.md)을 업데이트하여 새로운 구성 요소를 문서화하십시오.

## MAUI (모바일 클라이언트)

History.MobileClient는 .NET MAUI를 사용한 크로스 플랫폼 모바일 애플리케이션입니다.

### 아키텍처

- **패턴**: MVVM (Model-View-ViewModel) with CommunityToolkit.Mvvm
- **네비게이션**: Shell 기반 탭 네비게이션
- **메시징**: WeakReferenceMessenger 사용하는 중
- **스타일링**: XAML 리소스 딕셔너리, UraniumUI, Syncfusion 테마
- **플랫폼별 코드**: Platforms 폴더에 iOS/Android 특정 구현

### 주요 구성 요소

- **Pages**: XAML 기반 UI 페이지 (예: TimelinePage, UserPage, StickersPage, StickerDetailPage)
- **ViewModels**: CommunityToolkit.Mvvm ObservableObject 상속, 명령 및 속성 바인딩
- **ContentViews**: 재사용 가능한 UI 컴포넌트 (UserCollectionView, StickerCollectionView)
- **Behaviors**: 사용자 상호작용 처리 (예: SwipeToCloseBehavior)
- **Helpers**: 플랫폼별 유틸리티 (미디어 피커, 웹뷰 쿠키)
- **DataTypes**: 메시징 및 데이터 전송용 클래스
- **Enums**: 상호작용 타입, 포스트 타입 등
- **KakaoStoryNotificationPoller**: 카카오스토리 알림 폴링 공용 로직 (포그라운드 2.5초 / Android 백그라운드 15분 JobScheduler / iOS BGAppRefreshTask 공유). 전체 알림 목록을 주기 폴링하고 최신 알림 ID 베이스라인 이후의 새 알림만 로컬 알림으로 표시. 알림 fetch API가 쪽지 이벤트를 전달하지 않으므로 쪽지 목록(`GetMails`)도 별도 베이스라인으로 스캔하여 `type == "receive"` && `read_at == null`인 새 쪽지만 로컬 알림으로 표시. 401 시 로그인 UI를 절대 띄우지 않음. 탭 바 배지는 이 폴러와 무관하며 `TabBarBadgePoller`와 목록 페이지들이 관리함. 포그라운드 루프는 매 사이클 `App.IsForeground`을 확인해 윈도우 이벤트가 미발화해도 백그라운드 폴링이 불가능하며, `IsPollLoggingEnabled`(true)일 때 ADB(logcat)에 `[HH:mm:ss.fff]` 타임스탬프 로그를 남김.
- **TabBarBadgePoller**: 탭 바 배지 전용 포그라운드 폴러 (10초). History 알림(`HistoryUnreadNotificationCount`), 받은 쪽지(`HistoryUnreadMailCount`), 친구 요청(`HistoryPendingFriendRequestCount`)과 카카오스토리 알림/쪽지/초대장(`KakaoStory*Count`)을 폴링하여 배지 카운트를 갱신. 카카오스토리 알림 설정(`KakaoStoryNotificationEnabled`)과 독립적이라 알림이 꺼져 있어도 배지는 계속 갱신됨. 카카오 구간은 `IsBackgroundMode = true` + `MaxRetryCount = 2`로 로그인 모달을 띄우지 않음. 카카오스토리 알림/쪽지/친구 신청 배지 합산은 SettingsPage의 `KakaoStoryNotificationBadgeEnabled`/`KakaoStoryMailBadgeEnabled`/`KakaoStoryFriendRequestBadgeEnabled` 설정으로 각각 켜고 끌 수 있으며, 꺼진 카테고리는 배지 폴러가 폴링하지 않음. 포그라운드 루프는 매 사이클 `App.IsForeground`을 확인해 백그라운드 폴링을 차단하며, `IsPollLoggingEnabled`(true)일 때 ADB(logcat)에 `[HH:mm:ss.fff]` 타임스탬프 로그를 남김.
- **KakaoStoryNotificationPoster** (Android/iOS): 카카오스토리 알림을 로컬 알림으로 표시 (Android: 기존 push 채널 `{PackageName}.push` / iOS: UNUserNotificationCenter, Firebase 미사용). 탭하면 scheme 기반으로 게시글/프로필 이동. 쪽지 알림은 커스텀 scheme `kakaostory://messages/{id}`로 쪽지 상세(MessagePage)를 열며, 제목은 `{보낸 사람}님이 쪽지를 보냈습니다` 형식.
- **KakaoStoryNotificationRefreshService** (Android): 백그라운드 알림 폴링 JobService (15분 주기, `KakaoStoryNotificationJobId = 2`).
- **KakaoStoryRealtimeNotificationService** (Android): 상시 알림 기반 포그라운드 서비스 (dataSync 타입, 전용 채널 `{PackageName}.realtime` 무음/ongoing). 백그라운드에서 10초마다(절전모드 `PowerManager.IsPowerSaveMode`에서는 1분마다) `KakaoStoryNotificationPoller.PollOnceAsync()`를 호출해 카카오스토리 알림을 실시간에 가깝게 수신. 대기 주기는 매 사이클 재평가되므로 절전 모드 변경이 다음 사이클부터 반영됨. 앱이 포그라운드인 동안에는 2.5초 폴러가 같은 목록을 커버하므로 폴링을 건너뜀(`App.IsForeground`). `IsPollLoggingEnabled`(true)일 때 ADB(logcat)에 `[HH:mm:ss.fff]` 타임스탬프 로그를 남김. 설정 키 `KakaoStoryRealtimeNotificationEnabled`(기본 ON)로 켜고 끄며, 활성화 시 SettingsPage에서 상시 알림 안내 확인(PromptOk/PromptCancel)을 거침. Android 15+ dataSync 6시간 제한으로 시스템이 중지하면 `OnTimeout`에서 정리되고, 앱을 다시 열면 `MainActivity.OnCreate`가 재시작(그 사이 15분 JobService가 폴백). 로그아웃 시 `CleanupSharedVariables`에서 중지. 시작/중지는 `KakaoStoryRealtimeNotificationManager`가 담당하며, `KakaoStoryUtils.EnsureLoggedInAsync`의 카카오스토리 로그인 성공 경로에서도 `StartIfEnabled`로 재무장(이미 실행 중이면 no-op).
- **KakaoStoryNotificationDelegate** (iOS): UNUserNotificationCenter 델리게이트 소유자. 카카오 알림은 직접 처리(포그라운드 배너/탭 네비게이션), 그 외는 Firebase 플러그인으로 콜백 forwarding 하여 기존 FCM 푸시 동작 보존. AppDelegate.FinishedLaunching에서 Firebase 플러그인이 설정한 델리게이트를 교체.
- **KakaoStoryBackgroundRefresh** (iOS): 백그라운드 알림 폴링 BGAppRefreshTask (`com.airtaxi.history.kakaostoryrefresh`, 시스템 결정 시점 ~15분 간격). 앱이 백그라운드 진입 시(Window.Stopped) 다음 실행을 예약.

### Blazor 타임라인 (BlazorTimelinePage)

`BlazorTimelinePage`는 타임라인 피드를 BlazorWebView로 렌더링하는 대체 구현입니다 (`TimelinePage` → `TimelineViewModel` 로직 이식):

- **WebView 하이버네이션 공통**: 모든 Blazor 페이지는 `WeakReferenceMessenger`의 `BlazorWebViewHibernationMessage`(bool)를 수신해 앱이 백그라운드로 가면(`App.OnWindowStopped`) Android `WebView.OnPause()`를, 포그라운드 복귀(`OnWindowResumed`) 시 `OnResume()`을 호출. OnPause는 JS 타이머/애니메이션/영상 재생을 정지시키므로, 실시간 알림 FGS가 프로세스를 살려두어도 백그라운드 Blazor 탭이 CPU를 태우지 않음.
- **BlazorTimelinePage** (`Pages/BlazorTimelinePage.xaml`): `BlazorWebView` 호스트 페이지. Android 핸들러에서 `HapticFeedbackEnabled = false`(네이티브 롱클릭 햅틱 차단), `MediaPlaybackRequiresUserGesture = false`(인라인 영상 autoplay 허용), 테마 색 WebView 배경 지정.
- **TimelineViewModel** (`ViewModels/TimelineViewModel.cs`): 포스트 로드/새로고침/모드 전환 로직. `TimelinePage.xaml.cs`에서 이식.
- **Components/Timeline/**: Blazor 컴포넌트. `Timeline.razor`(피드 루트), `PostCard.razor`, `CommentPreview.razor`, `MediaCarousel.razor`, `TextContents.razor`, `PollCard.razor`, `ExternalUrlCard.razor`. `MvvmCardBase<T>`가 MAUI의 BindingContext를 대체 (PropertyChanged 구독 → 재렌더).
- **MasonryFeed.razor**: 타임라인 피드의 공용 렌더러 (피드 마크업/무한스크롤/인터롭 init/Dispose/스크롤탑/풀투리프레시). `IFeed`(IBlazorFeedViewModel) 파라미터로 데이터 소스와 무관하게 동작하며, `Header`(스크롤 영역 상단 콘텐츠, 타임라인 pills), `ItemTemplate`(XAML ItemTemplateSelector 대응, 기본 PostCard), `EnablePullToRefresh`(검색 페이지는 false) 파라미터 제공. `Timeline.razor`는 pills + MasonryFeed 조합으로 축소됨.
- **wwwroot/timeline-interop.js**: DOM 전용 로직 (테마, 무한 스크롤, 캐러셀 인디케이터/높이, 당겨서 새로고침, 롱프레스 복사, IntersectionObserver 기반 영상 시야 이탈 리셋, 피드 영상 강제 뮤트, Masonry 스태거드).
- **wwwroot/masonry.pkgd.min.js**: 로컬 번들 Masonry.js 4.2.2 (오프라인 동작). `TimelinePage.OnSizeChanged`와 동일하게 `floor(내부폭 / 700) + 1` 컬럼으로 스태거드 배치 — 1컬럼은 일반 문서 흐름(LinearItemsLayout 대응), 2컬럼 이상만 Masonry 활성화(`#masonry-grid.masonry-active`).
- **wwwroot/timeline.css**: 피드 스타일. `data-theme` 속성 기반 다크/라이트 테마. `index.html`의 인라인 스크립트가 `prefers-color-scheme`으로 첫 페인트 전 테마를 선적용해 다크 모드 흰색 플래시 방지.

### Blazor 프로필 (BlazorUserPage)

`BlazorUserPage`는 사용자 프로필을 BlazorWebView로 렌더링하는 대체 구현입니다 (`UserPage` → `UserProfileViewModel` 로직 이식). 기존 `UserPage`는 다른 페이지들이 설정하는 정적 `ShouldRefresh`/`ShouldRefreshKakaoStory` 플래그 보관용 데드코드로 유지됩니다:

- **BlazorUserPage** (`Pages/BlazorUserPage.xaml`): 헤더(뒤로가기/타이틀/레이아웃 토글/쪽지/메모/친구/차단/설정 아이콘), `BlazorWebView`, 글쓰기 FAB, 스크롤 탑 버튼을 네이티브 크롬으로 유지. 헤더 아이콘 visibility는 `UserProfileViewModel` INPC 프로퍼티에 바인딩. Android 핸들러 설정(햅틱/autoplay/테마 배경/`ApplyWebViewSize`)은 BlazorTimelinePage와 동일.
- **UserProfileViewModel** (`ViewModels/UserProfileViewModel.cs`): `UserPage.xaml.cs`에서 이식한 로드/새로고침/모드 전환/레이아웃 토글/헤더 액션 로직. `ProfileVm`(`BaseProfileViewModel`)과 `Items`(`BasePostViewModel` 컬렉션) 소유. `IsMyProfileTab`(파라미터리스 생성자=내 프로필 탭)과 `ShowPillGrid`를 별도 보유.
- **Components/Profile/**: `Profile.razor`(루트), `ProfileCard.razor`(`ProfileTemplate` 이식 — 배경/프로필 미디어, 즐겨찾기 별, 액션 버튼, 프로필 이미지 롱프레스는 `attachLongPress` 사용), `PostPreviewCard.razor`(`PostPreviewTemplate` 이식 — 3열 그리드 셀).
- **그리드 모드 ↔ 타임라인 모드**: `UseGridLayout`에 따라 `.preview-grid`(CSS grid 3열, `GridItemsLayout` Span=3/Spacing=1 대응)와 `#masonry-grid` + `PostCard` 재사용을 전환. 레이아웃 토글은 리사이즈 없이 DOM만 바뀌므로 `Profile.razor`가 `timelineInterop.initMasonry`/`destroyMasonry`를 명시 호출.
- **wwwroot/profile.css**: 프로필 카드/미리보기 그리드 스타일 (`index.html`에서 timeline.css 뒤에 로드).
- 유의: XAML `CollectionView` 기반 기능(RecyclerView 가상화 설정, iOS 스크롤 위치 저장/복원, 1초 스크롤 폴링)은 Blazor 패턴(IntersectionObserver/scroll 이벤트)으로 대체됨.

### Blazor 서브 피드 페이지 (발견/검색/관심글)

`BlazorPublicPostsPage`(발견), `BlazorSearchPostsPage`(게시글 검색), `BlazorBookmarkedPostsPage`(관심글)는 각각 `PublicPostsPage`/`SearchPostsPage`/`BookmarkedPostsPage`의 Blazor 이식입니다. 셋 다 `MasonryFeed`를 공유 피드로 사용하며 페이지 크롬(헤더/검색바/빈 상태/스크롤탑/인디케이터)은 네이티브로 유지합니다:

- **뷰모델**: `PublicPostsViewModel`(`GetPublicPosts` 페이징, `PublicPostsPage.ShouldRefresh` 체크), `SearchPostsViewModel`(`SearchAsync(query)`/LoadMore, `IsEmptyVisible`), `BookmarkedPostsViewModel`(`GetBookmarkedPosts` 20개 페이징, `PostUnbookmarkedMessage` 처리, `IsEmptyVisible`, 스크롤탑 표면) — 모두 `IBlazorFeedViewModel` 구현. 코드비하인드(페이지)에서 이식.
- **루트 컴포넌트**: `PublicPosts.razor`(ItemTemplate 셀렉터: `HistoryPublicPostViewModel` → PublicPostCard, 리포스트 → PostCard), `SearchPosts.razor`(풀투리프레시 비활성), `BookmarkedPosts.razor`.
- **PublicPostCard.razor**: `PublicPostTemplate` 이식 — 카드 탭이 프로필 이동(`HandleProfileTapCommand`), 더보기는 `HandlePublicPostMoreTapCommand`, 액션 행/댓글 프리뷰 없음, 공유 섹션 유지.
- **네이티브 크롬**: 검색 페이지는 네이티브 SearchBar 유지(검색 실행 시 키보드 숨김, iOS 소프트 키보드 SafeAreaEdges). 관심글 페이지는 빈 상태 오버레이(네이티브) + **스크롤탑 버튼 포함**(레거시 XAML 페이지의 누락 버그 수정).
- 구 페이지 3개는 데드코드로 유지(`PublicPostsPage.ShouldRefresh`는 HistoryPostViewModel/BulkPostManagePage/MorePage에서 설정되므로 필수 보관).

### 레거시 XAML 페이지 (취급 불필요)

다음 XAML 페이지는 각각 Blazor 버전으로 대체되어 **레거시**로 간주합니다. 코드 수정·리팩토링·스타일 참고 대상에서 제외하고, 신규 기능 구현 시 Blazor 버전을 기준으로 삼습니다:

- `Pages/TimelinePage.xaml` → `Pages/BlazorTimelinePage.xaml` (타임라인)
- `Pages/UserPage.xaml` → `Pages/BlazorUserPage.xaml` (프로필)
- `Pages/PublicPostsPage.xaml` → `Pages/BlazorPublicPostsPage.xaml` (발견)
- `Pages/SearchPostsPage.xaml` → `Pages/BlazorSearchPostsPage.xaml` (게시글 검색)
- `Pages/BookmarkedPostsPage.xaml` → `Pages/BlazorBookmarkedPostsPage.xaml` (관심글)

단, `UserPage`(정적 `ShouldRefresh`/`ShouldRefreshKakaoStory` 플래그)와 발견/검색/관심글 페이지 3개(정적 `PublicPostsPage.ShouldRefresh` 플래그)는 다른 페이지들이 설정하는 정적 플래그 보관용 데드코드로 유지되므로 **삭제하지 않습니다**. 해당 정적 플래그 프로퍼티와 코드비하인드의 기본 골격만 유지하고, 그 외 로직은 취급하지 않습니다.

### 앱 구조

앱쉘(AppShell.xaml)은 다음 탭으로 구성됩니다:
- **타임라인**: 친구들의 포스트 피드
- **알림/쪽지**: 알림 및 개인 메시지
- **친구**: 친구 관리 (목록, 추가, 요청 등)
- **더보기**: 발견(공개 포스트), 스티커 등 추가 기능
- **프로필**: 사용자 프로필

### 스티커 시스템

스티커는 게시글 및 댓글에서 사용 가능한 커스텀 이미지 에셋입니다:
- **StickersPage**: 스티커 목록 및 검색
- **StickerDetailPage**: 스티커 상세 정보, 에셋 보기, 구독/구독취소 버튼
- **CreateStickerPage**: 새 스티커 생성 (아이콘, 이름, 카테고리, 에셋 업로드)
- **StickerCollectionView**: 글쓰기/댓글에서 스티커 선택 UI (탭 바 + 에셋 그리드)
- **MentionsViewModel**: % 문자로 스티커 표시, 탭 선택 및 최근 사용 로드

스티커 특징:
- 누구나 생성 가능, 비공개 옵션 지원
- 최대 384x384 크기, 정적 이미지 및 움짤(GIF/WebP) 지원 (동영상 불가)
- 최대 50개 에셋, 각 파일 5MB 이하
- 삭제는 본인 또는 모더레이터만 가능
- **구독 기능**: 다른 사용자의 공개 스티커를 구독하여 빠르게 접근
- **최근 사용**: 스티커 에셋 사용 시 자동 기록, 최대 50개 저장

스티커 선택 UI (StickerCollectionView):
- 상단 탭 바: 최근 사용(시계 아이콘) + 구독/본인 스티커 아이콘 탭
- 스티커 이름 라벨: 현재 선택된 스티커 이름 표시
- 에셋 그리드: 4열 그리드로 스티커 에셋 표시
- 스티커 선택 시 사용 기록 자동 전송

## 코딩 표준 (MAUI)

- **XAML**: 명명된 스타일 사용, 바인딩 모드 명시적 지정, FontImageSource로 아이콘, DataTemplate로 재사용 가능한 UI, x:DataType로 타입화된 바인딩
- **ViewModels**: ObservableProperty, RelayCommand 사용, API 호출을 위한 async 메서드 (반드시 HistoryPostViewModel.cs 참고)
- **네비게이션**: App.PushAsync/PopAsync 정적 메서드 사용
- **API 호출**: App.ExecuteRequestAsync 사용, 로딩 상태 관리
- **메시징**: WeakReferenceMessenger.Default.Send/Receive
- **컬렉션**: 동적 목록을 위한 ObservableCollection
- **CollectionView**: 프로필 섹션을 위한 Header, 페이지네이션을 위한 RemainingItemsThreshold, 다른 타입을 위한 ItemTemplateSelector (반드시 PostPage 참고)
- **Data Templates**: 포스트, 댓글, 미디어의 조건부 렌더링을 위한 DataTemplateSelector
- **Behaviors**: 스와이프나 탭 같은 상호작용을 위한 커스텀 비헤이비어
- **ContentViews**: x:Name으로 코드 비하인드 액세스를 위한 재사용 가능한 컴포넌트

### 일반 패턴 (MAUI)

- **페이지 구조**: 레이아웃을 위한 XAML, 초기화 및 이벤트 처리를 위한 코드 비하인드, 설정을 위한 Loaded 이벤트
- **ViewModel 구조**: 데이터 바인딩을 위한 속성, 액션을 위한 명령, API 호출을 위한 async 메서드, 목록을 위한 ObservableCollection
- **데이터 템플릿**: 포스트, 프로필, 댓글 같은 다른 아이템 타입을 위한 DataTemplateSelector, 동적 콘텐츠를 위한 BindableLayout
- **컬렉션 뷰**: 풀투리프레시 필요한 경우 위한 RefreshView 사용. (반드시 TimelinePage 참고)
- **모달**: 오버레이(login, editor)를 위한 PushModalAsync, 네비게이션을 위한 PushAsync
- **토스트/알럿**: CommunityToolkit.Maui.Alerts 사용
- **플로팅 버튼**: 포스트 작성 같은 액션을 위한 TapGestureRecognizer가 있는 Border (반드시 TimelinePage 참고)
- **헤더**: 네비게이션 및 액션을 위한 이미지와 레이블이 있는 Grid, 상태 바 스타일링을 위한 StatusBarBehavior (주황색: TimelinePage 참고, 검정색: 
  - 버튼이 있는 헤더: 백 버튼, 타이틀, 액션 버튼(예: 검색)이 있는 Grid (반드시 TimelinePage 참고)
  - 버튼이 없는 헤더: 백 버튼과 타이틀만 있는 Grid (SettingsPage 예시)
- **미디어**: 이미지를 위한 CachedImage, 비디오를 위한 플랫폼별 처리 (반드시 Content.xaml 및 Media.xaml 참고)
- **텍스트 입력**: 텍스트 입력 (반드시 TextContentView.xaml, EditPostPage 참고)
- **미디어 첨부**: 반드시 EditPostPage.xaml.cs, EditCommentPage.xaml.cs 구현 둘 다 참고
- **반응형 디자인**: 폰/태블릿 적응형 레이아웃을 위한 SizeChanged 이벤트 (반드시 TimelinePage 참고)

### 종속성 (MAUI)

- MVVM 지원을 위한 CommunityToolkit.Maui/Mvvm
- 아이콘을 위한 UraniumUI.Icons/Material
- 고급 컨트롤을 위한 Syncfusion.Maui.Toolkit
- 이미지 캐싱을 위한 FFImageLoading.Maui
- 푸시 알림을 위한 Plugin.Firebase.CloudMessaging

## 응답 지침

해당하는 프로젝트의 파일 구조를 먼저 읽은 뒤 진행하십시오.
모든 응답은 한국어로 제공하십시오.


## 기타
Windows 환경에서 iOS 빌드 오류는 무시해도 괜찮습니다.
`net10.0-android` 빌드에서 `XALNS7015` 오류(Writing mixed-mode assemblies is not supported)가 발생해도 빌드 성공이므로 무시해도 됩니다.

빌드 검증 시에는 반드시 `net10.0-android` 대상 프레임워크로 검증하십시오.