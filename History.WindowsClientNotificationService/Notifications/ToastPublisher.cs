using History.Commons.Enums;
using History.Commons.Helpers;
using History.WindowsClientNotificationService.Core;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.Management.Deployment;
using Windows.UI.Notifications;

namespace History.WindowsClientNotificationService.Notifications;

// Shows local toasts under the History app's AUMID so they read as the app's notifications and
// open the app through the shared "history-app://toast" deep link. The packaged AUMID is used
// explicitly because the notification platform accepts it even from a process that has no
// package identity, and the string is stable across app updates.
public sealed class ToastPublisher(FileLogger logger)
{
    private const string PackageName = "49536HowonLee.297428538D1EE";
    private const string ApplicationId = "App";
    private const string AppLogoPackageSource = "ms-appx:///Assets/Square44x44Logo.targetsize-48.png";
    private const string AppLogoRelativePath = @"Assets\Square44x44Logo.targetsize-48.png";
    private const string RepositoryPackagesKeyPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";
    private const int MaxTrackedGroups = 50;

    private readonly Lock _toastLock = new();
    private readonly Dictionary<string, List<ToastNotification>> _trackedToastsByGroup = [];
    private ToastNotifier _toastNotifier;
    private string _aumid;
    private string _appLogoSource;
    private bool _appLogoResolved;

    public void Show(string title, string body, string imageUrl, IReadOnlyDictionary<string, string> data)
    {
        var payload = ToastPayloadBuilder.Build(title, body, imageUrl, data, ResolveAppLogoSource());
        if (payload == null) return;

        var aumid = ResolveAumid();
        if (aumid == null)
        {
            logger.Log("Toast skipped: the History app package could not be located.");
            return;
        }

        try
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(payload);
            var toastNotification = new ToastNotification(xmlDocument);
            ResolveToastNotifier(aumid).Show(toastNotification);
            TrackToast(ToastPayloadBuilder.ResolveGroup(data), toastNotification);
        }
        catch (Exception exception) { logger.Log($"Toast display failed: {exception.Message}"); }
    }

    // Dismisses every toast for a post so a post the user has read stops showing as an unread
    // system notification. The on-screen banners are hidden first, then the matching action center
    // entries are removed by group.
    public void RemovePostToasts(NotificationPostPlatform platform, string postId)
    {
        var group = NotificationToastKeys.BuildPostGroup(platform, postId);
        if (group == null) return;

        var aumid = ResolveAumid();
        if (aumid == null) return;

        try
        {
            HideTrackedToasts(group);
            ToastNotificationManager.History.RemoveGroup(group, aumid);
        }
        catch (Exception exception) { logger.Log($"Toast removal failed: {exception.Message}"); }
    }

    public void ClearHistory()
    {
        var aumid = ResolveAumid();
        if (aumid == null) return;

        try { ToastNotificationManager.History.Clear(aumid); }
        catch (Exception exception) { logger.Log($"Toast history clear failed: {exception.Message}"); }
    }

    private ToastNotifier ResolveToastNotifier(string aumid)
    {
        if (_toastNotifier != null) return _toastNotifier;

        _toastNotifier = ToastNotificationManager.CreateToastNotifier(aumid);
        return _toastNotifier;
    }

    // Keeps the toast objects per group so a live banner can be hidden when the post is read. The
    // map is bounded because unreferenced groups are already gone from the action center.
    private void TrackToast(string group, ToastNotification toastNotification)
    {
        if (string.IsNullOrEmpty(group)) return;

        lock (_toastLock)
        {
            if (!_trackedToastsByGroup.TryGetValue(group, out var toasts))
            {
                toasts = [];
                _trackedToastsByGroup[group] = toasts;
                while (_trackedToastsByGroup.Count > MaxTrackedGroups) _trackedToastsByGroup.Remove(_trackedToastsByGroup.Keys.First());
            }

            toasts.Add(toastNotification);
        }
    }

    private void HideTrackedToasts(string group)
    {
        List<ToastNotification> toasts;
        lock (_toastLock)
        {
            if (!_trackedToastsByGroup.Remove(group, out toasts)) return;
        }

        var toastNotifier = _toastNotifier;
        if (toastNotifier == null) return;

        // A toast may already have expired; hiding is best-effort and the history removal still runs.
        foreach (var toastNotification in toasts)
        {
            try { toastNotifier.Hide(toastNotification); }
            catch { }
        }
    }

    // The notification shows the app icon next to the text. The package asset is referenced through
    // ms-appx when the process runs with package identity, and through the absolute path of the
    // installed package otherwise, because ms-appx cannot be resolved without identity.
    private string ResolveAppLogoSource()
    {
        if (_appLogoResolved) return _appLogoSource;
        _appLogoResolved = true;

        _appLogoSource = HasPackageIdentity() ? AppLogoPackageSource : ResolveAppLogoSourceFromInstalledLocation();
        return _appLogoSource;
    }

    private string ResolveAppLogoSourceFromInstalledLocation()
    {
        try
        {
            var installedLocation = new PackageManager().FindPackagesForUser(string.Empty, PackageName).FirstOrDefault()?.InstalledLocation.Path;
            if (installedLocation == null) return null;

            var logoPath = Path.Combine(installedLocation, AppLogoRelativePath);
            return File.Exists(logoPath) ? new Uri(logoPath).AbsoluteUri : null;
        }
        catch (Exception exception)
        {
            logger.Log($"App logo lookup failed: {exception.Message}");
            return null;
        }
    }

    private static bool HasPackageIdentity()
    {
        try { return Windows.ApplicationModel.Package.Current != null; }
        catch { return false; }
    }

    // Resolution order: the current package identity (when the app model launched the service),
    // the registered package repository, then the deployment API.
    private string ResolveAumid()
    {
        if (_aumid != null) return _aumid;

        var familyName = ResolveFamilyName();
        if (familyName == null) return null;

        _aumid = $"{familyName}!{ApplicationId}";
        return _aumid;
    }

    private string ResolveFamilyName()
    {
        try { return Windows.ApplicationModel.Package.Current.Id.FamilyName; }
        catch { }

        var repositoryFamilyName = ResolveFamilyNameFromRegistry();
        if (repositoryFamilyName != null) return repositoryFamilyName;

        try
        {
            var package = new PackageManager().FindPackagesForUser(string.Empty, PackageName).FirstOrDefault();
            return package?.Id.FamilyName;
        }
        catch (Exception exception)
        {
            logger.Log($"Package lookup failed: {exception.Message}");
            return null;
        }
    }

    // Repository keys are package full names, which begin with the package name plus an
    // underscore; the family name is the name and publisher hash segments.
    private static string ResolveFamilyNameFromRegistry()
    {
        try
        {
            using var packagesKey = Registry.CurrentUser.OpenSubKey(RepositoryPackagesKeyPath);
            if (packagesKey == null) return null;

            var packageFullName = packagesKey.GetSubKeyNames().FirstOrDefault(name => name.StartsWith(PackageName + "_", StringComparison.OrdinalIgnoreCase));
            if (packageFullName == null) return null;

            var segments = packageFullName.Split('_');
            return segments.Length >= 2 ? $"{segments[0]}_{segments[^1]}" : null;
        }
        catch { return null; }
    }
}
