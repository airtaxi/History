using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.Commons.Interfaces;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Plugin.Firebase.CloudMessaging;
using ShimSkiaSharp;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace History.MobileClient;

public partial class App : Application
{
    private static readonly SemaphoreSlim ApiRequestSemaphore = new(1, 1);
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
                        var result = await Page.DisplayAlert("오류", $"MAUI 코드베이스에서 버그가 발견되었습니다. 애플리케이션을 재시작해주세요.", "확인", "자세히 알아보기");
                        if (result) return;

                        await Page.DisplayAlert("자세히 알아보기", "이 오류는 MAUI의 CarouselView에서 발생하는 버그로, History 애플리케이션과는 관련이 없습니다.", "확인");
                    });
                }
                else if (!exception.Message.Contains("FFImageLoading.Maui.Platform.DroidImageView") && !exception.StackTrace.Contains("FFImageLoading.Maui.Platform.DroidImageView"))
                    Dispatcher.Dispatch(() => Page.DisplayAlert("오류", $"{exception.Message}\n{exception.StackTrace}", Constants.PromptOk));

                Debugger.BreakForUserUnhandledException(exception);
            }
        };
#endif
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
            duplicatePage = navigationStack.FirstOrDefault(p => p is PostPage existingPostPage && existingPostPage.ViewModel.Post.Id == postPage.ViewModel.Post.Id);
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
            await ApiRequestSemaphore.WaitAsync();
            WeakReferenceMessenger.Default.Send(new LoadingStateChangedMessage(true));

            await Shared.ApiHandler.ExecuteRequestAsync(request);
            return Result.Success();
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType))
                await TopPage.DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
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

            if (!hiddenErrorTypes.Contains(errorType))
                await TopPage.DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
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

    protected override Window CreateWindow(IActivationState activationState)
    {
        MainWindow = new Window(new LoginPage());
        return MainWindow;
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
        else
        {
            if (!data.TryGetValue("PostId", out var postId)) return;

            var postResult = await ExecuteRequestAsync(new GetPost(postId));
            if (postResult.IsFailure) return;

            var postViewModel = new PostViewModel(postResult.Value, PostType.Unwrapped);
            var page = new PostPage(postViewModel);
            await PushAsync(page);
        }

    }
}