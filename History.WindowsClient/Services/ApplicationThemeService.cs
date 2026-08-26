using History.WindowsClient.Models;
using Microsoft.UI.Xaml;
using System.ComponentModel;

namespace History.WindowsClient.Services;

public sealed partial class ApplicationThemeService(ApplicationSettingsService settingsService) : IDisposable
{
    private readonly List<WeakReference<FrameworkElement>> _themeTargetReferences = [];

    private bool _disposed;

    public event Action<ElementTheme> ThemeChanged;

    public void SetTheme(ElementTheme theme)
    {
        if (settingsService.Settings.Theme == theme) return;

        settingsService.Settings.Theme = theme;

        ApplyThemeToRegisteredTargets();
        ThemeChanged?.Invoke(theme);
    }

    private void ApplyThemeToElement(FrameworkElement frameworkElement) => frameworkElement.RequestedTheme = settingsService.Settings.Theme;

    public void ApplyThemeToWindow(Window window)
    {
        if (window.Content is FrameworkElement frameworkElement)
        {
            RegisterThemeTarget(frameworkElement);
        }
    }

    public void RegisterThemeTarget(FrameworkElement frameworkElement)
    {
        ApplyThemeToElement(frameworkElement);
        for (var themeTargetReferenceIndex = _themeTargetReferences.Count - 1; themeTargetReferenceIndex >= 0; themeTargetReferenceIndex--)
        {
            if (!_themeTargetReferences[themeTargetReferenceIndex].TryGetTarget(out var registeredFrameworkElement))
            {
                _themeTargetReferences.RemoveAt(themeTargetReferenceIndex);
                continue;
            }

            if (ReferenceEquals(registeredFrameworkElement, frameworkElement)) return; // Already added
        }

        _themeTargetReferences.Add(new WeakReference<FrameworkElement>(frameworkElement));
    }

    private void ApplyThemeToRegisteredTargets()
    {
        for (var themeTargetReferenceIndex = _themeTargetReferences.Count - 1; themeTargetReferenceIndex >= 0; themeTargetReferenceIndex--)
        {
            if (!_themeTargetReferences[themeTargetReferenceIndex].TryGetTarget(out var frameworkElement))
            {
                _themeTargetReferences.RemoveAt(themeTargetReferenceIndex);
                continue;
            }

            ApplyThemeToElement(frameworkElement);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        settingsService.Settings.PropertyChanged -= OnApplicationSettingsPropertyChanged;
    }

    private void OnApplicationSettingsPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArguments)
    {
        if (propertyChangedEventArguments.PropertyName != nameof(ApplicationSettings.Theme)) return;

        SetTheme(settingsService.Settings.Theme);
    }
}