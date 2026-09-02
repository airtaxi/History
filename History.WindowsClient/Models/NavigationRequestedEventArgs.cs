namespace History.WindowsClient.Models;

public sealed class NavigationRequestedEventArgs(Type pageType, object parameter) : EventArgs
{
    public Type PageType { get; } = pageType;

    public object Parameter { get; } = parameter;
}