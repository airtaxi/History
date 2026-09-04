using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace History.WindowsClient.Helpers;

// Win32 modal-window support: a modal window takes the owner window as its Win32 owner so it
// stays above it in z-order, and the owner window stops receiving input until the modal closes.
public static partial class WindowHelper
{
    private const int GWLP_HWNDPARENT = -8;

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
        var windowHandle = WindowNative.GetWindowHandle(window);
        var ownerWindowHandle = WindowNative.GetWindowHandle(ownerWindow);

        SetWindowOwner(windowHandle, ownerWindowHandle);
        EnableWindow(ownerWindowHandle, false);

        window.Closed += (_, __) =>
        {
            EnableWindow(ownerWindowHandle, true);
            SetForegroundWindow(ownerWindowHandle);
        };
    }

    private static void SetWindowOwner(IntPtr windowHandle, IntPtr ownerWindowHandle) => SetWindowLongPtr(windowHandle, GWLP_HWNDPARENT, ownerWindowHandle);

    // 32-bit Windows does not export SetWindowLongPtrW; there it is a macro for SetWindowLongW.
    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => Environment.Is64BitProcess ? SetWindowLongPtrW(hWnd, nIndex, dwNewLong) : SetWindowLongW(hWnd, nIndex, dwNewLong);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetWindowLongW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);
}
