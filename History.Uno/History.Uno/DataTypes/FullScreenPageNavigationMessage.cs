using Microsoft.UI.Xaml.Controls;

namespace History.Uno.DataTypes;

public class FullScreenPageNavigationMessage(Page page, bool disappear) : ValueChangedMessage<Page>(page)
{
    public bool Disappear { get; } = disappear;
}