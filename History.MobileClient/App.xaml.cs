using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.Enums;
using History.Commons.Interfaces;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using ShimSkiaSharp;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace History.MobileClient;

public partial class App : Application
{
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
    public static Page TopPage => Navigation.ModalStack.Count > 0 ? Navigation.ModalStack[Navigation.ModalStack.Count - 1] : Page;

    public static INavigation Navigation => Current.Windows[0].Page.Navigation;
    public static async Task PushModalAsync(Page page) => await Current.Windows[0].Page.Navigation.PushModalAsync(page);
    public static async Task PopModalAsync() => await Current.Windows[0].Page.Navigation.PopModalAsync();

    
    public static async Task<Result> ExecuteRequestAsync(IBaseRequest request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            TopPage.IsEnabled = false;
            TopPage.IsBusy = true;

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
            TopPage.IsEnabled = true;
            TopPage.IsBusy = false;
        }
    }

    public static async Task<Result<T>> ExecuteRequestAsync<T>(IBaseRequest<T> request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            TopPage.IsEnabled = false;
            TopPage.IsBusy = true;

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
            TopPage.IsEnabled = true;
            TopPage.IsBusy = false;
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
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(pushData);

        if (!data.TryGetValue("Type", out var rawType)) return;
        if (!Enum.TryParse<NotificationType>(rawType, out var type)) return;

        if (type == NotificationType.FriendRequest)
        {
            if (!data.TryGetValue("UserId", out var userId)) return;

            var page = new UserPage(userId);
            await PushModalAsync(page);
        }
        else
        {
            if (!data.TryGetValue("PostId", out var postId)) return;

            var postResult = await ExecuteRequestAsync(new GetPost(postId));
            if (postResult.IsFailure) return;

            var postViewModel = new PostViewModel(postResult.Value, false);
            var page = new PostPage(postViewModel);
            await PushModalAsync(page);
        }

    }
}