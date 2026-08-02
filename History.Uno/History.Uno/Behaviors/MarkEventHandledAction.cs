using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;

namespace History.Uno.Behaviors;

/// <summary>
/// Marks the triggering Tapped event as handled so that it does not bubble up to ancestor
/// EventTriggerBehavior instances. Add this as the last action inside an EventTriggerBehavior
/// attached to a nested tap target that lives within a larger tappable surface.
/// </summary>
public sealed partial class MarkEventHandledAction : DependencyObject, IAction
{
    public object Execute(object sender, object parameter)
    {
        if (parameter is TappedRoutedEventArgs tappedRoutedEventArgs) tappedRoutedEventArgs.Handled = true;

        return false;
    }
}
