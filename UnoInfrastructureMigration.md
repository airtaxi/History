# History MAUI -> Uno Platform — 4단계 인프라 이식 계획

> **목적**: 이 문서는 `History.MobileClient`(MAUI)의 플랫폼별 인프라 기능 중 **푸시 알림(FCM) + 백그라운드 JWT 리프레시 + 매니페스트/권한/설정 파일**을 `History.Uno`(Uno Platform)로 이식하기 위한 완전한 구현 지침이다. 이 문서만 있으면 이전 대화 맥락 없이도 새 에이전트가 처음부터 구현할 수 있다.

> **중요 규칙**: 이식 작업 중 **빌드 오류가 발생하면 해결을 시도하지 말고 즉시 중단 후 사용자에게 해결을 요청**하라. 오류 메시지 전문과 발생 위치를 사용자에게 전달한다. 자의적으로 코드를 수정하여 오류를 우회하지 마라.

---

## 0. 사전 지식

### 0.1 프로젝트 구조

```
E:\Repos\History\
├── History.Commons\              # 공유 라이브러리 (DTO, API, Enums, Configuration) — 플랫폼 독립
├── History.MobileClient\         # MAUI 클라이언트 (이전 대상)
├── History.Uno\                  # Uno Platform 클라이언트 (이전 결과)
│   ├── History.Uno\              # 메인 Uno 프로젝트
│   │   ├── Platforms\
│   │   │   ├── Android\          # Android 플랫폼 코드
│   │   │   └── iOS\              # iOS 플랫폼 코드
│   │   ├── DataTypes\            # 메시징 클래스 (이미 이식됨)
│   │   ├── Models\
│   │   ├── Strings\
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── MainPage.xaml / .cs
│   │   ├── Shared.cs
│   │   ├── Utils.cs
│   │   ├── GlobalUsings.cs
│   │   ├── appsettings.json
│   │   ├── app.manifest
│   │   └── History.Uno.csproj
│   ├── History.Uno.MauiControls\ # MAUI Embedding용 컨트롤
│   ├── Directory.Packages.props  # Central Package Management
│   ├── Directory.Build.props
│   └── Directory.Build.targets
└── UnoMigration.md               # 전체 마이그레이션 계획
```

### 0.2 현재 상태

- **1단계 완료**: `History.Commons` 참조, `Constants`, `Shared.cs`, `Configuration`, `App.xaml.cs`(API 호출, 네비게이션, 알럿), `Utils.cs`(플랫폼 독립 부분), `DataTypes/` 메시지 클래스, `Enums/` — 빌드 통과
- **2단계 미착수**: 핵심 페이지(`LoginPage`, `AppShell`, `TimelinePage`, `UserPage` 등)가 아직 Uno에 없음
- **4단계 본 작업**: 페이지 없이도 구축 가능한 인프라(FCM, JWT 리프레시, 매니페스트)를 먼저 이식

### 0.3 핵심 참조 레포지토리

`E:\Repos\Adoz\AdozMobile` — **Uno Platform + `Plugin.Firebase.CloudMessaging` 3.1.2** 조합이 작동하는 검증된 레포. 핵심 패턴:
- Android: `MainActivity : ApplicationActivity`에서 `CrossFirebase.Initialize(this)`
- iOS: **별도 AppDelegate 없음** — `App.xaml.cs`에서 `WillFinishLaunching` 오버라이드로 `CrossFirebase.Initialize()` 호출
- `Plugin.Firebase.CloudMessaging`는 Uno의 `ApplicationActivity` / `NativeApplication` 기반에서 정상 작동

### 0.4 플랫폼 심볼

Uno Platform에서 사용하는 조건부 컴파일 심볼:
- `#if __ANDROID__` — Android 타겟
- `#if __IOS__` — iOS 타겟
- `#if ANDROID` / `#if IOS` — Uno 템플릿 기본 심볼 (Adoz 레포에서 사용)

이 문서에서는 기존 Uno 프로젝트가 `#if ANDROID` / `#if IOS`를 사용 중이므로(App.xaml.cs 64행 참조) 동일한 심볼을 사용한다.

### 0.5 MAUI -> Uno API 매핑

| MAUI API | Uno 대응 | 비고 |
|---|---|---|
| `MainThread.BeginInvokeOnMainThread` | `Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ...)` 또는 Uno의 `CoreApplication.MainView.Dispatcher` | 비동기 디스패치 |
| `Preferences.Set/Get/Remove` | `Configuration.SetValue/GetValue` (`History.Commons.Configuration`) | 파일 기반 JSON 저장소, 이미 Uno에서 사용 중 |
| `Application.Current.Dispatcher.Dispatch` | `Dispatcher.RunAsync(...)` | |
| `AppInfo.Current.VersionString` | `Package.Current.Id.Version` (Windows) / 플랫폼별 API | 4단계에서는 미구현 |
| `MauiAppCompatActivity` | `Microsoft.UI.Xaml.ApplicationActivity` (Uno) | Android |
| `MauiUIApplicationDelegate` | `App.xaml.cs`의 `WillFinishLaunching` 오버라이드 | iOS |
| `MauiApplication` | `Microsoft.UI.Xaml.NativeApplication` (Uno) | Android Application |

### 0.6 이미 Uno에 존재하는 파일 (수정 대상)

이 문서에서 수정하는 기존 파일들의 현재 내용은 각 섹션에 원문을 포함한다.

### 0.7 History.Commons에서 이미 사용 가능한 API

| 클래스 | 경로 | 용도 |
|---|---|---|
| `RegisterFirebaseToken` | `History.Commons/Api/User/RegisterFirebaseToken.cs` | FCM 토큰 서버 등록 (`POST /api/user/register-firebase-token?token=...`) |
| `RefreshToken` | `History.Commons/Api/User/RefreshToken.cs` | JWT 리프레시 (`POST /api/user/refresh-token`) |
| `GetPost` | `History.Commons/Api/Post/GetPost.cs` | 포스트 조회 |
| `GetUser` | `History.Commons/Api/User/GetUser.cs` | 유저 조회 |
| `GetNotifications` | `History.Commons/Api/User/GetNotifications.cs` | 알림 목록 조회 |
| `GetFriends` | `History.Commons/Api/Friendship/GetFriends.cs` | 친구 목록 조회 |
| `Configuration` | `History.Commons/Configuration.cs` | JSON 파일 기반 설정 저장소 (`GetValue<T>`, `SetValue`) |
| `NotificationType` | `History.Commons/Enums/NotificationType.cs` | 알림 타입 열거형 |
| `ApiHandler` | `History.Commons/` | API 요청 핸들러 |

---

## 1. NuGet 패키지 추가

### 1.1 `Directory.Packages.props` 수정

**파일**: `E:\Repos\History\History.Uno\Directory.Packages.props`

**현재 내용**:
```xml
<Project ToolsVersion="15.0">
  <ItemGroup>
    <PackageVersion Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageVersion Include="Microsoft.Maui.Controls.Compatibility" Version="$(MauiVersion)" />
  </ItemGroup>
</Project>
```

