using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace History.WindowsClient.Helpers;

// Win32 modal-window support: a modal window takes the owner window as its Win32 owner so it
// stays above it in z-order, and the owner window stops receiving input until the modal closes.
public static class WindowHelper
{
    // Makes the window modal against the owner window, then activates it.
    public static void ActivateModal(this Window window, Window ownerWindow)
    {
        window.MakeModal(ownerWindow);
        window.Activate();
    }

    // Makes the window modal against the owner window from this point on: the owner window
    // stops receiving input immediately, and closing the modal window restores the owner's
    // input and focus.
    public static void MakeModal(this Window window, Window ownerWindow)
    {
        var windowHandle = (HWND)WindowNative.GetWindowHandle(window);
        var ownerWindowHandle = (HWND)WindowNative.GetWindowHandle(ownerWindow);

        SetWindowOwner(windowHandle, ownerWindowHandle);
        PInvoke.EnableWindow(ownerWindowHandle, false);

        window.Closed += (_, __) =>
        {
            PInvoke.EnableWindow(ownerWindowHandle, true);
            PInvoke.SetForegroundWindow(ownerWindowHandle);
        };
    }

    private static void SetWindowOwner(HWND windowHandle, HWND ownerWindowHandle) => PInvoke.SetWindowLongPtr(windowHandle, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, ownerWindowHandle);
}
