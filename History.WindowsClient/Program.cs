using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.Runtime.InteropServices;

namespace History.WindowsClient;

public static class Program
{
    private const string SingleInstanceKey = "main";

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ole32.dll")]
    private static extern uint CoWaitForMultipleObjects(uint dwFlags, uint dwMilliseconds, ulong nHandles, IntPtr[] pHandles, out uint dwIndex);

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
        var redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
        Task.Run(() =>
        {
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
            SetEvent(redirectEventHandle);
        });

        const uint infinite = 0xFFFFFFFF;
        _ = CoWaitForMultipleObjects(0, infinite, 1, [redirectEventHandle], out _);
        CloseHandle(redirectEventHandle);
    }
}