**수정 후**:
```xml
<Project ToolsVersion="15.0">
  <ItemGroup>
    <PackageVersion Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageVersion Include="Microsoft.Maui.Controls.Compatibility" Version="$(MauiVersion)" />
    <PackageVersion Include="Plugin.Firebase.CloudMessaging" Version="4.0.1" />
  </ItemGroup>
</Project>
```

### 1.2 `History.Uno.csproj` 수정

**파일**: `E:\Repos\History\History.Uno\History.Uno\History.Uno.csproj`

**현재 내용** (전체):
```xml
<Project Sdk="Uno.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-android;net10.0-ios</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UnoSingleProject>true</UnoSingleProject>
    <ApplicationTitle>History.Uno</ApplicationTitle>
    <ApplicationId>com.airtaxi.history</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationPublisher>History.Uno</ApplicationPublisher>
    <Description>History.Uno powered by Uno Platform.</Description>
    <UnoFeatures>
      Lottie;
      Svg;
      Hosting;
      Toolkit;
      Material;
      Logging;
      MauiEmbedding;
      Mvvm;
      Configuration;
      ThemeService;
      SkiaRenderer;
    </UnoFeatures>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\History.Commons\History.Commons.csproj" />
    <ProjectReference Include="..\History.Uno.MauiControls\History.Uno.MauiControls.csproj" />
  </ItemGroup>
</Project>
```

**수정 후** — `</PropertyGroup>` 뒤에 `PackageReference`와 Firebase 설정 파일 ItemGroup을 추가:
```xml
<Project Sdk="Uno.Sdk">
  <PropertyGroup>
    <!-- (기존 PropertyGroup 그대로 유지) -->
    <TargetFrameworks>net10.0-android;net10.0-ios</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UnoSingleProject>true</UnoSingleProject>
    <ApplicationTitle>History.Uno</ApplicationTitle>
    <ApplicationId>com.airtaxi.history</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationPublisher>History.Uno</ApplicationPublisher>
    <Description>History.Uno powered by Uno Platform.</Description>
    <UnoFeatures>
      Lottie;
      Svg;
      Hosting;
      Toolkit;
      Material;
      Logging;
      MauiEmbedding;
      Mvvm;
      Configuration;
      ThemeService;
      SkiaRenderer;
    </UnoFeatures>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Plugin.Firebase.CloudMessaging" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\History.Commons\History.Commons.csproj" />
    <ProjectReference Include="..\History.Uno.MauiControls\History.Uno.MauiControls.csproj" />
  </ItemGroup>

  <!-- Firebase Android configuration -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
    <GoogleServicesJson Include="google-services.json" />
  </ItemGroup>

  <!-- Firebase iOS configuration -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0-ios'">
    <BundleResource Include="GoogleService-Info.plist" />
  </ItemGroup>
</Project>
```

### 1.3 빌드 테스트

패키지 추가 후 **즉시 Android 타겟 빌드**를 실행하여 `Plugin.Firebase.CloudMessaging` 4.0.1이 .NET 10 + Uno SDK에서 호환되는지 확인한다.

```bash
dotnet build "E:/Repos/History/History.Uno/History.Uno/History.Uno.csproj" -f net10.0-android
```

> **오류 발생 시**: 해결을 시도하지 말고 즉시 사용자에게 보고하라. 특히 다음 오류들이 발생할 수 있다:
> - 패키지 버전 호환성 오류 → 3.1.2로 폴백 여부를 사용자에게 문의
> - `Xamarin.Firebase.Messaging` 전이 의존성 충돌 → 사용자에게 보고
> - `google-services.json` 미존재 오류 → 2단계에서 아직 파일을 추가하지 않았으므로 정상. 파일 추가 후 재빌드

---

## 2. Firebase 설정 파일 복사

### 2.1 `google-services.json` (Android)

**원본**: `E:\Repos\History\History.MobileClient\google-services.json`
**대상**: `E:\Repos\History\History.Uno\History.Uno\google-services.json`

원본 내용:
```json
{
  "project_info": {
    "project_number": "712187112723",
    "project_id": "history-43a21",
    "storage_bucket": "history-43a21.firebasestorage.app"
  },
  "client": [
    {
      "client_info": {
        "mobilesdk_app_id": "1:712187112723:android:97646f51d7f62a706fbee6",
        "android_client_info": {
          "package_name": "com.airtaxi.history"
        }
      },
      "oauth_client": [],
      "api_key": [
        {
          "current_key": "AIzaSyDuDXTwCeC9PSJRSoM96DDzaoRir-AnFs4"
        }
      ],
      "services": {
        "appinvite_service": {
          "other_platform_oauth_client": []
        }
      }
    }
  ],
  "configuration_version": "1"
}
```

파일을 그대로 대상 경로에 복사한다. `package_name: com.airtaxi.history`는 Uno 프로젝트의 `ApplicationId`와 일치한다.

### 2.2 `GoogleService-Info.plist` (iOS)

**원본**: `E:\Repos\History\History.MobileClient\GoogleService-Info.plist`
**대상**: `E:\Repos\History\History.Uno\History.Uno\GoogleService-Info.plist`

원본 내용:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>API_KEY</key>
	<string>AIzaSyB9ypytHxhSFCYNGJdL8xFjRmNId-jYO5E</string>
	<key>GCM_SENDER_ID</key>
	<string>712187112723</string>
	<key>PLIST_VERSION</key>
	<string>1</string>
	<key>BUNDLE_ID</key>
	<string>com.airtaxi.history</string>
	<key>PROJECT_ID</key>
	<string>history-43a21</string>
	<key>STORAGE_BUCKET</key>
	<string>history-43a21.firebasestorage.app</string>
	<key>IS_ADS_ENABLED</key>
	<false></false>
	<key>IS_ANALYTICS_ENABLED</key>
	<false></false>
	<key>IS_APPINVITE_ENABLED</key>
	<true></true>
	<key>IS_GCM_ENABLED</key>
	<true></true>
	<key>IS_SIGNIN_ENABLED</key>
	<true></true>
	<key>GOOGLE_APP_ID</key>
	<string>1:712187112723:ios:121c50a7f666ae796fbee6</string>
</dict>
</plist>
```

파일을 그대로 대상 경로에 복사한다. `BUNDLE_ID: com.airtaxi.history`는 Uno 프로젝트의 `ApplicationId`와 일치한다.

---

## 3. Android — `MainActivity.Android.cs` 확장

**파일**: `E:\Repos\History\History.Uno\History.Uno\Platforms\Android\MainActivity.Android.cs`

### 3.1 현재 내용 (전체)

```csharp
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace History.Uno.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);
    }

}
```

### 3.2 MAUI 원본 참조 (이식 소스)

MAUI `MainActivity.cs`(`E:\Repos\History\History.MobileClient\Platforms\Android\MainActivity.cs`)에서 이식할 기능:

1. **FCM 초기화**: `CrossFirebase.Initialize(this)` + `FirebaseCloudMessagingImplementation.OnNewIntent(Intent)`
2. **알림 빌더**: `FirebaseCloudMessagingImplementation.NotificationBuilderProvider`
3. **알림 채널**: `CreateNotificationChannelIfNeeded()` / `CreateNotificationChannel()`
4. **알림 권한**: `CheckNotificationPermission()` (Android 13+)
5. **알림 이벤트 구독**: `NotificationTapped` / `NotificationReceived` → `NotificationHandler` 호출
6. **JobScheduler 스케줄**: `ScheduleJob()` (TokenRefreshService 예약)

MAUI 원본의 `HandleIntent`, `SetupKeyboardDetection`, `OnActivityResult`, 공유 인텐트 처리는 **이번 단계에서 제외** (2~3단계 페이지 이전 후 구현).

### 3.3 수정 후 전체 내용

```csharp
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using History.Uno.Services;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Core.Platforms.Android;

