using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Message;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.Commons.Interfaces;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Plugin.Firebase.CloudMessaging;
using ShimSkiaSharp;
using Syncfusion.Maui.Toolkit.Localization;
using Syncfusion.Maui.Toolkit.Picker;

namespace History.MobileClient;

public partial class App : Application
{
    private static readonly SemaphoreSlim ActionRequestSemaphore = new(1, 1);
    private static readonly SemaphoreSlim NavigationSemaphore = new(1, 1);

    public static Window MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();

#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, e) =>
        {
            e.Handled = true;
            var exception = e.Exception;
            if (exception != null)
            {
                Android.Util.Log.Error("History", $"History Unhandled Exception: {exception.Message}\n{exception.StackTrace}");
                if (exception.Message.Contains("CarouselView"))
                {
                    Dispatcher.Dispatch(async () =>
                    {
                        var result = await Page.DisplayAlertAsync("오류", $"MAUI 코드베이스에서 버그가 발견되었습니다. 애플리케이션을 재시작해주세요.", "확인", "자세히 알아보기");
                        if (result) return;

                        await Page.DisplayAlertAsync("자세히 알아보기", "이 오류는 MAUI의 CarouselView에서 발생하는 버그로, History 애플리케이션과는 관련이 없습니다.", "확인");
                    });
                }
                else if (!exception.Message.Contains("FFImageLoading.Maui.Platform.DroidImageView") && !exception.StackTrace.Contains("FFImageLoading.Maui.Platform.DroidImageView"))
                    Dispatcher.Dispatch(() => Page.DisplayAlertAsync("오류", $"{exception.Message}\n{exception.StackTrace}", Constants.PromptOk));

                Debugger.BreakForUserUnhandledException(exception);
            }
        };
#endif
        UpdateSyncFusionTheme();

        RequestedThemeChanged += (_, __) => UpdateSyncFusionTheme();

        var theme = Configuration.GetValue<string>("Theme");
        if (theme == "Light") UserAppTheme = AppTheme.Light;
        else if (theme == "Dark") UserAppTheme = AppTheme.Dark;
        else UserAppTheme = AppTheme.Unspecified;

        KakaoStoryApiHandler.OnReloginRequired = KakaoStoryUtils.ReLoginAsync;
    }

    public static Page Page
    {
        get => Current.Windows[0].Page;
        set => Current.Windows[0].Page = value;
    }

#if IOS
    public static Page TopPage => Navigation.ModalStack.Count > 0 ? Navigation.ModalStack[Navigation.ModalStack.Count - 1] : Page;
#else
    public static Page TopPage => Navigation.ModalStack.Count > 0 ? Navigation.NavigationStack[Navigation.NavigationStack.Count - 1] : Page;
