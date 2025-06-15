using Android.App;
using Android.Runtime;

// Needed for JobScheduler
[assembly: UsesPermission(Android.Manifest.Permission.WakeLock)]
[assembly: UsesPermission(Android.Manifest.Permission.ReceiveBootCompleted)]

// Needed for haptic feedback
[assembly: UsesPermission(Android.Manifest.Permission.Vibrate)]

// Needed for Picking photo/video
[assembly: UsesPermission(Android.Manifest.Permission.WriteExternalStorage)]

// Neeed for self-update
[assembly: UsesPermission(Android.Manifest.Permission.InstallPackages)]
[assembly: UsesPermission(Android.Manifest.Permission.RequestInstallPackages)]
[assembly: UsesPermission(Android.Manifest.Permission.RequestDeletePackages)]
[assembly: UsesPermission(Android.Manifest.Permission.DeletePackages)]

namespace History.MobileClient;
[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