namespace History.Uno.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    private const string TAG = "History";

    protected override void OnCreate(Bundle savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);

        // Firebase Cloud Messaging initialization
        CrossFirebase.Initialize(this);
        FirebaseCloudMessagingImplementation.OnNewIntent(Intent);

        // Notification builder provider — defines how push notifications appear in the system tray
        FirebaseCloudMessagingImplementation.NotificationBuilderProvider = notification => new NotificationCompat.Builder(this, $"{PackageName}.push")
            .SetSmallIcon(Resource.Mipmap.icon_plain)
            .SetContentTitle(notification.Title)
            .SetContentText(notification.Body)
            .SetPriority(NotificationCompat.PriorityDefault)
            .SetAutoCancel(true);

        // Notification channel and permission setup
        CreateNotificationChannelIfNeeded();
        CheckNotificationPermission();

        // Subscribe to FCM notification events (shared handler)
        CrossFirebaseCloudMessaging.Current.NotificationTapped += NotificationHandler.OnNotificationTapped;
        CrossFirebaseCloudMessaging.Current.NotificationReceived += NotificationHandler.OnNotificationReceived;

        // Schedule background JWT token refresh job
        ScheduleJob();
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        FirebaseCloudMessagingImplementation.OnNewIntent(intent);
    }

    private void CheckNotificationPermission()
    {
        if ((int)Build.VERSION.SdkInt < 33) return;

#pragma warning disable CA1416
        bool isNotificationPermissionGranted = CheckNotificationPermissionGranted();
        if (!isNotificationPermissionGranted)
        {
            AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            alert.SetTitle("안내");
            alert.SetMessage("푸시 알림을 받기 위해서는 알림 권한을 활성화해주세요");
            alert.SetButton("확인", (_, _) =>
            {
                var denied = ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.PostNotifications);
                if (denied)
                {
                    Intent intent = new Intent("android.settings.APPLICATION_DETAILS_SETTINGS");
                    var uri = global::Android.Net.Uri.FromParts("package", PackageName, null);
                    intent.SetData(uri);
                    StartActivity(intent);
                }
                else ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.PostNotifications }, 3939);
            });
            alert.Show();
        }
#pragma warning restore CA1416
    }

    private void CreateNotificationChannelIfNeeded()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            CreateNotificationChannel();
    }

    private void CreateNotificationChannel()
    {
        var channelId = $"{PackageName}.push";
        var channel = new NotificationChannel(channelId, "푸시 알림", NotificationImportance.Max);
        channel.EnableLights(true);
        channel.EnableVibration(true);
        channel.SetShowBadge(true);
        var notificationManager = (NotificationManager)GetSystemService(NotificationService);
        notificationManager.CreateNotificationChannel(channel);
        FirebaseCloudMessagingImplementation.ChannelId = channelId;
    }

    private void ScheduleJob()
    {
        try
        {
            var jobScheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            var componentName = new ComponentName(this, Java.Lang.Class.FromType(typeof(TokenRefreshService)));

            // Check if the job is already scheduled
            var allPendingJobs = jobScheduler.AllPendingJobs;
            foreach (var job in allPendingJobs)
            {
                if (job.Id == 1)
                {
                    Log.Debug(TAG, "Job is already scheduled.");
                    return;
                }
            }

            var jobInfo = new JobInfo.Builder(1, componentName)
                .SetPeriodic(Constants.TokenRefreshIntervalMilliseconds)
                .SetPersisted(true) // Persist across device reboots
                .Build();

            var result = jobScheduler.Schedule(jobInfo);
            if (result == JobScheduler.ResultSuccess)
                Log.Debug(TAG, "Job scheduled successfully.");
            else
                Log.Debug(TAG, "Job scheduling failed.");
        }
        catch (Exception exception)
        {
            Log.Error(TAG, $"Job scheduling failed: {exception.Message}\n{exception.StackTrace}");
        }
    }

#pragma warning disable CA1416
    [global::System.Runtime.Versioning.SupportedOSPlatform("android33.0")]
    private static bool CheckNotificationPermissionGranted() => ContextCompat.CheckSelfPermission(global::Android.App.Application.Context, Manifest.Permission.PostNotifications) == Permission.Granted;
#pragma warning restore CA1416
}
```

### 3.4 주의사항

- `Resource.Mipmap.icon_plain` — Uno 템플릿의 기본 아이콘 리소스명. 실제 리소스명은 `Platforms/Android/Resources/`에 있는 mipmap 폴더에 따라 다를 수 있다. 빌드 시 `Resource.Mipmap.icon_plain`이 존재하지 않으면 `Resource.Mipmap.icon` 또는 적절한 리소스로 변경해야 한다. **오류 발생 시 사용자에게 보고.**
- `NotificationHandler`는 섹션 5에서 생성한다.
- `TokenRefreshService`는 섹션 4에서 생성한다.
- `Constants.TokenRefreshIntervalMilliseconds` — `Constants`가 아직 Uno 프로젝트에 없을 수 있다. MAUI `Constants.cs`의 값은 `24 * 60 * 60 * 1000`(1일). Uno에 `Constants` 클래스가 없으면 이 파일에서 직접 상수를 사용하거나 섹션 6에서 `Constants`를 추가한다.

---

## 4. Android — 백그라운드 JWT 리프레시 서비스

### 4.1 `TokenRefreshService.Android.cs` (신규)

**파일**: `E:\Repos\History\History.Uno\History.Uno\Platforms\Android\TokenRefreshService.Android.cs`

MAUI 원본(`E:\Repos\History\History.MobileClient\Platforms\Android\TokenRefreshService.cs`)을 그대로 이식. 네임스페이스만 `History.Uno.Droid`로 변경.

```csharp
#if __ANDROID__
using Android.App;
using Android.App.Job;
using Android.Util;
using History.Commons;
using History.Commons.Api.User;

namespace History.Uno.Droid;

[Service(Name = "com.airtaxi.history.TokenRefreshService", Permission = "android.permission.BIND_JOB_SERVICE")]
public class TokenRefreshService : JobService
{
    private const string TAG = "History";