#endif

    public static INavigation Navigation => Current.Windows[0].Page.Navigation;

    public static async Task PushAsync(Page page)
    {
        if (NavigationSemaphore.CurrentCount == 0) return;

        await NavigationSemaphore.WaitAsync();

        Page duplicatePage = null;
        if (page is PostPage postPage)
        {
#if IOS
            var navigationStack = Navigation.ModalStack;
#else
            var navigationStack = Navigation.NavigationStack;
#endif
            duplicatePage = navigationStack.FirstOrDefault(p => p is PostPage existingPostPage && existingPostPage.PostId == postPage.PostId);
        }
        else if (page is UserPage userPage)
        {
#if IOS
            var navigationStack = Navigation.ModalStack;
#else
            var navigationStack = Navigation.NavigationStack;
#endif
            duplicatePage = navigationStack.FirstOrDefault(p => p is UserPage existingUserPage && existingUserPage.UserId == userPage.UserId);
        }

#if IOS
        try { await Current.Windows[0].Page.Navigation.PushModalAsync(page); }
#else
        try { await Current.Windows[0].Page.Navigation.PushAsync(page); }
#endif
        catch (Exception)
        {
            if (NavigationSemaphore.CurrentCount == 0) NavigationSemaphore.Release();
#if !IOS
            await PushModalAsync(page);
#endif
        }
        finally
        {
            if (duplicatePage != null) Navigation.RemovePage(duplicatePage);

            if (NavigationSemaphore.CurrentCount == 0)
            {
                NavigationSemaphore.Release();
            }
        }
    }

    public static async Task PopAsync()
    {
        if (NavigationSemaphore.CurrentCount == 0) return;

        await NavigationSemaphore.WaitAsync();
#if IOS
        try { await Current.Windows[0].Page.Navigation.PopModalAsync(); }
#else
        try { await Current.Windows[0].Page.Navigation.PopAsync(); }
#endif
        finally
        {
            if (NavigationSemaphore.CurrentCount == 0)
            {
                NavigationSemaphore.Release();
            }
        }
    }

    public static async Task PushModalAsync(Page page)
    {
        if (NavigationSemaphore.CurrentCount == 0) return;

        await NavigationSemaphore.WaitAsync();
        try { await Current.Windows[0].Page.Navigation.PushModalAsync(page); }
        finally
        {
            if(NavigationSemaphore.CurrentCount == 0)
            {
                NavigationSemaphore.Release();
            }
        }
    }

    public static async Task PopModalAsync()
    {
        if (NavigationSemaphore.CurrentCount == 0) return;

        await NavigationSemaphore.WaitAsync();
        try { await Current.Windows[0].Page.Navigation.PopModalAsync(); }
        finally
        {
            if (NavigationSemaphore.CurrentCount == 0)
            {
                NavigationSemaphore.Release();
            }
        }
    }

    public static async Task<Result> ExecuteRequestAsync(IBaseRequest request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            await ExecuteWithLoadingAsync(() => Shared.ApiHandler.ExecuteRequestAsync(request));
            return Result.Success();
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await TopPage.DisplayAlertAsync("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
            return (errorType, exception.Message);
        }
    }

    public static async Task<Result<T>> ExecuteRequestAsync<T>(IBaseRequest<T> request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            return await ExecuteWithLoadingAsync(() => Shared.ApiHandler.ExecuteRequestAsync(request));
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await TopPage.DisplayAlertAsync("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
            return (errorType, exception.Message);
        }
    }

    public static async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        try
        {
            await ActionRequestSemaphore.WaitAsync();
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(true));

            await action();
        }
        finally
        {
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(false));
            ActionRequestSemaphore.Release();
        }
    }

    public static async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action)
    {
        try
        {
            await ActionRequestSemaphore.WaitAsync();
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(true));

            return await action();
        }
        finally
        {
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(false));
            ActionRequestSemaphore.Release();
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

    protected override Window CreateWindow(IActivationState activationState)
    {
        MainWindow ??= new Window(new LoginPage());
        return MainWindow;
    }

    private static void UpdateSyncFusionTheme()
    {
        ICollection<ResourceDictionary> mergedDictionaries = Current.Resources.MergedDictionaries;
        if (mergedDictionaries != null)
        {
            var toolkitTheme = mergedDictionaries.OfType<Syncfusion.Maui.Toolkit.Themes.SyncfusionThemeResourceDictionary>().FirstOrDefault();
            var coreTheme = mergedDictionaries.OfType<Syncfusion.Maui.Themes.SyncfusionThemeResourceDictionary>().FirstOrDefault();
            if (toolkitTheme != null)
            {
                var appTheme = Utils.GetGlobalAppTheme();
                if (appTheme == AppTheme.Light)
                {
                    coreTheme.VisualTheme = Syncfusion.Maui.Themes.SfVisuals.MaterialLight;
                    toolkitTheme.VisualTheme = Syncfusion.Maui.Toolkit.Themes.SfVisuals.MaterialLight;
                }
                else
                {
                    coreTheme.VisualTheme = Syncfusion.Maui.Themes.SfVisuals.MaterialDark;
                    toolkitTheme.VisualTheme = Syncfusion.Maui.Toolkit.Themes.SfVisuals.MaterialDark;
                }

                SfPickerResources.ResourceManager = new ResourceManager("History.MobileClient.Resources.SfDateTimePicker", Current.GetType().Assembly);
            }
        }
    }

    public static async Task HandlePushNotificationAsync(string pushData)
    {
        Preferences.Remove("PushData");

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(pushData);

        if (!data.TryGetValue("Type", out var rawType)) return;
        if (!Enum.TryParse<NotificationType>(rawType, out var type)) return;

        if (type == NotificationType.FriendRequest)
        {
            if (!data.TryGetValue("UserId", out var userId)) return;

            var page = new UserPage(userId);
            await PushAsync(page);
        }
        else if (type == NotificationType.Message)
        {
            if (!data.TryGetValue("MessageId", out var messageId)) return;
            var messageResult = await ExecuteRequestAsync(new GetMessage(messageId));
            if (messageResult.IsFailure) return;

            var messageViewModel = new MessageViewModel(messageResult.Value);
            var page = new MessagePage(messageViewModel);
            await PushAsync(page);
        }
        else if (type == NotificationType.Restriction)
        {
            var accept = await Page.DisplayAlertAsync("제재 내역", data["Body"], Constants.PromptOk, "소명 신청하기");
            if (!accept)
            {
                var copy = await Page.DisplayAlertAsync("알림", "공식 디스코드에서 소명 신청을 받고 있습니다.", "디스코드 초대 URL 복사", "확인");
                if (copy)
                {
                    await Clipboard.SetTextAsync(Constants.DiscordInviteUrl);
                    await Toast.Make("디스코드 초대 URL이 클립보드에 복사되었습니다.").Show();
                }
            }
        }
        else if (type == NotificationType.InviteCodeRequest) await PushAsync(new InviteCodeRequestsPage());
        else if (type == NotificationType.InviteCodeRequestResult) await PushAsync(new InviteCodesPage());
        else
        {
            if (!data.TryGetValue("PostId", out var postId)) return;

            var postResult = await ExecuteRequestAsync(new GetPost(postId));
            if (postResult.IsFailure) return;

            var postViewModel = new HistoryPostViewModel(postResult.Value, PostType.Unwrapped);
            var page = new PostPage(postViewModel);
            await PushAsync(page);
        }

    }
}