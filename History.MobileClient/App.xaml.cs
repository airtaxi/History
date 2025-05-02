using History.Commons;
using History.Commons.Interfaces;
using History.MobileClient.Pages;
using RestSharp;
using System.Net;

namespace History.MobileClient;

public partial class App : Application
{
    public static Window MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    public static Page Page
    {
        get => Current.Windows[0].Page;
        set => Current.Windows[0].Page = value;
    }
    public static INavigation Navigation => Current.Windows[0].Page.Navigation;
    public static async Task PushModalAsync(Page page) => await Current.Windows[0].Page.Navigation.PushModalAsync(page);
    public static async Task PopModalAsync() => await Current.Windows[0].Page.Navigation.PopModalAsync();

    public static async Task<Result> ExecuteRequestAsync(IBaseRequest request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            Current.Windows[0].Page.IsEnabled = false;
            Current.Windows[0].Page.IsBusy = true;

            await Shared.ApiHandler.ExecuteRequestAsync(request);
            return Result.Success();
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType))
                await Current.Windows[0].Page.DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
            return (errorType, exception.Message);
        }
        finally
        {
            Current.Windows[0].Page.IsEnabled = true;
            Current.Windows[0].Page.IsBusy = false;
        }
    }

    public static async Task<Result<T>> ExecuteRequestAsync<T>(IBaseRequest<T> request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            Current.Windows[0].Page.IsEnabled = false;
            Current.Windows[0].Page.IsBusy = true;

            return await Shared.ApiHandler.ExecuteRequestAsync(request);
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType))
                await Current.Windows[0].Page.DisplayAlert("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
            return (errorType, exception.Message);
        }
        finally
        {
            Current.Windows[0].Page.IsEnabled = true;
            Current.Windows[0].Page.IsBusy = false;
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
}