    public override bool OnStartJob(JobParameters jobParameters)
    {
        Log.Debug(TAG, "Token refresh job started.");

        Task.Run(async () =>
        {
            try
            {
                var accessToken = Configuration.GetValue<string>("AccessToken");
                var refreshToken = Configuration.GetValue<string>("RefreshToken");
                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken)) return;

                var result = await ApiHandler.Public.ExecuteRequestAsync(new RefreshToken(refreshToken));
                accessToken = result.AccessToken;
                refreshToken = result.RefreshToken;

                Shared.ApiHandler = new ApiHandler(accessToken, refreshToken);

                Configuration.SetValue("AccessToken", accessToken);
                Configuration.SetValue("RefreshToken", refreshToken);

                Log.Debug(TAG, "Token refreshed.");
            }
            catch (Exception exception)
            {
                Log.Error(TAG, $"Token refresh failed: {exception.Message}");
                return;
            }
            finally
            {
                JobFinished(jobParameters, true);
            }
        });

        return true;
    }

    public override bool OnStopJob(JobParameters jobParameters)
    {
        Log.Debug(TAG, "Token refresh job stopped.");
        return true;
    }
}
#endif
```

### 4.2 `BootCompletedReceiver.Android.cs` (신규)

**파일**: `E:\Repos\History\History.Uno\History.Uno\Platforms\Android\BootCompletedReceiver.Android.cs`

MAUI 원본(`E:\Repos\History\History.MobileClient\Platforms\Android\BootCompletedReceiver.cs`)을 그대로 이식.

```csharp
#if __ANDROID__
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Util;

namespace History.Uno.Droid;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BootCompletedReceiver : BroadcastReceiver
{
    private const string TAG = "History";

    public override void OnReceive(Context context, Intent intent)
    {
        if (intent.Action.Equals(Intent.ActionBootCompleted))
        {
            Log.Debug(TAG, "Boot completed, rescheduling job.");
            var jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(TokenRefreshService)));

            var allPendingJobs = jobScheduler.AllPendingJobs;
            foreach (var job in allPendingJobs)
            {
                if (job.Id == 1)
                {
                    Log.Debug(TAG, "Job is already scheduled.");
                    return;
                }
            }

            var jobInfo = new JobInfo.Builder(1, componentName)
                .SetPeriodic(Constants.TokenRefreshIntervalMilliseconds)
                .SetPersisted(true)
                .Build();

            jobScheduler.Schedule(jobInfo);
        }
    }
}
#endif
```

### 4.3 `Main.Android.cs` 수정 — 권한 어셈블리 속성 추가

**파일**: `E:\Repos\History\History.Uno\History.Uno\Platforms\Android\Main.Android.cs`

**현재 내용** (전체):
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml.Media;

namespace History.Uno.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }

}
```

**수정 후** — 파일 상단에 `[assembly: UsesPermission]` 추가:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml.Media;

// Permissions for JobScheduler (background token refresh)
[assembly: UsesPermission(Android.Manifest.Permission.WakeLock)]
[assembly: UsesPermission(Android.Manifest.Permission.ReceiveBootCompleted)]

// Permission for haptic feedback (vibration in notifications)
[assembly: UsesPermission(Android.Manifest.Permission.Vibrate)]

namespace History.Uno.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }

}
```

---

## 5. 공통 — `NotificationHandler.cs` (플랫폼 독립 알림 처리)

**파일**: `E:\Repos\History\History.Uno\History.Uno\Services\NotificationHandler.cs` (신규, 폴더도 생성)

이 클래스는 MAUI의 `MauiProgram.cs`에 있던 `OnNotificationTapped`, `OnNotificationReceived`, `UpdateNotificationContext`와 `App.xaml.cs`의 `HandlePushNotificationAsync`를 통합한 플랫폼 독립 핸들러다.

### 5.1 MAUI 원본 로직

**`OnNotificationTapped`** (MAUI `MauiProgram.cs` 126-136행):
- 알림 탭 시 `data`를 JSON 직렬화하여 `PushData`로 저장 (앱이 아직 로드되지 않은 경우) 또는 `HandlePushNotificationAsync` 직접 호출 (앱 로드 후)

**`OnNotificationReceived`** (MAUI `MauiProgram.cs` 138-166행):
- 알림 수신 시 `UpdateNotificationContext(data)` 호출
- `PostId`가 있으면 `GetPost` API 호출 → `ValueChangedMessage<PostResponseDto>` 메시징
- `NotificationType.FriendRequest` + `UserId`가 있으면 `GetUser` API 호출 → `ValueChangedMessage<UserResponseDto>` 메시징
- 항상 `GetNotifications` + `GetFriends` API 호출하여 데이터 동기화

**`HandlePushNotificationAsync`** (MAUI `App.xaml.cs` 277-330행):
- `NotificationType`별 페이지 이동:
  - `FriendRequest` → `UserPage(userId)`
  - `Message` → `GetMessage` API → `MessagePage`
  - `Restriction` → 제재 내역 알럿 + 디스코드 소명 안내
  - `InviteCodeRequest` → `InviteCodeRequestsPage`
  - `InviteCodeRequestResult` → `InviteCodesPage`
  - 그 외 (Comment, PostMention 등) → `GetPost` API → `PostPage`

### 5.2 구현 내용

```csharp
using System.Text.Json;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.Uno.DataTypes;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace History.Uno.Services;

public static class NotificationHandler
{
    /// <summary>
    /// Called when the user taps a push notification.
    /// If the app shell is not yet loaded, stores the push data for later processing.
    /// If the app is loaded, immediately handles the notification (navigates to the relevant page).
    /// </summary>
    public static void OnNotificationTapped(object sender, Plugin.Firebase.CloudMessaging.EventArgs.FCMNotificationTappedEventArgs e)
    {
        var data = e.Notification.Data;
        var pushData = JsonSerializer.Serialize(data);

        // Check if the app is loaded by verifying the root frame has content.
        // Unlike MAUI's AppShell.IsLoaded, Uno uses the root frame's current page.
        if (App.RootFrame?.Content == null)
        {
            // App not loaded yet — store push data for later processing after login
            Configuration.SetValue("PushData", pushData);
        }
        else
        {
            // App is loaded — handle immediately on the UI thread
            _ = HandlePushNotificationAsync(pushData);
        }
    }

    /// <summary>
    /// Called when a push notification is received while the app is in the foreground.
    /// Updates the in-app data context (posts, users, notifications, friends) without navigating.
    /// </summary>
    public static void OnNotificationReceived(object sender, Plugin.Firebase.CloudMessaging.EventArgs.FCMNotificationReceivedEventArgs e)
        => _ = UpdateNotificationContextAsync(e.Notification.Data);

