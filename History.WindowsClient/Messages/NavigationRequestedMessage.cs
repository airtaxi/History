using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

public sealed class NavigationRequestedMessage(XamlRoot xamlRoot, Type pageType, object parameter)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public Type PageType { get; } = pageType;

    public object Parameter { get; } = parameter;

    public static void Send(XamlRoot xamlRoot, Type pageType, object parameter)
    {
        var message = new NavigationRequestedMessage(xamlRoot, pageType, parameter);
        WeakReferenceMessenger.Default.Send(message);
    }
}