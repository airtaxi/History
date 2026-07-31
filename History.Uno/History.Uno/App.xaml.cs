using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.UI.Xaml.Controls;
using Uno.Resizetizer;
using History.Uno.Services;

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