    /// <summary>
    /// Updates in-app data based on notification payload.
    /// Fetches the relevant post/user, then refreshes notifications and friends lists.
    /// </summary>
    public static async Task UpdateNotificationContextAsync(IDictionary<string, string> data)
    {
        if (data == null) return;
        if (!data.TryGetValue("Type", out var rawType) || !Enum.TryParse<NotificationType>(rawType, out var type)) return;
        if (Shared.ApiHandler == null) return;

        try
        {
            // Fetch the relevant entity based on notification type
            if (data.TryGetValue("PostId", out var postId))
            {
                var post = await Shared.ApiHandler.ExecuteRequestAsync(new GetPost(postId));
                _ = App.MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post)));
            }
            else if (type == NotificationType.FriendRequest && data.TryGetValue("UserId", out var userId))
            {
                var user = await Shared.ApiHandler.ExecuteRequestAsync(new GetUser(userId));
                _ = App.MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    WeakReferenceMessenger.Default.Send(new ValueChangedMessage<UserResponseDto>(user)));
            }

            // Always refresh notifications and friends lists
            var notifications = await Shared.ApiHandler.ExecuteRequestAsync(new GetNotifications());
            _ = App.MainWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                WeakReferenceMessenger.Default.Send(new NotificationsMessage(notifications)));

            var friends = await Shared.ApiHandler.ExecuteRequestAsync(new GetFriends(Shared.UserId));
            Shared.Friends = friends;
        }
        catch { }
    }

    /// <summary>
    /// Handles a push notification tap by navigating to the relevant page.
    /// Called after the app has loaded (from LoginPage or directly if already loaded).
    /// </summary>
    public static async Task HandlePushNotificationAsync(string pushData)
    {
        // Clear the stored push data
        Configuration.SetValue("PushData", null);

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(pushData);
        if (data == null) return;
        if (!data.TryGetValue("Type", out var rawType)) return;
        if (!Enum.TryParse<NotificationType>(rawType, out var type)) return;

        // TODO: 2-3단계 페이지 이전 후 각 케이스별 페이지 이동 활성화
        // 현재는 데이터 동기화만 수행하고 페이지 이동은 스킵한다.
        // 페이지가 구현되면 아래 switch 문에서 해당 페이지로 이동한다.

        switch (type)
        {
            case NotificationType.FriendRequest:
                // TODO: await App.PushAsync(typeof(UserPage), userId);
                // data.TryGetValue("UserId", out var userId);
                break;

            case NotificationType.Message:
                // TODO: var messageResult = await App.ExecuteRequestAsync(new GetMessage(messageId));
                // TODO: await App.PushAsync(typeof(MessagePage), messageViewModel);
                break;

            case NotificationType.Restriction:
                // TODO: await App.DisplayAlertAsync("제재 내역", data["Body"], Constants.PromptOk, "소명 신청하기");
                break;

            case NotificationType.InviteCodeRequest:
                // TODO: await App.PushAsync(typeof(InviteCodeRequestsPage));
                break;

            case NotificationType.InviteCodeRequestResult:
                // TODO: await App.PushAsync(typeof(InviteCodesPage));
                break;

            default:
                // Comment, CommentMention, CommentLike, Share, Repost, PostReaction, PostMention, FavoriteFriendNewPost, Birthday, Report
                // TODO: var postResult = await App.ExecuteRequestAsync(new GetPost(postId));
                // TODO: await App.PushAsync(typeof(PostPage), postViewModel);
                break;
        }

        // Perform data synchronization regardless
        await UpdateNotificationContextAsync(data);
    }
}
```

### 5.3 주의사항

- `App.MainWindow.Dispatcher.RunAsync` — Uno에서 UI 스레드 디스패치. `App.MainWindow`는 `App.xaml.cs`의 static 속성(이미 존재). **빌드 시 `Windows.UI.Core.CoreDispatcherPriority`를 찾을 수 없다는 오류가 발생하면**, `Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(...)`로 대체한다. 단, **오류 발생 시 사용자에게 보고 후 시도**한다.
- `Plugin.Firebase.CloudMessaging.EventArgs.FCMNotificationTappedEventArgs` — 네임스페이스 경로가 버전에 따라 다를 수 있다. 4.0.1에서 확인 필요. **빌드 오류 시 사용자에게 보고.**
- `Configuration.SetValue("PushData", null)` — `Configuration`의 `SetValue`는 `null`을 허용하지 않을 수 있다. MAUI의 `Preferences.Remove`에 해당. 필요시 `Configuration.SetValue("PushData", "")`로 빈 문자열을 저장하거나, `Configuration`에 `RemoveKey` 메서드가 있는지 확인. **오류 시 사용자에게 보고.**
- `GetMessage` API는 `History.Commons.Api.Message` 네임스페이스에 있다. 이식 시 `using History.Commons.Api.Message;` 추가 필요 (TODO 활성화 시).

---

## 6. `Constants.cs` 추가

**파일**: `E:\Repos\History\History.Uno\History.Uno\Constants.cs` (신규)

MAUI 원본(`E:\Repos\History\History.MobileClient\Constants.cs`)에서 FCM/JWT 리프레시에 필요한 상수만 이식.

```csharp
namespace History.Uno;

public static class Constants
{
    public const int TokenRefreshIntervalMilliseconds = 24 * 60 * 60 * 1000; // 1 Day

    public const string DefaultProfileImageFileName = "default_profile_image.jpg";
    public const string DefaultBackgroundImageFileName = "icon.png";
    public const string PromptOk = "확인";
    public const string PromptCancel = "취소";
    public const string PromptYes = "예";
    public const string PromptNo = "아니요";

    public const string DiscordInviteUrl = "https://discord.com/invite/g9jk3GR3vD";
}
```

> `GoogleAuthWebClientId`, `GoogleAuthAppleClientId`, `GoogleAuthRequestCode`, `KakaoStoryCredentialEncryptionKey`는 4단계 잔여 항목(OAuth, 카카오스토리) 이식 시 추가한다.

---

## 7. `Utils.cs` — `RefreshFirebaseToken` 추가

**파일**: `E:\Repos\History\History.Uno\History.Uno\Utils.cs`

### 7.1 현재 내용

파일 끝(130행)은:
```csharp
    [GeneratedRegex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled)]
    public static partial Regex UrlRegex();
}
```

### 7.2 수정 내용

클래스 닫는 중괄호 `}` 앞에 `RefreshFirebaseToken` 메서드를 추가한다. 파일 상단에 `using`도 추가 필요.

**파일 상단에 추가할 using**:
```csharp
using Plugin.Firebase.CloudMessaging;
```

**클래스 내 끝에 추가할 메서드** (`UrlRegex()` 뒤, 닫는 `}` 앞):

```csharp
    public static async Task RefreshFirebaseToken()
    {
        await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        var firebaseToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        Console.WriteLine($"FCM token: {firebaseToken}");

        if (Shared.ApiHandler == null)
        {
            var accessToken = Configuration.GetValue<string>("AccessToken");
            var refreshToken = Configuration.GetValue<string>("RefreshToken");

            if (accessToken != null && refreshToken != null) Shared.ApiHandler = new(accessToken, refreshToken);
            else return;
        }

        try { await Shared.ApiHandler.ExecuteRequestAsync(new RegisterFirebaseToken(firebaseToken)); }
        catch { }
    }
