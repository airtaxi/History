using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Enums;
using History.Commons.Interfaces;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Services;
using History.WindowsClient.ViewModels;
using History.WindowsClient.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace History.WindowsClient;

public partial class App : Application
{
    private const string OAuthProtocolScheme = "history-app";

    private static readonly SemaphoreSlim ActionRequestSemaphore = new(1, 1);

    public static IServiceProvider Services { get; private set; }

    private Window _window;

    public App()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        InitializeComponent();

        ApiHandler.Platform = "Windows";
        ApiHandler.ApplicationVersion = GetApplicationVersion();

        UnhandledException += OnApplicationUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        AppInstance.GetCurrent().Activated += OnAppInstanceActivated;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();

        // Cold start via "history-app://" protocol activation: the login view model
        // subscribes to messages during MainWindow construction above, so handle
        // the activation arguments after the window is up.
        TryHandleOAuthActivation(AppInstance.GetCurrent().GetActivatedEventArgs());
    }

    // Redirected activation from a second instance (see Program.Main).
    private static void OnAppInstanceActivated(object sender, AppActivationArguments arguments) => TryHandleOAuthActivation(arguments);

    private static void TryHandleOAuthActivation(AppActivationArguments arguments)
    {
        if (arguments.Kind != ExtendedActivationKind.Protocol) return;
        if (arguments.Data is not IProtocolActivatedEventArgs protocolActivatedEventArguments) return;

        var uri = protocolActivatedEventArguments.Uri;
        if (!uri.Scheme.Equals(OAuthProtocolScheme, StringComparison.OrdinalIgnoreCase)) return;

        // The server redirects to "{state}?id_token=...", and the state is the
        // redirect URL: "history-app://auth/google" or "history-app://auth/apple".
        // In these URIs the "auth" segment is parsed as the host, and the
        // provider name ("google"/"apple") is the path segment.
        if (!uri.Host.Equals("auth", StringComparison.OrdinalIgnoreCase)) return;

        var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length < 1) return;

        SocialService? provider = pathSegments[0].ToLowerInvariant() switch
        {
            "google" => SocialService.Google,
            "apple" => SocialService.Apple,
            _ => null,
        };
        if (provider == null) return;

        var queryParameters = HttpUtility.ParseQueryString(uri.Query);
        var idToken = queryParameters["id_token"];
        if (string.IsNullOrEmpty(idToken)) return;

        WeakReferenceMessenger.Default.Send(new OAuthLoginMessage(idToken, provider.Value, queryParameters["user"]));
    }

    public static async Task<Result> ExecuteRequestAsync(IBaseRequest request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try
        {
            await ExecuteWithLoadingAsync(() => CommonShared.ApiHandler.ExecuteRequestAsync(request));
            return Result.Success();
        }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await ShowErrorDialogAsync($"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}");
            return (errorType, exception.Message);
        }
    }

    public static async Task<Result<T>> ExecuteRequestAsync<T>(IBaseRequest<T> request, params ErrorType[] hiddenErrorTypes)
    {
        hiddenErrorTypes ??= [];

        try { return await ExecuteWithLoadingAsync(() => CommonShared.ApiHandler.ExecuteRequestAsync(request)); }
        catch (HttpRequestException exception)
        {
            var errorType = StatusCodeToErrorType(exception.StatusCode ?? HttpStatusCode.InternalServerError);

            if (!hiddenErrorTypes.Contains(errorType)) await ShowErrorDialogAsync($"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}");
            return (errorType, exception.Message);
        }
    }

    public static async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        try
        {
            await ActionRequestSemaphore.WaitAsync();
            MainWindow.ShowLoading();

            await action();
        }
        finally
        {
            MainWindow.HideLoading();
            ActionRequestSemaphore.Release();
        }
    }

    public static async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action)
    {
        try
        {
            await ActionRequestSemaphore.WaitAsync();
            MainWindow.ShowLoading();

            return await action();
        }
        finally
        {
            MainWindow.HideLoading();
            ActionRequestSemaphore.Release();
        }
    }

    public static async Task ShowErrorDialogAsync(string message)
    {
        if (MainWindow.Frame.DispatcherQueue.HasThreadAccess) await MainWindow.Frame.ShowMessageDialogAsync(new("오류", message));
        else MainWindow.Frame.DispatcherQueue.TryEnqueue(async () => await MainWindow.Frame.ShowMessageDialogAsync(new("오류", message)));
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

    private static string GetApplicationVersion()
    {
        try
        {
            if (Package.Current != null)
            {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }
        catch { }

        return "1.0.0";
    }

    private static void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ApplicationSettingsService>();
        serviceCollection.AddSingleton(sp => sp.GetRequiredService<ApplicationSettingsService>().Settings);
        serviceCollection.AddSingleton(sp => new ApplicationThemeService(sp.GetRequiredService<ApplicationSettingsService>()));
        serviceCollection.AddSingleton(sp => new ApplicationNotificationService());
        serviceCollection.AddSingleton(sp => new StoreUpdateService(sp.GetRequiredService<ApplicationSettingsService>(), sp.GetRequiredService<ApplicationNotificationService>()));
        serviceCollection.AddTransient(sp => new LoginPageViewModel(sp.GetRequiredService<ApplicationSettingsService>()));
        serviceCollection.AddTransient(sp => new RegisterPageViewModel(sp.GetRequiredService<ApplicationSettingsService>()));
        serviceCollection.AddTransient(sp => new MainPageViewModel());
    }
    private static void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs unhandledExceptionEventArguments)
    {
        WriteException("Microsoft.UI.Xaml.Application.UnhandledException", unhandledExceptionEventArguments.Exception);
        unhandledExceptionEventArguments.Handled = true;
    }

    private static void OnCurrentDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs unhandledExceptionEventArguments)
    {
        if (unhandledExceptionEventArguments.ExceptionObject is Exception exception)
        {
            WriteException("AppDomain.CurrentDomain.UnhandledException", exception);
        }
    }

    private static void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArguments)
    {
        WriteException("TaskScheduler.UnobservedTaskException", unobservedTaskExceptionEventArguments.Exception);
        unobservedTaskExceptionEventArguments.SetObserved();
    }

    private static void WriteException(string source, Exception exception) => Debug.WriteLine(CreateExceptionMessage(source, exception));

    private static string CreateExceptionMessage(string source, Exception exception)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append('[');
        stringBuilder.Append(source);
        stringBuilder.Append("] ");

        var currentException = exception;
        while (currentException is not null)
        {
            stringBuilder.Append(currentException.GetType().FullName);
            stringBuilder.Append(": ");
            stringBuilder.Append(currentException.Message);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(currentException.StackTrace);

            currentException = currentException.InnerException;
            if (currentException is not null) stringBuilder.AppendLine("--->");
        }

        return stringBuilder.ToString();
    }
}
