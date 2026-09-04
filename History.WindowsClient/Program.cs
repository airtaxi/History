using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace History.WindowsClient;

public static class Program
{
    private const string SingleInstanceKey = "main";

    [STAThread]
    private static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var mainInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);
        if (!mainInstance.IsCurrent)
        {
            // Awaiting RedirectActivationToAsync directly in an async Task Main flips the main
            // thread to MTA, which later makes WebView2 fail to initialize with
            // RPC_E_CHANGED_MODE (0x80010106). Run the redirect on a background thread and
            // wait on the STA via CoWaitForMultipleObjects so the apartment stays STA.
            RedirectActivationTo(AppInstance.GetCurrent().GetActivatedEventArgs(), mainInstance);
            return;
        }

        Application.Start(initializationParams =>
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            var synchronizationContext = new DispatcherQueueSynchronizationContext(dispatcherQueue);
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            _ = new App();
        });
    }

    private static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
    {
        using var redirectEventHandle = PInvoke.CreateEvent(null, true, false, null);
        Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            PInvoke.SetEvent(redirectEventHandle);
        });

        var waitHandle = new HANDLE(redirectEventHandle.DangerousGetHandle());
        _ = PInvoke.CoWaitForMultipleObjects(0, PInvoke.INFINITE, [waitHandle], out _);
    }
}