```

> `RegisterFirebaseToken`은 `History.Commons.Api.PushNotification` 네임스페이스에 있다. `GlobalUsings.cs`에 `global using History.Commons;`가 있으므로 네임스페이스가 자동으로 참조되지 않는다면 `using History.Commons.Api.PushNotification;`을 추가한다. **빌드 오류 시 사용자에게 보고.**

---

## 8. iOS — `App.xaml.cs`에 `WillFinishLaunching` 추가

**파일**: `E:\Repos\History\History.Uno\History.Uno\App.xaml.cs`

### 8.1 현재 내용 (전체)

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.UI.Xaml.Controls;
using Uno.Resizetizer;

namespace History.Uno;

public partial class App : Application
{
    private static readonly SemaphoreSlim ApiRequestSemaphore = new(1, 1);
    private static readonly SemaphoreSlim NavigationSemaphore = new(1, 1);

    public static Window MainWindow { get; private set; }
    public static Frame RootFrame { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected IHost Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
#if MAUI_EMBEDDING
            .UseMauiEmbedding<MauiControls.App>(maui => maui
                .UseMauiControls())
#endif
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .ConfigureServices((context, services) =>
                {
                })
            );

        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();

        // Set up ApiHandler metadata
#if ANDROID
        ApiHandler.Platform = "Android";
#elif IOS
        ApiHandler.Platform = "iOS";
#endif
        ApiHandler.ApplicationVersion = "1.0.0";

        // Load tokens from Configuration and initialize ApiHandler
        var accessToken = Configuration.GetValue<string>("AccessToken");
        var refreshToken = Configuration.GetValue<string>("RefreshToken");
        if (accessToken != null && refreshToken != null) Shared.ApiHandler = new ApiHandler(accessToken, refreshToken);

        // Set up root frame
        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
        }

        RootFrame = rootFrame;

        if (rootFrame.Content == null) rootFrame.Navigate(typeof(MainPage), args.Arguments);

        MainWindow.Activate();
    }

    // --- Navigation ---

    public static Page Page => RootFrame?.Content as Page;
    public static Page TopPage => Page;

    public static async Task PushAsync(Type pageType, object parameter = null)
    {
        if (NavigationSemaphore.CurrentCount == 0) return;

        await NavigationSemaphore.WaitAsync();
        try { RootFrame?.Navigate(pageType, parameter); }
        finally { if (NavigationSemaphore.CurrentCount == 0) NavigationSemaphore.Release(); }
    }

    public static async Task PopAsync()
    {
        if (NavigationSemaphore.CurrentCount == 0) return;

        await NavigationSemaphore.WaitAsync();
        try { if (RootFrame?.CanGoBack == true) RootFrame.GoBack(); }
        finally { if (NavigationSemaphore.CurrentCount == 0) NavigationSemaphore.Release(); }
    }

    public static async Task PushModalAsync(Type pageType, object parameter = null) => await PushAsync(pageType, parameter);

    public static async Task PopModalAsync() => await PopAsync();

    // --- API Request Execution ---

    public static async Task<Result> ExecuteRequestAsync(IBaseRequest request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            await ApiRequestSemaphore.WaitAsync();
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(true));

            await Shared.ApiHandler.ExecuteRequestAsync(request);
            return Result.Success();
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await DisplayAlertAsync("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
            return (errorType, exception.Message);
        }
        finally
        {
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(false));
            ApiRequestSemaphore.Release();
        }
    }

    public static async Task<Result<T>> ExecuteRequestAsync<T>(IBaseRequest<T> request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            await ApiRequestSemaphore.WaitAsync();
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(true));

            return await Shared.ApiHandler.ExecuteRequestAsync(request);
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await DisplayAlertAsync("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
            return (errorType, exception.Message);
        }
        finally
        {
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(false));
            ApiRequestSemaphore.Release();
        }
    }

    private static ErrorType StatusCodeToErrorType(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Forbidden => ErrorType.Forbidden,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        HttpStatusCode.BadRequest => ErrorType.BadRequest,
        HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
        _ => ErrorType.ProgramError,
    };

    // --- Alert Helpers (ContentDialog-based) ---

    public static async Task DisplayAlertAsync(string title, string message, string ok = "확인")
    {
        var page = TopPage;
        if (page == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = ok,
            XamlRoot = page.XamlRoot
        };
        await dialog.ShowAsync();
    }

    public static async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        var page = TopPage;
        if (page == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = accept,
            SecondaryButtonText = cancel,
            XamlRoot = page.XamlRoot
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
```

### 8.2 수정 내용

두 곳을 수정한다:

#### 8.2.1 파일 상단 using 추가

현재:
```csharp
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.UI.Xaml.Controls;
using Uno.Resizetizer;
```

수정 후:
```csharp
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.UI.Xaml.Controls;
using Uno.Resizetizer;
using History.Uno.Services;
#if IOS
using Foundation;
using UIKit;
using Plugin.Firebase.Core.Platforms.iOS;
using Plugin.Firebase.CloudMessaging;
#endif
```

#### 8.2.2 `OnLaunched` 메서드 뒤에 `WillFinishLaunching` 오버라이드 추가

`OnLaunched` 메서드의 닫는 중괄호 `}` 뒤, `// --- Navigation ---` 주석 앞에 추가:

```csharp
#if IOS
    // iOS Firebase initialization — called by Uno Platform when the UIApplicationDelegate launches.
    // Uno surfaces UIApplicationDelegate callbacks as overrides on the shared App class.
    // No separate AppDelegate.cs file is needed.
    public override bool WillFinishLaunching(UIApplication application, NSDictionary launchOptions)
    {
        CrossFirebase.Initialize();
        Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation.Initialize();

        // Subscribe to FCM notification events (shared handler)
        CrossFirebaseCloudMessaging.Current.NotificationTapped += NotificationHandler.OnNotificationTapped;
        CrossFirebaseCloudMessaging.Current.NotificationReceived += NotificationHandler.OnNotificationReceived;

        return false;
    }
#endif
```

> **주의**: `WillFinishLaunching`은 `Microsoft.UI.Xaml.Application`의 virtual 메서드로 Uno iOS에서 노출된다. Adoz 레포에서 검증된 패턴이다. 만약 빌드 시 이 메서드를 오버라이드할 수 없다는 오류가 발생하면, `FinishedLaunching`을 대신 사용하거나 별도의 `AppDelegate` 클래스 작성이 필요할 수 있다. **오류 발생 시 사용자에게 보고.**

---

## 9. `AndroidManifest.xml` 수정

**파일**: `E:\Repos\History\History.Uno\History.Uno\Platforms\Android\AndroidManifest.xml`

### 9.1 현재 내용 (전체)

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  <application android:allowBackup="true" android:supportsRtl="true"></application>
</manifest>
```

### 9.2 수정 후 (전체)

MAUI 원본(`E:\Repos\History\History.MobileClient\Platforms\Android\AndroidManifest.xml`)과 Adoz 원본을 참조하여 병합:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  <application android:allowBackup="true" android:supportsRtl="true">

    <!-- Firebase Cloud Messaging — token receiver -->
    <receiver
      android:name="com.google.firebase.iid.FirebaseInstanceIdInternalReceiver"
      android:exported="false" />
    <receiver
      android:name="com.google.firebase.iid.FirebaseInstanceIdReceiver"
      android:exported="true"
      android:permission="com.google.android.c2dm.permission.SEND">
      <intent-filter>
        <action android:name="com.google.android.c2dm.intent.RECEIVE" />
        <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
        <category android:name="${applicationId}" />
      </intent-filter>
    </receiver>

    <!-- Default notification icon for FCM -->
    <meta-data
      android:name="com.google.firebase.messaging.default_notification_icon"
      android:resource="@mipmap/icon_plain" />

  </application>

  <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
  <uses-permission android:name="android.permission.INTERNET" />
  <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
</manifest>
```

