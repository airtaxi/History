using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Helpers;

// Reflection-free replacement for the removed behavior package's Unloaded event trigger: the
// attached property runs the bound command when the element leaves the visual tree, which keeps
// the trimmed and Native AOT publishes free of the IL2026 warnings it used to raise.
internal static class FrameworkElementCommandHelper
{
    public static readonly DependencyProperty UnloadedCommandProperty = DependencyProperty.RegisterAttached("UnloadedCommand", typeof(ICommand), typeof(FrameworkElementCommandHelper), new PropertyMetadata(null, OnUnloadedCommandChanged));

    public static ICommand GetUnloadedCommand(DependencyObject dependencyObject) => (ICommand)dependencyObject.GetValue(UnloadedCommandProperty);

    public static void SetUnloadedCommand(DependencyObject dependencyObject, ICommand value) => dependencyObject.SetValue(UnloadedCommandProperty, value);

    private static void OnUnloadedCommandChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not FrameworkElement frameworkElement) return;

        frameworkElement.Unloaded -= OnFrameworkElementUnloaded;

        if (e.NewValue is ICommand) frameworkElement.Unloaded += OnFrameworkElementUnloaded;
    }

    private static void OnFrameworkElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement frameworkElement) return;

        var command = GetUnloadedCommand(frameworkElement);

        if (command?.CanExecute(e) == true) command.Execute(e);
    }
}
