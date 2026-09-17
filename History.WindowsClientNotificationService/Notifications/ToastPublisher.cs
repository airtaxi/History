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
    private const string RepositoryPackagesKeyPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages";

    private string _aumid;

    public void Show(string title, string body, string imageUrl, IReadOnlyDictionary<string, string> data)
    {
        var payload = ToastPayloadBuilder.Build(title, body, imageUrl, data);
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
            ToastNotificationManager.CreateToastNotifier(aumid).Show(new ToastNotification(xmlDocument));
        }
        catch (Exception exception) { logger.Log($"Toast display failed: {exception.Message}"); }
    }

    public void ClearHistory()
    {
        var aumid = ResolveAumid();
        if (aumid == null) return;

        try { ToastNotificationManager.History.Clear(aumid); }
        catch (Exception exception) { logger.Log($"Toast history clear failed: {exception.Message}"); }
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