> **주의**: `@mipmap/icon_plain` 리소스가 `Platforms/Android/Resources/`에 존재해야 한다. 현재 Uno 프로젝트에는 mipmap 폴더가 없을 수 있다. 빌드 오류 발생 시 `@mipmap/icon`으로 변경하거나, 알림용 아이콘 리소스를 추가해야 한다. **오류 발생 시 사용자에게 보고.**
>
> `WakeLock`, `ReceiveBootCompleted`, `Vibrate` 권한은 `Main.Android.cs`의 `[assembly: UsesPermission]`으로 선언되므로 매니페스트에 중복해서 넣지 않는다. 빌드 시 자동으로 병합된다.

---

## 10. `Info.plist` 수정 (iOS)

**파일**: `E:\Repos\History\History.Uno\History.Uno\Platforms\iOS\Info.plist`

### 10.1 현재 내용 (전체)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>LSRequiresIPhoneOS</key>
	<true/>
	<key>UIDeviceFamily</key>
	<array>
		<integer>1</integer>
		<integer>2</integer>
	</array>
	<key>UIRequiredDeviceCapabilities</key>
	<array>
		<string>armv7</string>
		<string>arm64</string>
	</array>
	<key>UISupportedInterfaceOrientations</key>
	<array>
		<string>UIInterfaceOrientationPortrait</string>
		<string>UIInterfaceOrientationLandscapeLeft</string>
		<string>UIInterfaceOrientationLandscapeRight</string>
	</array>
	<key>UISupportedInterfaceOrientations~ipad</key>
	<array>
		<string>UIInterfaceOrientationPortrait</string>
		<string>UIInterfaceOrientationPortraitUpsideDown</string>
		<string>UIInterfaceOrientationLandscapeLeft</string>
		<string>UIInterfaceOrientationLandscapeRight</string>
	</array>
	<key>UIViewControllerBasedStatusBarAppearance</key>
	<false/>
	<key>XSAppIconAssets</key>
	<string>Assets.xcassets/icon.appiconset</string>
	<key>UIApplicationSupportsIndirectInputEvents</key>
	<true/>

	<!--
	Adjust this to your application's encryption usage.
	<key>ITSAppUsesNonExemptEncryption</key>
	<false/>
	-->
</dict>
</plist>
```

### 10.2 수정 내용

`UIApplicationSupportsIndirectInputEvents` 뒤에 `UIBackgroundModes`를 추가한다:

```xml
	<key>UIBackgroundModes</key>
	<array>
		<string>remote-notification</string>
	</array>
```

수정 후 해당 영역:
```xml
	<key>UIApplicationSupportsIndirectInputEvents</key>
	<true/>

	<key>UIBackgroundModes</key>
	<array>
		<string>remote-notification</string>
	</array>

	<!--
	Adjust this to your application's encryption usage.
	<key>ITSAppUsesNonExemptEncryption</key>
	<false/>
	-->
</dict>
</plist>
```

> `remote-notification` 백그라운드 모드는 FCM이 백그라운드에서 알림을 수신할 수 있도록 iOS에 등록하는 필수 항목이다.

---

## 11. `Entitlements.plist` 수정 (iOS)

**파일**: `E:\Repos\History\History.Uno\History.Uno\Platforms\iOS\Entitlements.plist`

### 11.1 현재 내용 (전체)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
</dict>
</plist>
```

### 11.2 수정 후 (전체)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>aps-environment</key>
	<string>production</string>
</dict>
</plist>
```

> `aps-environment`는 Apple Push Notification Service(APNS) 환경을 지정한다. 개발 중에는 `development`를 사용하고, App Store 배포 시 `production`으로 변경한다. 현재는 `production`으로 설정. Adoz 레포에서는 이 항목이 누락되어 있었으나, 정상적인 FCM 작동을 위해 필요하다.
>
> **참고**: `com.apple.developer.applesignin`과 `keychain-access-groups`는 Apple Sign-In 이식 시(4단계 잔여) 추가한다.

---

## 12. `GlobalUsings.cs` 확인

**파일**: `E:\Repos\History\History.Uno\History.Uno\GlobalUsings.cs`

현재 내용:
```csharp
global using System.Collections.Immutable;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using History.Uno.Models;
#if MAUI_EMBEDDING
global using History.Uno.MauiControls;
#endif
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using CommunityToolkit.Mvvm.Messaging;
global using CommunityToolkit.Mvvm.Messaging.Messages;
global using History.Commons;
global using History.Commons.Enums;
global using History.Commons.DataTypes.ResponseDtos;
global using History.Commons.DataTypes.Contents;
global using History.Commons.Interfaces;
global using History.Uno.Enums;
global using History.Uno.DataTypes;
```

이 파일에 **추가가 필요할 수 있는 using**:
- `global using History.Uno.Services;` — `NotificationHandler`를 전역에서 참조하기 위해. 단, `App.xaml.cs`에서만 사용되므로 파일별 using으로도 충분하다.

수정이 필요한지는 빌드 결과에 따라 결정한다. **빌드 오류 시 사용자에게 보고 후 결정.**

---

## 13. `app.manifest` 확인

**파일**: `E:\Repos\History\History.Uno\History.Uno\app.manifest`

이 파일은 Windows 타겟 전용이며, iOS/Android 타겟에는 영향을 주지 않는다. 수정 불필요.

---

## 14. 빌드 및 검증

### 14.1 Android 빌드

```bash
dotnet build "E:/Repos/History/History.Uno/History.Uno/History.Uno.csproj" -f net10.0-android
```

### 14.2 iOS 빌드

```bash
dotnet build "E:/Repos/History/History.Uno/History.Uno/History.Uno.csproj" -f net10.0-ios
```

> iOS 빌드는 macOS 환경에서만 완전히 성공한다. Windows에서는 바인딩 컴파일까지는 가능하지만 링커 단계에서 실패할 수 있다. Windows에서 iOS 빌드 오류는 무시해도 괜찮다(AGENTS.md에 명시됨: "Windows 환경에서 iOS 빌드 오류는 무시해도 괜찮습니다").

### 14.3 빌드 오류 처리 규칙

> **절대 규칙**: 빌드 오류가 발생하면 **해결을 시도하지 말고 즉시 중단 후 사용자에게 해결을 요청**하라.
>
> 사용자에게 전달할 정보:
> 1. 오류 메시지 전문 (컴파일러/링커 출력)
> 2. 오류가 발생한 파일 경로와 줄 번호
> 3. 어떤 섹션의 작업 중 발생했는지
> 4. 예상되는 원인 (선택사항)
>
> 자의적으로 코드를 수정하여 오류를 우회하지 마라. 특히 다음 유형의 오류는 사용자 결정이 필요하다:
> - 패키지 버전 호환성 오류 → 다른 버전 시도 여부
> - 리소스 미존재 (`Resource.Mipmap.icon_plain`) → 리소스 추가 또는 대체 이름 사용
> - API 네임스페이스 불일치 (`Plugin.Firebase.CloudMessaging.EventArgs` 경로) → 정확한 네임스페이스 확인
> - Uno 메서드 오버라이드 불가 (`WillFinishLaunching`) → 대체 패턴 필요
> - `Dispatcher.RunAsync` API 불일치 → 올바른 Uno 디스패치 API 확인

---

## 15. `UnoMigration.md` 업데이트

모든 구현이 완료되면 `E:\Repos\History\UnoMigration.md`의 4단계 체크리스트를 업데이트한다.

### 15.1 완료 항목 체크

다음 항목을 `[x]`로 변경:
- `[x]` **Firebase Cloud Messaging**: Android `BootCompletedReceiver`, `TokenRefreshService`, `MainActivity` FCM 초기화 → Uno `Platforms/Android` MainActivity 재구현; iOS `AppDelegate` FCM 초기화 → Uno `App.xaml.cs` `WillFinishLaunching` 재구현
- `[x]` **AndroidManifest.xml**, **Info.plist** 마이그레이션 (FCM 관련 부분)
- `[x]` **Firebase 설정**: `google-services.json` (Android), `GoogleService-Info.plist` (iOS) 연동
- `[x]` `Entitlements.plist` `aps-environment` 추가

### 15.2 남은 항목 별도 섹션 추가

4단계 체크리스트 아래에 다음 섹션을 추가:

```markdown
### 4단계 잔여 (2~3단계 페이지 이전과 연계하여 진행)

