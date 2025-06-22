using System.Diagnostics;
using System.Net;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Interfaces;
using History.MobileClient.DataTypes;
using Microsoft.Maui.Controls.Xaml;
using Uno.Resizetizer;
using Windows.UI.Popups;

namespace History.MobileClient;

public partial class App : Application
{
    private static readonly SemaphoreSlim ApiRequestSemaphore = new(1, 1);
    private static readonly SemaphoreSlim NavigationSemaphore = new(1, 1);

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
                    s_mainWindow.DispatcherQueue.TryEnqueue(async () =>
                    {
                        var result = await ShowMessageDialogAsync("오류", $"MAUI 코드베이스에서 버그가 발견되었습니다. 애플리케이션을 재시작해주세요.", "확인", "자세히 알아보기");
                        if (result == ContentDialogResult.Primary) return;

                        await ShowMessageDialogAsync("자세히 알아보기", "이 오류는 MAUI의 CarouselView에서 발생하는 버그로, History 애플리케이션과는 관련이 없습니다.", "확인");
                    });
                }
                else if (!exception.Message.Contains("FFImageLoading.Maui.Platform.DroidImageView") && !exception.StackTrace.Contains("FFImageLoading.Maui.Platform.DroidImageView"))
                    s_mainWindow.DispatcherQueue.TryEnqueue(async () => await ShowMessageDialogAsync("오류", $"{exception.Message}\n{exception.StackTrace}", Constants.PromptOk));

                Debugger.BreakForUserUnhandledException(exception);
            }
        };
#endif

#if __IOS__ || __ANDROID__
        FeatureConfiguration.Style.ConfigureNativeFrameNavigation();
#endif
    }

    private static Window s_mainWindow;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        s_mainWindow = new Window();
#if DEBUG
        s_mainWindow.UseStudio();
#endif

#if MAUI_EMBEDDING && !WINDOWS
        this.UseMauiEmbedding<MauiControls.App>(s_mainWindow, maui => maui
                    .UseMauiControls());
#endif

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (s_mainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new();

            // Place the frame in the current Window
            s_mainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        s_mainWindow.SetWindowIcon();
        // Ensure the current window is active
        s_mainWindow.Activate();

#if ANDROID || IOS
        var isDarkMode = SystemThemeHelper.IsRootInDarkMode(rootFrame.XamlRoot);
        MauiControls.App.SetAppTheme(isDarkMode);
#endif
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }

#if IOS
    public override bool OpenUrl(UIKit.UIApplication app, Foundation.NSUrl url, Foundation.NSDictionary options)
    {
        Google.SignIn.SignIn.SharedInstance.HandleUrl(url);

        return base.OpenUrl(app, url, options);
    }

    public override bool FinishedLaunching(UIKit.UIApplication application, Foundation.NSDictionary launchOptions)
    {
        Plugin.Firebase.Core.Platforms.iOS.CrossFirebase.Initialize();
        Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation.Initialize();
        return base.FinishedLaunching(application, launchOptions);
    }
#endif

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
                await ShowMessageDialogAsync("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
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
                await ShowMessageDialogAsync("오류", $"알 수 없는 오류가 발생했습니다.\n[{exception.StatusCode}]: {exception.Message}", Constants.PromptOk);
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

    public static async Task<ContentDialogResult> ShowMessageDialogAsync(string title, string content, string primaryButtonText = Constants.PromptOk, string secondaryButtonText = null) => await s_mainWindow.Content.ShowMessageDialogAsync(title, content, primaryButtonText, secondaryButtonText);
}