인프라 이식(FCM, JWT 리프레시, 매니페스트)은 완료. 다음 항목들은 해당 페이지/기능이 Uno로 이전된 후 구현한다.

- [ ] **Google OAuth**: `Auth/GoogleAuthService` Android/iOS 분할 구현을 Uno 플랫폼별로 이전 (`Platforms/Android`, `Platforms/iOS`). 2단계 `LoginPage` 이전 시 활성화.
- [ ] **Apple OAuth**: `AppleLoginPage` + `Platforms/iOS` Apple Sign-In. 2단계 `LoginPage`/`AppleLoginPage` 이전 시 활성화.
- [ ] **카카오스토리**: `KakaoStory/` 3파일(`KakaoStoryApiHandler.cs`, `KakaoStoryUtils.cs`, `DataTypes.cs`), `KakaoStoryLoginPage`, `KakaoStoryRewritePage` 이전. 플랫폼 독립 HTTP 코드라 직접 복사 가능. RestSharp, Newtonsoft.Json 의존성 추가 필요. 3단계 페이지 이전 시.
- [ ] **미디어 피커**: `AndroidMediaPickerHelper` → Uno 플랫폼별 Media Picker; `Xamarin.MediaGallery` 대체. iOS는 `PHPickerViewController` 네이티브 구현 필요. 3단계 `EditPostPage`/`EditCommentPage` 이전 시.
- [ ] **키보드 감지**: `KeyboardSizeMessage` (Android `WindowInsetsListener`, iOS `UIKeyboard` 알림) → Uno 플랫폼별 재구현. `KeyboardSizeMessage`는 이미 `History.Uno/DataTypes/`에 이식됨. 3단계 `EditPostPage`/`PostPage` 이전 시.
- [ ] **공유 인텐트 (Android)**: `MainActivity.HandleIntent` (ActionSend/ActionSendMultiple) → Uno `Platforms/Android` MainActivity. 3단계 `EditPostPage` 이전 후.
- [ ] **iOS 커스텀 렌더러**: `CustomShellRenderer`, `CustomTabBarAppearanceTracker`, `StaggeredItemsViewLayout` → **불필요** (Uno Toolkit `TabBar` + `ItemsRepeater` 사용). 마이그레이션 계획에서 제외.
- [ ] **백 버튼 처리**: `AppShell.OnBackButtonPressed` (Android 더블탭 종료) → Uno `Platforms/Android` BackButton 처리. 2단계 `AppShell` → `TabBar` 이전 시.
- [ ] **앱 아이콘/스플래시**: `Resources/AppIcon`, `Resources/Splash` → Uno `Assets`, `app.manifest`, `Package.appxmanifest`. 별도 작업.
- [ ] **폰트**: `Resources/Fonts` (OpenSans, MaterialSymbols, FontAwesome) → Uno `Assets/Fonts`. 별도 작업.
- [ ] **서명**: Android `signing.keystore`, iOS 인증서/프로비저닝 설정. 배포 단계.
- [ ] **`Utils.GetGlobalAppTheme`**: MAUI `Application.Current.UserAppTheme` → Uno `ThemeService` (`UnoFeature: ThemeService` 이미 활성화). 2단계 이후.
- [ ] **`Utils.CheckForUpdateAsync`**: `AppInfo.Current.VersionString` → Uno 플랫폼별 버전 API. 2단계 이후.
```

---

## 16. 구현 순서 요약

다음 순서대로 구현한다. 각 단계 후 빌드를 시도하지 않고, 모든 파일 수정을 완료한 후 한 번에 빌드한다.

| 순서 | 섹션 | 파일 | 작업 |
|---|---|---|---|
| 1 | §1.1 | `Directory.Packages.props` | 패키지 버전 추가 |
| 2 | §1.2 | `History.Uno.csproj` | PackageReference + Firebase 설정 파일 Item 추가 |
| 3 | §2.1 | `google-services.json` | MAUI에서 복사 |
| 4 | §2.2 | `GoogleService-Info.plist` | MAUI에서 복사 |
| 5 | §6 | `Constants.cs` | 신규 생성 |
| 6 | §5 | `Services/NotificationHandler.cs` | 신규 생성 |
| 7 | §7 | `Utils.cs` | `RefreshFirebaseToken` 추가 |
| 8 | §3 | `MainActivity.Android.cs` | FCM 초기화 + 알림 채널/권한 + 이벤트 구독 + JobScheduler |
| 9 | §4.1 | `TokenRefreshService.Android.cs` | 신규 생성 |
| 10 | §4.2 | `BootCompletedReceiver.Android.cs` | 신규 생성 |
| 11 | §4.3 | `Main.Android.cs` | 권한 어셈블리 속성 추가 |
| 12 | §9 | `AndroidManifest.xml` | FCM receiver + 권한 + 알림 아이콘 메타데이터 |
| 13 | §8 | `App.xaml.cs` | using 추가 + `WillFinishLaunching` 오버라이드 |
| 14 | §10 | `Info.plist` | `UIBackgroundModes` 추가 |
| 15 | §11 | `Entitlements.plist` | `aps-environment` 추가 |
| 16 | §14 | — | Android 빌드 실행 |
| 17 | §15 | `UnoMigration.md` | 체크리스트 업데이트 |

> **빌드 오류 시**: 즉시 중단하고 사용자에게 보고. 오류 해결을 시도하지 마라.